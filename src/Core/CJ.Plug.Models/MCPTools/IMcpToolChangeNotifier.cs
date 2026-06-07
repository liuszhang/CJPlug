namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// MCP Tool 变更通知接口。
/// 模块通过此接口发出工具发布/删除/启禁用通知，宿主应用负责实现具体的广播机制（如 SignalR）。
/// </summary>
public interface IMcpToolChangeNotifier
{
    Task NotifyAsync(string toolId, string action, CancellationToken ct = default);
}
