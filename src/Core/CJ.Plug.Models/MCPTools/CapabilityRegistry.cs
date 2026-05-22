using System.Text;

namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 插件能力注册表 — 收集所有 IPluginCapability 实现，
/// 为 AI Workflow Builder 的 LLM Prompt 提供插件能力上下文。
/// </summary>
public class CapabilityRegistry
{
    private readonly List<IPluginCapability> _capabilities = new();

    /// <summary>
    /// 注册插件能力
    /// </summary>
    public void Register(IPluginCapability capability)
    {
        var existing = _capabilities.FindIndex(c =>
            c.PluginTypeKey == capability.PluginTypeKey);
        if (existing >= 0)
        {
            _capabilities[existing] = capability; // 覆盖
        }
        else
        {
            _capabilities.Add(capability);
        }
    }

    /// <summary>
    /// 批量注册
    /// </summary>
    public void RegisterRange(IEnumerable<IPluginCapability> capabilities)
    {
        foreach (var c in capabilities)
            Register(c);
    }

    /// <summary>
    /// 根据 PluginTypeKey 查找
    /// </summary>
    public IPluginCapability? Get(string pluginTypeKey) =>
        _capabilities.FirstOrDefault(c =>
            c.PluginTypeKey.Equals(pluginTypeKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 获取所有已注册的能力
    /// </summary>
    public IReadOnlyList<IPluginCapability> GetAll() => _capabilities.AsReadOnly();

    /// <summary>
    /// 根据标签过滤
    /// </summary>
    public IEnumerable<IPluginCapability> FilterByTags(params string[] tags) =>
        _capabilities.Where(c => c.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)));

    /// <summary>
    /// 生成注入 LLM System Prompt 的插件能力描述文本。
    /// 限制总长度以避免超出 LLM context。
    /// </summary>
    public string ToPromptContext(int maxLength = 4000)
    {
        var sb = new StringBuilder();
        sb.AppendLine("以下是可用的插件（积木）列表，每个插件都有明确的输入和输出参数类型：");
        sb.AppendLine();

        foreach (var cap in _capabilities)
        {
            var entry = FormatCapability(cap);
            if (sb.Length + entry.Length > maxLength)
            {
                sb.AppendLine($"(剩余 {_capabilities.Count - _capabilities.IndexOf(cap)} 个插件因长度限制未列出)");
                break;
            }
            sb.Append(entry);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 格式化单个插件能力为 LLM Prompt 文本
    /// </summary>
    public static string FormatCapability(IPluginCapability cap)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### {cap.Name} [{string.Join(", ", cap.Tags)}]");
        sb.AppendLine($"类型标识: {cap.PluginTypeKey}");
        sb.AppendLine($"描述: {cap.Description}");

        // 输入参数
        if (cap.Inputs.Count > 0)
        {
            sb.AppendLine("输入参数:");
            foreach (var p in cap.Inputs)
            {
                var req = p.IsRequired ? "必填" : "可选";
                var arr = p.IsArray ? "[]" : "";
                var def = !string.IsNullOrEmpty(p.DefaultValue) ? $" (默认: {p.DefaultValue})" : "";
                sb.AppendLine($"  - {p.Name}: {p.Type}{arr} [{req}] — {p.Description}{def}");
            }
        }

        // 输出参数
        if (cap.Outputs.Count > 0)
        {
            sb.AppendLine("输出参数:");
            foreach (var p in cap.Outputs)
            {
                var arr = p.IsArray ? "[]" : "";
                sb.AppendLine($"  - {p.Name}: {p.Type}{arr} — {p.Description}");
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }
}
