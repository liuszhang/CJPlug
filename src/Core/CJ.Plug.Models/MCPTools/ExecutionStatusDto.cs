namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 执行状态响应 —— MCP 客户端轮询使用
/// </summary>
public class ExecutionStatusDto
{
    public string? WorkflowInstanceId { get; set; }
    public string? Status { get; set; }
    public string? SubStatus { get; set; }
    public string? ResultMessage { get; set; }
    public string? ResultString { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}
