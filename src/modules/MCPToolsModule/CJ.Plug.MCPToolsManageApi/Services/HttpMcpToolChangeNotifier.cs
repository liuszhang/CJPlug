using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.MCPToolsManageApi.Services;

/// <summary>
/// 通过 HTTP 调用 DispatchServer 的 API 端点触发 SignalR 广播
/// </summary>
public class HttpMcpToolChangeNotifier : IMcpToolChangeNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpMcpToolChangeNotifier> _logger;

    public HttpMcpToolChangeNotifier(IHttpClientFactory httpClientFactory, ILogger<HttpMcpToolChangeNotifier> logger)
    {
        _httpClient = httpClientFactory.CreateClient("DispatchServer");
        _logger = logger;
    }

    public async Task NotifyAsync(string toolId, string action, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/dispatch/notifyMcpToolUpdated?toolId={Uri.EscapeDataString(toolId)}&action={Uri.EscapeDataString(action)}";
            var response = await _httpClient.PostAsync(url, null, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("MCP Tool change notified: {ToolId}, {Action}", toolId, action);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify MCP Tool change: {ToolId}, {Action}", toolId, action);
        }
    }
}
