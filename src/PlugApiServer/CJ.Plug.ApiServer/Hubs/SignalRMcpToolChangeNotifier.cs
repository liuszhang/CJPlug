using CJ.Plug.Models.MCPTools;
using Microsoft.AspNetCore.SignalR;

namespace CJ.Plug.ApiServer.Hubs;

/// <summary>
/// SignalR 实现 MCP Tool 变更通知器
/// </summary>
public class SignalRMcpToolChangeNotifier : IMcpToolChangeNotifier
{
    private readonly IHubContext<MainHub> _hubContext;

    public SignalRMcpToolChangeNotifier(IHubContext<MainHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(string toolId, string action, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("MCPToolUpdated", toolId, action, cancellationToken: ct);
    }
}
