using CJ.Plug.Models.MCPTools;
using NXPlug;

namespace NXPlug.Capabilities;

/// <summary>
/// Nx设置模型参数插件的能力描述 — 设置NX模型参数
/// </summary>
public class NxSetParametersPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "NXSetParametersPlug";
    public string Name => "设置NX模型参数";

    public string Description =>
        "设置NX模型参数，支持批量修改模型参数值。" +
        "适用于NX模型参数化设计、批量参数更新等场景。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "modelFilePath", Type = "String",
            Description = "NX模型文件路径",
            IsRequired = true
        },
        new()
        {
            Name = "newParameterString", Type = "String",
            Description = "新的参数字符串，格式为'参数名=参数值'，多个参数用分号分隔",
            IsRequired = true
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "result", Type = "String",
            Description = "执行结果信息"
        },
        new()
        {
            Name = "success", Type = "Boolean",
            Description = "是否执行成功"
        },
    };

    public string[] Tags => new[] { "NX", "模型", "参数", "CAD", "设计" };
}
