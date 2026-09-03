using CJ.Plug.Models.MCPTools;
using CatiaPlug;

namespace CatiaPlug.Capabilities;

/// <summary>
/// Catia 获取模型参数插件的能力描述 — 通过 CATIA COM 自动化读取模型参数
/// </summary>
public class CatiaGetParametersPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "CatiaGetParameters";
    public string Name => "获取Catia模型参数";

    public string Description =>
        "通过 CATIA V5 COM 自动化打开 CATPart/CATProduct 模型，遍历 Parameters 集合，" +
        "读取 Length/Angle/Mass/用户参数等并以 name=value 序列化输出。" +
        "适用于 CATIA 模型参数化设计、参数盘点等场景。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "modelFilePath", Type = "File",
            Description = "CATIA 模型文件（.CATPart / .CATProduct）的完整路径",
            IsRequired = true
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "resultString", Type = "String",
            Description = "读取到的参数列表（name=value，空格分隔）"
        },
    };

    public string[] Tags => new[] { "CATIA", "参数", "CAD", "读取", "Dassault" };
}
