namespace CJ.Plug.LlmConfigModel.Models;

/// <summary>
/// LLM 模型配置
/// </summary>
public class LlmModelConfig
{
    public int Id { get; set; }

    /// <summary>所属供应商 ID</summary>
    public int LlmProviderId { get; set; }

    /// <summary>模型标识名（如 "gpt-4o"、"deepseek-r1"、"qwen3:4b"）</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>模型类型：Chat / Embedding / Completion / Image</summary>
    public string ModelType { get; set; } = "Chat";

    /// <summary>最大 Token 数</summary>
    public int? MaxTokens { get; set; }

    /// <summary>温度参数 (0.0 - 2.0)</summary>
    public double? Temperature { get; set; }

    /// <summary>是否为默认模型（每种类型最多一个默认）</summary>
    public bool IsDefault { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>模型描述/备注</summary>
    public string? Description { get; set; }

    /// <summary>额外参数（JSON 格式）</summary>
    public string? ExtraParams { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
}
