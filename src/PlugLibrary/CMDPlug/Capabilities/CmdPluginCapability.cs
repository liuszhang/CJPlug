using CJ.Plug.Models.MCPTools;

namespace CMDPlug.Capabilities;

/// <summary>
/// 命令行插件的能力描述 — 执行系统命令或可执行程序
/// </summary>
public class CmdPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "CMDPlug";
    public string Name => "命令行执行";

    public string Description =>
        "在系统命令行中执行任意命令或启动可执行程序。" +
        "适用于文件操作、系统管理、调用外部工具等。命令通过标准输出返回结果。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "command", Type = "String",
            Description = "要执行的命令或可执行程序路径",
            IsRequired = true
        },
        new()
        {
            Name = "arguments", Type = "String", IsArray = true,
            Description = "命令行参数列表"
        },
        new()
        {
            Name = "workingDirectory", Type = "String",
            Description = "工作目录路径"
        },
        new()
        {
            Name = "timeout", Type = "Int",
            Description = "命令超时时间（秒）",
            DefaultValue = "60"
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "stdout", Type = "String",
            Description = "命令的标准输出内容"
        },
        new()
        {
            Name = "stderr", Type = "String",
            Description = "命令的错误输出内容"
        },
        new()
        {
            Name = "exitCode", Type = "Int",
            Description = "命令退出码（0 表示成功）"
        },
    };

    public string[] Tags => new[] { "系统", "命令行", "Shell", "文件操作", "工具" };
}
