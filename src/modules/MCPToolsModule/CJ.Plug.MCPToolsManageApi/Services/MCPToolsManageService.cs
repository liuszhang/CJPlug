using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.MCPToolsManageApi.Services
{
    public class MCPToolsManageService : BaseRepositoryService<MCPTool, int>, IMCPToolsManageService
    {
        public MCPToolsManageService(MainDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<MCPTool>> GetActiveToolsAsync()
        {
            var allTools = await base.GetAllAsync();
            //var activeTools = allTools.Where(t => t.IsEnabled == true);
            //foreach (var tool in activeTools)
            //{
            //    tool.Id = tool.SourcePlugId;
            //}
            return allTools.Where(t => t.IsEnabled == true);
        }
    }
}
