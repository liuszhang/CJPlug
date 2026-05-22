using System.Net.Http.Json;
using CJ.Plug.Models.MCPTools;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.McpServer.Services;

/// <summary>
/// 动态工作流 Tool 注册器 — 
/// 从 DispatchServer 拉取已发布工作流列表，缓存供 WorkflowTools 使用。
/// 后续升级为每个工作流独立注册 MCP Tool 时，本类将负责构造 McpServerTool 列表。
/// </summary>
public class DynamicToolRegistry
{
    private readonly HttpClient _dispatchClient;
    private readonly ILogger<DynamicToolRegistry> _logger;
    private List<PublishedWorkflowDto> _cachedWorkflows = new();

    public DynamicToolRegistry(
        IHttpClientFactory httpClientFactory,
        ILogger<DynamicToolRegistry> logger)
    {
        _dispatchClient = httpClientFactory.CreateClient("DispatchServer");
        _logger = logger;
    }

    /// <summary>
    /// 获取已发布工作流列表（从缓存）
    /// </summary>
    public IReadOnlyList<PublishedWorkflowDto> GetWorkflows() => _cachedWorkflows.AsReadOnly();

    /// <summary>
    /// 从 DispatchServer 刷新工作流缓存
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            _cachedWorkflows = await _dispatchClient
                .GetFromJsonAsync<List<PublishedWorkflowDto>>(
                    "/api/mcptools/getPublishedWorkflows",
                    cancellationToken: ct) ?? new();

            _logger.LogInformation(
                "DynamicToolRegistry refreshed: {Count} published workflows",
                _cachedWorkflows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to refresh tools from DispatchServer. Using {Count} cached.",
                _cachedWorkflows.Count);
        }
    }
}
