namespace CJ.Plug.LlmConfigModel.Models;

/// <summary>
/// LLM 供应商（如 OpenAI、DeepSeek、Ollama、OpenRouter 等）
/// </summary>
public class LlmProvider
{
    public int Id { get; set; }

    /// <summary>供应商标识（唯一，如 "openai"、"deepseek"、"ollama"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>显示名称（如 "OpenAI"、"DeepSeek"）</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>API 基础地址（如 "https://api.openai.com/v1"）</summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>API Key</summary>
    public string? ApiKey { get; set; }

    /// <summary>供应商描述</summary>
    public string? Description { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

    /// <summary>该供应商下的模型配置列表</summary>
    public List<LlmModelConfig> ModelConfigs { get; set; } = new();
}
