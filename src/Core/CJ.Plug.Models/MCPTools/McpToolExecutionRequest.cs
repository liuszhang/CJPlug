namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// MCP Tool 统一执行请求（支持工作流和单插头）
/// </summary>
public class McpToolExecutionRequest
{
    public string? PlugDefinitionId { get; set; }
    /// <summary>工具类型: "Workflow" 或 "Plugin"</summary>
    public string ToolType { get; set; } = "Workflow";
    public List<PlugVariableData> InputVariables { get; set; } = new();
}
