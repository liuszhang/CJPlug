using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using Serilog;
using System.Text.RegularExpressions;

namespace CJ.Plug.Models.Services
{
    /// <summary>
    /// 一些通用方法，用来处理参数中的一些模式匹配和替换操作
    /// </summary>
    public class ParameterGenerator
    {

        /// <summary>
        /// 通过CommandLineShema中的配置生成命令行参数
        /// 将命令行中的模式匹配（[]）用实际参数值替换
        /// </summary>
        /// <param name="plug"></param>
        /// <returns></returns>
        public static async Task<string?> EvalCommandLine(Plug.Plug plug)
        {
            //var commandLine = plug.GetPlugSettings().GetSetting(PlugSettingKey.CommandLineShema.ToString());
            var commandLine = plug.ToolCommandLineShema;
            //var commandLine = plug.GetVariableValue("CMDCommand");
            if (string.IsNullOrEmpty(commandLine))
            {
                return null;
            }
            var commandList = commandLine?.Split(" ");
            foreach (var c in commandList)
            {
                if (c.Contains('[') && c.Contains(']') &&c!="[ToolPath]")
                {
                    var rawName = c.Trim('[').Trim(']');
                    // 解析 [Name:Type] 格式，提取 Name 部分作为查找键
                    var colonIdx = rawName.IndexOf(':');
                    var lookupName = colonIdx > 0 ? rawName.Substring(0, colonIdx) : rawName;
                    var value = plug.PlugVariables.Find(p => p.Name == lookupName)?.Value;
                    commandLine = commandLine.Replace(c, value);
                    Console.WriteLine($"replace {c} with {value}");
                }
            }
            return commandLine;
        }

        /// <summary>
        /// 将参数中的模式匹配（[]）用实际参数值替换
        /// </summary>
        /// <param name="plug"></param>
        /// <returns></returns>
        public static async Task<Plug.Plug?> EvalPlugVariable(Plug.Plug plug)
        {
            foreach (var p in plug.PlugVariables)
            {
                var commandList = p.Value?.Split(" ");
                if (string.IsNullOrEmpty(p.Value) || commandList == null)
                {
                    continue;
                }
                foreach (var c in commandList)
                {
                    if (c.Contains('[') && c.Contains(']'))
                    {
                        var value = plug.PlugVariables.Find(p => p.Name == (c.Trim('[').Trim(']'))).Value;
                        p.Value = p.Value.Replace(c, value);
                        Console.WriteLine($"replace {c} with {value}");
                    }
                }                
            }
            return plug;
        }

        public static (string, string) ExtractNumberAndText(string input)
        {
            // 定义正则表达式模式，匹配 ${数字:任意字符}
            string pattern = @"\${(\d+):([^}]*)}";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // 提取匹配的数字部分
                string numberString = match.Groups[1].Value;
                string textString = match.Groups[2].Value;
                return (numberString, textString);
                //if (int.TryParse(numberString, out string number))
                //{
                //    return (numberString, textString);
                //}
            }

            throw new FormatException("Input string is not in the expected format.");
        }

        /// <summary>
        /// 服务端：将 MainFileServerPathRoot 和工具的相对路径拼接成完整的工具路径，区分绝对路径和相对路径
        /// </summary>
        /// <param name="toolPath">Tool.ToolPath，存储相对路径</param>
        /// <returns></returns>
        public static string? GenerateToolPath(string toolPath)
        {
            if (string.IsNullOrEmpty(toolPath))
            {
                return null;
            }
            //判断tool.ToolPath是绝对路径或相对路径
            if (toolPath.StartsWith("/") || toolPath.StartsWith("\\") || toolPath.Contains(":"))
            {
                return toolPath;
            }
            //相对路径，基于 MainFileServerPathRoot 拼接
            else
            {
                return Path.Combine(GlobalData.MainFileServerPathRoot, toolPath);
            }
        }

        /// <summary>
        /// 将命令行中的模式匹配（[]）用实际参数值替换
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="inputVariables"></param>
        /// <returns></returns>
        public static string? EvalCommandLine(string commandLine, List<PlugVariableData> inputVariables)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return commandLine;
            }
            try
            {
                //Log.Information("CommandLine before replace: " + commandLine);
                var commandList = commandLine?.Split(" ");
                foreach (var c in commandList)
                {
                    if (c.Contains('[') && c.Contains(']') && c != "[ToolPath]")
                    {
                        var rawName = c.TrimStart('[').TrimEnd(']');
                        // 解析 [Name:Type] 格式，提取 Name 部分作为查找键
                        var colonIdx = rawName.IndexOf(':');
                        var lookupName = colonIdx > 0 ? rawName.Substring(0, colonIdx) : rawName;
                        var value = inputVariables.Find(p => p.Name == lookupName)?.Value;
                        commandLine = commandLine.Replace(c, value);
                        //Log.Information($"replace {c} with {value}");
                    }
                }
                //处理ToolPath
                if (!commandLine.Contains("[ToolPath]"))
                {
                    Log.Warning("请注意工具命令行配置中不包含工具执行路径，请检查确认。");
                    //commandLine = "[ToolPath] " + commandLine;
                }
                //Log.Information("CommandLine after replace: " + commandLine);
                return commandLine;
            }
            catch (Exception ex)
            {
                CLog.Error($"命令行参数处理替换失败：{ex.Message}");
                CLog.Error($"{ex.StackTrace}");
                return null;
            }

        }
    }
}
