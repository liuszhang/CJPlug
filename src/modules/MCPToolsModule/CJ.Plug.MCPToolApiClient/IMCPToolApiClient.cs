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

    /// <summary>
    /// 读取指定路径的 MCP 配置文件
    /// </summary>
    Task<(string content, string filePath)> GetPreviewAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将用户编辑的 JSON 覆盖写入指定路径的配置文件
    /// </summary>
    Task<string> ConfigureMcpAsync(string filePath, string configContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开 Windows 文件选择对话框，返回选中路径
    /// </summary>
    Task<string> PickFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已持久化的 MCP 配置文件路径。key: WorkBuddy / Codex / Hermes
    /// </summary>
    Task<string> GetConfigPathAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 持久化 MCP 配置文件路径。key: WorkBuddy / Codex / Hermes
    /// </summary>
    Task SaveConfigPathAsync(string key, string filePath, CancellationToken cancellationToken = default);
}
