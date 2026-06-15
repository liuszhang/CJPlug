namespace CJ.Plug.LlmConfigModel.Models;

/// <summary>
/// MCP Server 连接配置
/// </summary>
public class McpServerConfig
{
    public int Id { get; set; }

    /// <summary>是否启用 MCP Server 连接</summary>
    public bool IsEnabled { get; set; }

    /// <summary>MCP Server 连接字符串</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>描述/备注</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
}
