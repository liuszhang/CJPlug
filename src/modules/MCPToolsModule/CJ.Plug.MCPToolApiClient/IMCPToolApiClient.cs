using CJ.Plug.Models.MCPTools;

namespace CJ.Plug.MCPToolApiClient;

public interface IMCPToolApiClient
{
    Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default);
    Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
    Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default);
    Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
    Task NotifyRefreshAsync(CancellationToken cancellationToken = default);
}
