using CJ.Plug.Models.MCPTools;

namespace CJ.Plug.MCPToolApiClient;

public interface IMCPToolApiClient
{
    Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default);
    Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
    Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default);
    Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default);
    Task NotifyRefreshAsync(CancellationToken cancellationToken = default);
    Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 Trae MCP 配置文件当前内容
    /// </summary>
    Task<(string content, string filePath)> GetTraePreviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 将用户编辑的 JSON 内容覆盖写入 Trae 配置文件，返回成功消息
    /// </summary>
    Task<string> ConfigureTraeMcpAsync(string configContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 一键将固定 cj-mcpserver 配置合并写入 Trae 配置文件，返回成功消息
    /// </summary>
    Task<string> EnableTraeMcpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 Claude Code MCP 配置文件当前内容
    /// </summary>
    Task<(string content, string filePath)> GetClaudePreviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 将用户编辑的 JSON 内容覆盖写入 Claude Code 配置文件，返回成功消息
    /// </summary>
    Task<string> ConfigureClaudeMcpAsync(string configContent, CancellationToken cancellationToken = default);
}
