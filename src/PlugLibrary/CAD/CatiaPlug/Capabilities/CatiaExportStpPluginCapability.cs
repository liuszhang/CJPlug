using CJ.Plug.Models.MCPTools;
using CatiaPlug;

namespace CatiaPlug.Capabilities;

/// <summary>
/// Catia 导出 STP 插件的能力描述 — 通过 CATIA COM 自动化将模型导出为 STEP 格式
/// </summary>
public class CatiaExportStpPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "CatiaExportStp";
    public string Name => "Catia模型转STP";

    public string Description =>
        "通过 CATIA V5 COM 自动化打开模型并调用 Document.ExportData(path, \"stp\")" +
        "将 CATPart/CATProduct 导出为 STEP（.stp）文件。" +
        "适用于跨 CAD 平台模型交换、下游工艺/仿真等场景。";

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
            Name = "stpOutputPath", Type = "String",
            Description = "STP 输出文件的完整路径",
            IsRequired = true
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "stpFilePath", Type = "String",
            Description = "生成的 STP 文件的完整路径"
        },
        new()
        {
            Name = "resultString", Type = "String",
            Description = "执行结果消息（成功/失败/异常信息）"
        },
    };

    public string[] Tags => new[] { "CATIA", "STP", "STEP", "CAD", "模型转换", "Dassault" };
}
