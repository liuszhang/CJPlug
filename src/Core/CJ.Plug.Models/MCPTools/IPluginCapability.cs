namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 插件能力契约 —— 描述一个插件对外暴露的输入/输出，供 AI Workflow Builder 编排使用。
/// 每个插件类型需要实现此接口并注册到 CapabilityRegistry。
/// </summary>
public interface IPluginCapability
{
    /// <summary>
    /// 插件类型唯一标识（通常是 PluginTypeKey，如 "REST", "Python", "CMD"）
    /// </summary>
    string PluginTypeKey { get; }

    /// <summary>
    /// 能力名称（用户可读，如 "HTTP请求"）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 能力描述 — 会被注入 LLM 的 System Prompt
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 输入参数定义
    /// </summary>
    List<CapabilityParameter> Inputs { get; }

    /// <summary>
    /// 输出参数定义
    /// </summary>
    List<CapabilityParameter> Outputs { get; }

    /// <summary>
    /// 标签 — 用于 LLM 语义匹配（如 ["网络", "API", "HTTP"]）
    /// </summary>
    string[] Tags { get; }
}

/// <summary>
/// 插件能力的单个参数描述
/// </summary>
public class CapabilityParameter
{
    /// <summary>参数名称</summary>
    public string Name { get; set; } = "";

    /// <summary>参数类型（VariableTypeEnum 的字符串形式：String/Int/Float/Bool/File）</summary>
    public string Type { get; set; } = "String";

    /// <summary>参数描述</summary>
    public string Description { get; set; } = "";

    /// <summary>是否必填</summary>
    public bool IsRequired { get; set; }

    /// <summary>是否是数组</summary>
    public bool IsArray { get; set; }

    /// <summary>默认值</summary>
    public string? Value { get; set; }
}
