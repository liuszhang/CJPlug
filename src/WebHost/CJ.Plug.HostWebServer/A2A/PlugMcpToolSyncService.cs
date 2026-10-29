using System.Net.Http.Json;
using CJCore.AgentTool.MCPTools;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.A2A;

/// <summary>
/// 把 CJPlug McpServer 的<b>动态工具清单</b>持续同步进 CJCore 的 <see cref="IToolRegistry"/>。
///
/// <para>《（设计中）CJPlug接入CJCore实施计划-20261029》P8.1。
/// <b>为什么是"同步"而不是一次性注册</b>：CJPlug 的工具来自<b>运行时发布的工作流/插头</b>
///（讲到 <c>DynamicToolRegistry.RefreshAsync</c> 从 ApiServer 拉 PublishedWorkflow 并重洗 Tool 名），
/// 集合随用户在客户端发布变更而变化 —— 静态注册一次就会越用越旧。</para>
///
/// <para><b>为什么走轮询而不是 SignalR</b>：McpServer 侧确实有 SignalR（订阅 DispatchServer 的
/// MCPToolUpdated），但本宿主要接的是 DispatchServer 的 hub，代价与复杂度都比一次轻 GET 高很多。
/// 工具清单是<b>低频变更</b>（发布新工作流才变），30s 轮询的成本可忽略；首次同步是在进程启动后
/// 立即跑一次，不等第一个周期。</para>
///
/// <para><b>失败默认保守</b>：取不到清单时<b>保持上一份</b>（不切成空集合）—— 一次网络抖动
/// 就把 Agent 的工具全下线，比"多留一会旧工具"伤害大得多。</para>
/// </summary>
public sealed class PlugMcpToolSyncService : BackgroundService
{
    /// <summary>轮询周期。与工具发布的实时性要求相称（发布 → 30s 内进入 Agent 工具列表）。</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    /// <summary>首次同步的宽限期：此时 McpServer / DispatchServer 可能还在冷启动。</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);

    private readonly DefaultToolRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PlugMcpToolSyncService> _logger;

    /// <param name="registry">
    /// 刻意注入<b>具体类型</b>而非 <see cref="IToolRegistry"/>：dynamic 池的增删入口
    /// （<c>AddExecutor</c> / <c>RemoveExecutor</c>）是 <see cref="DefaultToolRegistry"/> 上的成员，
    /// 契约里没有 —— CJCore 的取舍是"注册表可变这一事实不对消费方开放"。
    /// </param>
    public PlugMcpToolSyncService(
        DefaultToolRegistry registry,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PlugMcpToolSyncService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[P8] 工具清单同步失败，保留上一次的工具集合");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>上一轮同步时的工具名集合 —— 只删“我们曾经注册过、这次不在清单里”的工具，
    /// 不碰宿主/框架自己注册的 static 工具（<c>DefaultToolRegistry.RemoveExecutor</c> 也只动 dynamic 池）。</summary>
    private readonly HashSet<string> _lastNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>拉一次清单并按 diff 增删；同时也是启动期的首次强制同步入口。</summary>
    public async Task SyncOnceAsync(CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient(PlugMcpBridgeClientOptions.HttpClientName);

        var tools = await http.GetFromJsonAsync<List<ToolSummary>>(
            PlugMcpBridgePaths.ToolsRel, cancellationToken: ct) ?? new List<ToolSummary>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in tools)
        {
            if (string.IsNullOrWhiteSpace(t.Name)) continue;
            seen.Add(t.Name);

            var executor = new PlugMcpToolExecutor(
                t.Name, t.Description ?? string.Empty, t.ParametersJson ?? string.Empty,
                http, _loggerFactory.CreateLogger<PlugMcpToolExecutor>());

            // DefaultToolRegistry 对 dynamic 池是「同名覆盖」；static 池有同名内置工具时会被忽略（不抢占）。
            _registry.AddExecutor(executor);
        }

        var stale = _lastNames.Where(n => !seen.Contains(n)).ToList();
        foreach (var name in stale)
            _registry.RemoveExecutor(name);

        lock (_lastNames)
        {
            _lastNames.Clear();
            _lastNames.UnionWith(seen);
        }

        if (stale.Count > 0)
            _logger.LogInformation("[P8] 已下线 {Count} 个不再存在的动态工具", stale.Count);

        _logger.LogInformation(
            "[P8] 已从 CJPlug McpServer 同步 {Count} 个动态工具到 CJCore IToolRegistry",
            tools.Count);
    }

    private sealed record ToolSummary(string Name, string Description, string? ParametersJson);
}

/// <summary>桥用 HttpClient 的逻辑名与地址（由 Program.cs 注入具体地址）。</summary>
public static class PlugMcpBridgeClientOptions
{
    /// <summary>HttpClient 逻辑名（AddHttpClient(name, ...)）。</summary>
    public const string HttpClientName = "CJPlugMcpBridge";

    /// <summary>
    /// 默认地址。2026-10-29 单端口收敛后 = 主服务端根地址（<c>/api/plugbridge/*</c> 挂在它下面）。
    /// <para>
    /// ⚠ 不要写成 <c>http://localhost:5066/mcp</c>：这是给 <c>/api/plugbridge/*</c> 用的内部客户端，
    /// 每次请求都带绝对相对路径（<see cref="PlugMcpBridgePaths.ToolsRel"/>），BaseAddress 必须是根。
    /// 面向 IDE 的 MCP 连接串（<c>/mcp</c>）在 MCPToolsManage 那几处 UI 文案里单独维护。
    /// </para>
    /// </summary>
    public const string DefaultBaseAddress = "http://localhost:5066";
}
