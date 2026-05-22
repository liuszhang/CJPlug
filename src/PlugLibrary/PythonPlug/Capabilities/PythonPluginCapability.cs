using CJ.Plug.Models.MCPTools;

namespace PythonPlug.Capabilities;

/// <summary>
/// Python 脚本插件的能力描述 — 执行 Python 代码或脚本文件
/// </summary>
public class PythonPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "PythonPlug";
    public string Name => "Python脚本执行";

    public string Description =>
        "执行 Python 脚本代码或 .py 文件。支持标准库和已安装的第三方库。" +
        "可用于数据处理、计算、格式转换、文件操作等任务。脚本通过标准输出返回结果。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "scriptCode", Type = "String",
            Description = "Python 脚本代码（字符串形式）。与 scriptFile 二选一",
            IsRequired = false
        },
        new()
        {
            Name = "scriptFile", Type = "File",
            Description = "要执行的 .py 文件路径",
            IsRequired = false
        },
        new()
        {
            Name = "arguments", Type = "String", IsArray = true,
            Description = "传递给脚本的命令行参数列表"
        },
        new()
        {
            Name = "pythonPath", Type = "String",
            Description = "Python 解释器路径，默认使用系统 PATH 中的 python",
            DefaultValue = "python"
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "stdout", Type = "String",
            Description = "脚本的标准输出内容"
        },
        new()
        {
            Name = "stderr", Type = "String",
            Description = "脚本的错误输出内容"
        },
        new()
        {
            Name = "exitCode", Type = "Int",
            Description = "脚本退出码（0 表示成功）"
        },
    };

    public string[] Tags => new[] { "脚本", "Python", "计算", "数据处理", "格式转换" };
}
