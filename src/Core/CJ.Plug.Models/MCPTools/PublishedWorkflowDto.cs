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

    /// <summary>工具类型: "Workflow" 或 "Plugin"</summary>
    public string? ToolType { get; set; } = "Workflow";

    /// <summary>
    /// 是否危险操作（默认 false = 可直接执行）。
    /// <para>
    /// 由<b>发布侧显式声明</b>：取值来源是 <see cref="MCPTool.ToolSettingsJson"/> 里的
    /// <c>"isDangerous": true</c>（不改表结构，走既有的自由 JSON 列）。
    /// 消费方是 CJCore 侧的 <c>PlugMcpToolExecutor.IsDangerous</c> —— 为 true 时 Agent 执行前需要二次确认。
    /// </para>
    /// <para>
    /// ⚠ 历史坑：该字段曾经不存在，执行器把 <c>IsDangerous</c> 硬编码为 <c>false</c>，
    /// 导致"启动真实设备 / 下发工单"一类工作流经 A2A 工具面可直接执行、绕过人工确认。
    /// </para>
    /// </summary>
    public bool IsDangerous { get; set; }
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
    public string? Value { get; set; }
}
