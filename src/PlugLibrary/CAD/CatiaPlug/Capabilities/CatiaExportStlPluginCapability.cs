using CJ.Plug.Models.MCPTools;
using CatiaPlug;

namespace CatiaPlug.Capabilities;

/// <summary>
/// Catia 导出 STL 插件的能力描述 — 通过 CATIA COM 自动化将模型导出为 STL 三角网格
/// </summary>
public class CatiaExportStlPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "CatiaExportStl";
    public string Name => "Catia模型转STL";

    public string Description =>
        "通过 CATIA V5 COM 自动化打开模型并调用 STLExport 对象导出 STL 三角网格文件。" +
        "支持弦高偏差 Sag、ASCII/Binary 输出格式等参数。" +
        "适用于 3D 打印、快速原型制作、轻量化预览（配合 StlViewerPlug）等场景。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "modelFilePath", Type = "File",
            Description = "CATIA 模型文件（.CATPart / .CATProduct）的完整路径",
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
            Name = "sag", Type = "Double",
            Description = "弦高偏差 Sag（mm），控制 STL 网格精度。值越小网格越精细",
            Value = "0.1"
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

    public string[] Tags => new[] { "CATIA", "STL", "CAD", "3D打印", "模型转换", "Dassault" };
}
