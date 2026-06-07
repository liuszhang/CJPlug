using CJ.Plug.Models.MCPTools;
using NXPlug;

namespace NXPlug.Capabilities;

/// <summary>
/// NX 模型转 STL 插件的能力描述 — 通过 NX Open API 将 .prt 文件导出为 .stl 格式
/// </summary>
public class NXToStlPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "NXToStl";
    public string Name => "NX模型转STL";

    public string Description =>
        "将 Siemens NX 的 .prt 部件文件导出为 STL 三角网格文件。" +
        "支持自定义弦高公差、邻接公差和法线自动生成。" +
        "适用于 3D 打印、快速原型制作和跨平台模型交换。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "prtFilePath", Type = "File",
            Description = "NX 部件文件（.prt）的完整路径",
            IsRequired = true
        },
        new()
        {
            Name = "stlOutputPath", Type = "String",
            Description = "STL 输出文件的完整路径",
            IsRequired = true
        },
        new()
        {
            Name = "chordalTol", Type = "Double",
            Description = "弦高公差（mm），控制 STL 网格精度。值越小网格越精细",
            DefaultValue = "0.08"
        },
        new()
        {
            Name = "adjacencyTol", Type = "Double",
            Description = "邻接公差（mm），控制相邻面的缝合精度",
            DefaultValue = "0.08"
        },
        new()
        {
            Name = "autoNormalGen", Type = "Bool",
            Description = "是否自动生成面法线",
            DefaultValue = "true"
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "stlFilePath", Type = "String",
            Description = "生成的 STL 文件的完整路径"
        },
        new()
        {
            Name = "resultString", Type = "String",
            Description = "执行结果消息（成功/失败/异常信息）"
        },
    };

    public string[] Tags => new[] { "NX", "STL", "CAD", "3D打印", "模型转换", "Siemens" };
}
