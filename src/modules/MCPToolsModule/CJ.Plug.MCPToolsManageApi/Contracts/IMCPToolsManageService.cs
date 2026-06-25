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

    /// <summary>
    /// 读取 Trae MCP 配置文件原始 JSON 字符串。文件不存在返回 "{}"
    /// </summary>
    Task<(string content, string filePath)> PreviewTraeMcpAsync(string? traeConfigPath = null);

    /// <summary>
    /// 将用户编辑后的 JSON 内容直接覆盖写入 Trae 的 MCP 配置文件
    /// </summary>
    Task<string> ConfigureTraeMcpAsync(string configContent, string? traeConfigPath = null);

    /// <summary>
    /// 一键将固定 cj-mcpserver 配置合并写入 Trae 的 MCP 配置文件
    /// </summary>
    Task<string> EnableTraeMcpAsync(string? traeConfigPath = null);

    /// <summary>
    /// 读取 Claude Code MCP 配置文件原始 JSON 字符串。文件不存在返回 "{}"
    /// </summary>
    Task<(string content, string filePath)> PreviewClaudeMcpAsync(string? claudeConfigPath = null);

    /// <summary>
    /// 将用户编辑后的 JSON 内容直接覆盖写入 Claude Code 的 MCP 配置文件
    /// </summary>
    Task<string> ConfigureClaudeMcpAsync(string configContent, string? claudeConfigPath = null);
}
}
