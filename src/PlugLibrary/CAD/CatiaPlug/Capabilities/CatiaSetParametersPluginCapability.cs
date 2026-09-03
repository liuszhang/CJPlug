using CJ.Plug.Models.MCPTools;
using CatiaPlug;

namespace CatiaPlug.Capabilities;

/// <summary>
/// Catia 设置模型参数插件的能力描述 — 通过 CATIA COM 自动化批量更新模型参数
/// </summary>
public class CatiaSetParametersPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "CatiaSetParameters";
    public string Name => "设置Catia模型参数";

    public string Description =>
        "通过 CATIA V5 COM 自动化打开模型并按 key=value 批量更新参数值，" +
        "执行 part.Update() 与 part.Save() 持久化。" +
        "适用于 CATIA 模型参数化设计、批量参数更新等场景。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "modelFilePath", Type = "String",
            Description = "CATIA 模型文件（.CATPart / .CATProduct）的完整路径",
            IsRequired = true
        },
        new()
        {
            Name = "newParameterString", Type = "String",
            Description = "新的参数字符串，格式为'参数名=参数值'，多个参数用逗号分隔",
            IsRequired = true
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "resultString", Type = "String",
            Description = "执行结果消息（成功/失败/异常信息）"
        },
    };

    public string[] Tags => new[] { "CATIA", "模型", "参数", "CAD", "设计" };
}
