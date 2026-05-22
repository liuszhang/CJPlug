using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.MCPToolsManageApi.Contracts
{
public interface IMCPToolsManageService : IBaseRepositoryService<MCPTool, int>
{
    Task<IEnumerable<MCPTool>> GetActiveToolsAsync();

    /// <summary>
    /// 发布工作流为 MCP Tool：保存 Tool 记录并创建 Use 类型 PDZ 作为参数模板
    /// </summary>
    Task<MCPTool> PublishToolAsync(MCPTool tool);

    /// <summary>
    /// 获取所有已发布为 MCP Tool 的工作流，包含入口参数
    /// </summary>
    Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync();
}
}
