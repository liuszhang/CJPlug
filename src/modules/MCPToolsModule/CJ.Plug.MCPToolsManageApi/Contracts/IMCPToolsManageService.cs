using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.MCPToolsManageApi.Contracts
{
    public interface IMCPToolsManageService : IBaseRepositoryService<MCPTool, int>
    {
        Task<IEnumerable<MCPTool>> GetActiveToolsAsync();
    }
}
