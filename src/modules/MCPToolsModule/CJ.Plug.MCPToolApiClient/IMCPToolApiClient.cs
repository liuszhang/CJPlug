using CJ.Plug.Models.Job;
using CJ.Plug.Models.MCPTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.MCPToolApiClient
{
    public interface IMCPToolApiClient
    {
        Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default);
        Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
        Task DeleteMCPToolAsync(int toolId,CancellationToken cancellationToken = default);
        Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
    }
}
