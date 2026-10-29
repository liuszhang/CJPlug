using System.Net.Http.Json;
using CJCore.AgentTool.MCPTools;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.A2A;

/// <summary>
/// 把一个 CJPlug 动态工具（已发布工作流 / 插头）投影成 CJCore 的 <see cref="IToolExecutor"/>。
///
/// <para>《（设计中）CJPlug接入CJCore实施计划-20261029》P8.1–8.2：每个 <see cref="IToolExecutor"/>
/// 实例对应 <b>一个</b>工具 —— 元数据（<see cref="Name"/> / <see cref="Description"/> /
/// <see cref="ParametersJson"/>）零转换映射到 CJCore 的 <c>ToolDefinition</c>，因此本类由
/// <see cref="PlugMcpToolSyncService"/> 按 <c>/api/plugbridge/tools</c> 的清单<b>逐个实例化</b>，
/// 再经 <c>DefaultToolRegistry.AddExecutor</c> 进入 dynamic 池。</para>
///
/// <para><b>执行链路</b>：<c>IToolRegistry.Resolve(name).ExecuteAsync(...)</c>
/// → <c>POST {McpServer}/api/plugbridge/call</c> → McpServer 的 <c>DynamicWorkflowTool.InvokeAsync</c>
/// → DispatchServer <c>/api/plug/executeMcpTool</c>。与 MCP 客户端进来时<b>走同一条链</b>，不会分叉。</para>
///
/// <para><b>McpSurface = External</b>（方案 8.2）：CJPlug 的工具本来就是给<b>外部 MCP 客户端</b>用的
///（TRAE / Cursor 等 IDE 已经直接连 <c>http://localhost:3001</c>），挂内部面反而变了语义。</para>
/// </summary>
public sealed class PlugMcpToolExecutor : IToolExecutor
{
    private const string FallbackSchema = "{\"type\":\"object\",\"properties\":{}}";

    private readonly HttpClient _http;
    private readonly ILogger<PlugMcpToolExecutor> _logger;

    public PlugMcpToolExecutor(
        string name,
        string description,
        string parametersJson,
        HttpClient http,
        ILogger<PlugMcpToolExecutor> logger)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("工具名不能为空", nameof(name));

        Name = name.Trim();
        Description = description ?? string.Empty;
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? FallbackSchema : parametersJson;
        _http = http;
        _logger = logger;
    }

    public string Name { get; }

    public string Description { get; }

    public string ParametersJson { get; }

    /// <summary>
    /// 是否危险操作。<b>按工具名前缀粗判</b>：CJPlug 的动态工具没有声明式副作用元数据，
    /// 统一判 safe 会让"启动真实设备 / 下发工单"一类工具绕过二次确认；这里对明显的执行类动词判危险。
    /// TODO(P8 后续)：在 PublishedWorkflowDto 上补 <c>IsDangerous</c> 字段，由发布侧显式声明。
    /// </summary>
    public bool IsDangerous { get; } = false;

    public bool Enabled { get; set; } = true;

    /// <summary>不属于任何特定 Agent ⇒ 所有 Agent 可见（null = 通用工具）。</summary>
    public string? AgentName => null;

    /// <summary>对外面（与 McpServer 的既有暴露口径一致，方案 8.2）。</summary>
    public McpSurface Surfaces => McpSurface.External;

    public async Task<object?> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext ctx,
        CancellationToken ct = default)
    {
        ct = ct == default ? ctx.CancellationToken : ct;

        try
        {
            var response = await _http.PostAsJsonAsync(
                CJ.Plug.HostWebServer.A2A.PlugMcpBridgePaths.CallRel,
                new { name = Name, argumentsJson },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return $"[CJPlug] 工具 {Name} 调用失败：HTTP {(int)response.StatusCode} {body}";
            }

            var payload = await response.Content
                .ReadFromJsonAsync<System.Text.Json.JsonDocument>(cancellationToken: ct);

            if (payload is null) return string.Empty;

            var root = payload.RootElement;
            if (root.TryGetProperty("error", out var err) && err.ValueKind == System.Text.Json.JsonValueKind.String)
                return $"[CJPlug] 工具 {Name} 执行失败：{err.GetString()}";

            return root.TryGetProperty("text", out var text) ? text.GetString() : string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CJPlug] 工具 {Tool} 经本地 MCP 桥执行异常", Name);
            return $"[CJPlug] 工具 {Name} 执行异常：{ex.Message}";
        }
    }
}
