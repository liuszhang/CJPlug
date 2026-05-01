using System.Text.Json.Serialization;

namespace StationSettingUI.Models;

/// <summary>
/// 图站任务（对应 StationApiServer 返回的 JSON）
/// </summary>
public class StationTaskInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("plugTypeKey")]
    public string? PlugTypeKey { get; set; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; set; }

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("executeMode")]
    public string? ExecuteMode { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("subStatus")]
    public string? SubStatus { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    /// <summary>状态显示文本</summary>
    public string StatusDisplay => Status switch
    {
        "pending" => "待执行",
        "running" => "执行中",
        "completed" => "已完成",
        "failed" => "失败",
        _ => Status
    };

    /// <summary>耗时</summary>
    public string? Duration
    {
        get
        {
            if (CreatedAt == null) return null;
            if (!DateTime.TryParse(CreatedAt, out var created)) return null;

            var end = CompletedAt != null && DateTime.TryParse(CompletedAt, out var c) ? c : DateTime.Now;
            var span = end - created;
            return span.TotalSeconds < 60
                ? $"{span.TotalSeconds:F0}s"
                : $"{span.TotalMinutes:F1}m";
        }
    }
}
