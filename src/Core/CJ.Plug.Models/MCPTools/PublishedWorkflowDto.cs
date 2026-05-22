namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 已发布为 MCP Tool 的工作流信息 —— 包含工作流元数据和入口参数
/// McpServer 使用此数据动态注册 MCP Tool
/// </summary>
public class PublishedWorkflowDto
{
    /// <summary>工作流定义ID（= SourcePlugId，与 executePlugByDefinitionId 一致）</summary>
    public string? WorkflowDefinitionId { get; set; }

    /// <summary>MCP Tool 名称（工作流名称）</summary>
    public string? Name { get; set; }

    /// <summary>MCP Tool 描述</summary>
    public string? Description { get; set; }

    /// <summary>工作流入口参数列表（IsInput = true 的 PDZ 变量）</summary>
    public List<EntryVariableDto> EntryVariables { get; set; } = new();
}

/// <summary>
/// 工作流入口参数 —— 从 BaseVariable 投影，用于生成 MCP inputSchema
/// </summary>
public class EntryVariableDto
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsArray { get; set; }
    public string? DefaultValue { get; set; }
}
