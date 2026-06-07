using System.Net.Http.Json;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.McpServer.Tools;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace CJ.Plug.McpServer.Services;

/// <summary>
/// 动态工具注册器 —— 通过 SignalR 长连接接收 DispatchServer 的 MCP Tool 变更通知，
/// 按需从 ApiServer 拉取已发布工作流，构造 DynamicWorkflowTool 实例，
/// 提供 Tool 列表供 ListToolsHandler 使用，按名称查找工具供 CallToolHandler 使用。
/// </summary>
public class DynamicToolRegistry : IDisposable
{
    private readonly HttpClient _apiClient;
    private readonly ILogger<DynamicToolRegistry> _logger;
    private readonly object _lock = new();
    private Dictionary<string, DynamicWorkflowTool> _tools = new();
    private List<Tool> _protocolTools = new();
    private HubConnection? _hubConnection;
    private readonly string _dispatchHubUrl;

    public DynamicToolRegistry(
        IHttpClientFactory httpClientFactory,
        ILogger<DynamicToolRegistry> logger,
        string dispatchUrl,
        string apiUrl)
    {
        // SignalR 连接指向 DispatchServer
        _dispatchHubUrl = $"{dispatchUrl.TrimEnd('/')}/mainHub";

        // HTTP API 调用指向 ApiServer（mcptools 端点在 ApiServer 上）
        _apiClient = httpClientFactory.CreateClient("ApiServer");
        _logger = logger;
    }

    /// <summary>
    /// 获取所有动态工具的 Protocol.Tool 列表（供 ListToolsHandler 返回）
    /// </summary>
    public IReadOnlyList<Tool> GetProtocolTools()
    {
        lock (_lock)
        {
            return _protocolTools.AsReadOnly();
        }
    }

    /// <summary>
    /// 按名称查找动态工具（供 CallToolHandler 分发执行）
    /// </summary>
    public DynamicWorkflowTool? FindTool(string name)
    {
        lock (_lock)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }

    /// <summary>
    /// 从 ApiServer 刷新工作流缓存，重建工具映射
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var workflows = await _apiClient
                .GetFromJsonAsync<List<PublishedWorkflowDto>>(
                    "/api/mcptools/getPublishedWorkflows",
                    cancellationToken: ct) ?? new();

            var newTools = new Dictionary<string, DynamicWorkflowTool>();
            var newProtocolTools = new List<Tool>();
            var existingNames = new HashSet<string>();

            foreach (var dto in workflows)
            {
                var sanitizedName = ToolNameSanitizer.Sanitize(
                    dto.Name, dto.WorkflowDefinitionId);
                sanitizedName = ToolNameSanitizer.EnsureUnique(
                    sanitizedName, existingNames);
                existingNames.Add(sanitizedName);

                var tool = new DynamicWorkflowTool(
                    sanitizedName, dto, _apiClient);

                newTools[sanitizedName] = tool;
                newProtocolTools.Add(tool.ProtocolTool);
            }

            lock (_lock)
            {
                _tools = newTools;
                _protocolTools = newProtocolTools;
            }

            _logger.LogInformation(
                "DynamicToolRegistry refreshed: {Count} tools registered",
                newTools.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to refresh tools from ApiServer. Using {Count} cached.",
                _tools.Count);
        }
    }

    /// <summary>
    /// 启动 SignalR 长连接，监听 MCP Tool 变更通知
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_dispatchHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string>("MCPToolUpdated", async (toolId, action) =>
        {
            _logger.LogInformation(
                "Received MCPToolUpdated: toolId={ToolId}, action={Action}",
                toolId, action);
            await RefreshAsync(CancellationToken.None);
        });

        _hubConnection.Reconnecting += (ex) =>
        {
            _logger.LogWarning(ex, "SignalR reconnecting to {Url}", _dispatchHubUrl);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _logger.LogInformation(
                "SignalR reconnected: {ConnectionId}, refreshing tools",
                connectionId);
            await RefreshAsync(CancellationToken.None);
        };

        try
        {
            await _hubConnection.StartAsync(ct);
            _logger.LogInformation(
                "DynamicToolRegistry connected to SignalR hub: {Url}",
                _dispatchHubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to connect to SignalR hub: {Url}. Will retry on next tool update.",
                _dispatchHubUrl);
        }
    }

    public void Dispose()
    {
        _hubConnection?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
    }
}
