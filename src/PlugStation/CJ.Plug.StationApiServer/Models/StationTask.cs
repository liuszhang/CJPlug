namespace CJ.Plug_Aspire.StationApiService.Models;

/// <summary>
/// 图站任务记录 — 保存在本地 SQLite
/// </summary>
public class StationTask
{
    public int Id { get; set; }

    /// <summary>任务关联 ID (ToolJobCorrelationId)</summary>
    public string? CorrelationId { get; set; }

    /// <summary>插件类型 Key</summary>
    public string? PlugTypeKey { get; set; }

    /// <summary>工具名称</summary>
    public string? ToolName { get; set; }

    /// <summary>执行命令</summary>
    public string? Command { get; set; }

    /// <summary>执行模式</summary>
    public string? ExecuteMode { get; set; }

    /// <summary>任务状态: pending / running / completed / failed</summary>
    public string Status { get; set; } = "pending";

    /// <summary>结果子状态</summary>
    public string? SubStatus { get; set; }

    /// <summary>执行结果内容 (JSON)</summary>
    public string? Result { get; set; }

    /// <summary>创建时间</summary>
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>完成时间</summary>
    public string? CompletedAt { get; set; }
}
