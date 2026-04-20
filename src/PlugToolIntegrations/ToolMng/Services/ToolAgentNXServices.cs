using System.Diagnostics;
using ToolMng.Contracts;

namespace ToolMng.Services
{
    public class ToolAgentNXServices : IToolAgentNXServices
    {
        /// <summary>
        /// 导出NX模型至STL文件
        /// </summary>
        /// <param name="NXFilePath"></param>
        /// <param name="StlFilePath"></param>
        /// <returns></returns>
        public async Task<string?> ExportNXFileToStlFileAsync(string NXFilePath, string StlFilePath)
        {
            try
            {
                //新模型轻量化处理与展示
                //string stlExePath = @"D:\\99_Pro\\MyElsa\\NXConsoleApp1\\bin\\Debug\\net4.8\\NXToStl.exe";
                string rootFile = NXFilePath;
                string sourceFile = StlFilePath;
                //接下来调用NX工具读取模型参数
                // 创建一个新的进程启动信息               
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    //FileName = Directory.GetCurrentDirectory() + "\\toolAgentFiles\\Exe\\NXToStl\\NXToStl.exe",
                    FileName = @"D:\\99_Pro\\CJ.Plug\\ToolAgent\\NXToStl\\bin\\Debug\\net4.8\\NXToStl.exe",
                    Arguments = $"{rootFile} {sourceFile}",
                    RedirectStandardOutput = true, // 设置重定向标准输出
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Console.WriteLine(startInfo.Arguments);
                Process process1 = new Process();
                process1.StartInfo = startInfo;
                process1.Start();
                if (!process1.HasExited)
                {
                    StreamReader reader = process1.StandardOutput;
                    var output = await reader.ReadToEndAsync();
                    Console.WriteLine(output);
                }
                process1.WaitForExit();
                Console.WriteLine("模型轻量化数据导出完成");
                if (File.Exists(sourceFile)) 
                {
                    File.Delete(rootFile);

                    return sourceFile;
                }
                else
                {
                    Console.WriteLine("模型轻量化数据导出失败！");
                    return null;
                }
                

            }
            catch (FormatException fe)
            {
                Console.WriteLine($"Base64解码错误: {fe.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(StatusCodes.Status500InternalServerError.ToString() + $"失败: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 获取NX模型参数值
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>参数值表</returns>
        public async Task<string?> GetNXParametersFromNXFileAsync(string filePath)
        {
            try
            {                
                //接下来调用NX工具读取模型参数
                // 创建一个新的进程启动信息               
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @"D:\99_Pro\CJ.Plug\ToolAgent\NXGetParameters\bin\Debug\net4.8\NXGetParameters.exe",
                    //FileName = Directory.GetCurrentDirectory()+ "\\toolAgentFiles\\Exe\\NXGetParameters\\NXGetParameters.exe",

                    Arguments = $"{filePath}",
                    RedirectStandardOutput = true, // 设置重定向标准输出
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Console.WriteLine(startInfo.FileName);
                // 创建进程
                Process process = new Process();
                // 关联启动信息
                process.StartInfo = startInfo;
                // 启动进程
                process.Start();
                //string output = await process.StandardOutput.ReadToEndAsync();
                string output = "";
                // 确保在进程开始之后再读取输出
                if (!process.HasExited)
                {
                    StreamReader reader = process.StandardOutput;
                    output = await reader.ReadToEndAsync();
                    Console.WriteLine(output);
                }

                // 等待进程自然结束，这句是可选的，取决于你是否需要等待脚本执行完毕
                process.WaitForExit();

                Console.WriteLine("NX脚本已执行完毕。");
                File.Delete(filePath);
                //System.IO.File.Delete(bat);
                return output;


            }
            catch (FormatException fe)
            {
                Console.WriteLine($"Base64解码错误: {fe.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(StatusCodes.Status500InternalServerError.ToString() + $"文件保存失败: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 设置NX文件的参数值
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="newParameters"></param>
        /// <returns></returns>
        public async Task<bool> SetNXParametersToNXFileAsync(string filePath, string newParameters)
        {
            try
            {                
                //接下来调用NX工具设置模型参数
                // 创建一个新的进程启动信息               
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @"D:\99_Pro\CJ.Plug\ToolAgent\NXUpdateParameters\bin\Debug\net4.8\NXUpdateParameters.exe",
                    Arguments = $"{filePath} {newParameters}",
                    RedirectStandardOutput = true, // 设置重定向标准输出
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 创建进程
                Process process = new Process();
                Console.WriteLine(startInfo.Arguments);
                // 关联启动信息
                process.StartInfo = startInfo;
                // 启动进程
                process.Start();
                //string output = await process.StandardOutput.ReadToEndAsync();
                string output = "";
                // 确保在进程开始之后再读取输出
                if (!process.HasExited)
                {
                    StreamReader reader = process.StandardOutput;
                    output = await reader.ReadToEndAsync();
                    Console.WriteLine(output);
                }

                // 等待进程自然结束，这句是可选的，取决于你是否需要等待脚本执行完毕
                process.WaitForExit();

                Console.WriteLine("NX更新参数脚本已执行完毕。");
                //System.IO.File.Delete(bat);
                return true;


            }
            catch (FormatException fe)
            {
                Console.WriteLine($"Base64解码错误: {fe.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(StatusCodes.Status500InternalServerError.ToString() + $"文件保存失败: {ex.Message}");
                return false;
            }
        }
    }
}
