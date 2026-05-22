using CJ.Plug.Models.MCPTools;

namespace AiAgentPlug.Capabilities;

/// <summary>
/// AI 代理插件的能力描述 — 调用 LLM 进行文本生成、分析、转换
/// </summary>
public class AiAgentPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "AiAgentPlug";
    public string Name => "AI大模型调用";

    public string Description =>
        "调用大语言模型（LLM）进行文本生成、内容分析、格式转换、翻译、摘要等任务。" +
        "支持自定义 System Prompt 和 User Prompt，可配置温度、最大长度等参数。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "systemPrompt", Type = "String",
            Description = "系统提示词，定义 AI 的角色和行为",
            IsRequired = true
        },
        new()
        {
            Name = "userPrompt", Type = "String",
            Description = "用户提示词，具体的任务描述或输入内容",
            IsRequired = true
        },
        new()
        {
            Name = "temperature", Type = "Float",
            Description = "生成温度（0-2），越高越随机，越低越确定",
            DefaultValue = "0.7"
        },
        new()
        {
            Name = "maxTokens", Type = "Int",
            Description = "最大生成 Token 数",
            DefaultValue = "4096"
        },
        new()
        {
            Name = "model", Type = "String",
            Description = "使用的模型名称，默认使用系统配置的模型"
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "response", Type = "String",
            Description = "AI 生成的响应文本"
        },
        new()
        {
            Name = "usageTokens", Type = "Int",
            Description = "消耗的 Token 数量"
        },
    };

    public string[] Tags => new[] { "AI", "LLM", "大模型", "文本生成", "分析", "翻译", "摘要" };
}
