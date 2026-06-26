namespace CJ.Plug.LlmConfigModel.Models;

/// <summary>
/// MCP 配置文件路径持久化（WorkBuddy / Codex / Hermes）
/// </summary>
public class McpConfigPath
{
    /// <summary>主键："WorkBuddy" / "Codex" / "Hermes"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>配置文件绝对路径</summary>
    public string FilePath { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
}
