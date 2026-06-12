using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.StationAgent.Contracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.StationAgent.ToolAgents
{
    public class DefaultCmdExecute
    {
        public static async Task<(string, JobSubStatus)> ExecuteCMD(PlugExecutionRequest stationExecutionRequest, StationApiClient StationApiClient)
        {
            string ResultString = "";
            var command = stationExecutionRequest.RequestCommand;

            //await ApiService.SendLog("ToolPath:" + stationExecutionRequest.ToolFullPath);
            await StationApiClient.SendLog("Command:" + command);

            var workDirectory = string.IsNullOrEmpty(stationExecutionRequest.ToolFullPath) ? null : new FileInfo(stationExecutionRequest.ToolFullPath).DirectoryName;
            //Console.WriteLine("WorkingDirectory:" + workDirectory);
            // 创建 ProcessStartInfo 对象
            var startInfo = new ProcessStartInfo
            {
                //FileName = stationExecutionRequest.ToolFullPath ?? "C:\\Windows\\System32\\cmd.exe", // 要启动的工具名称                                                                 
                //FileName = "C:\\Windows\\System32\\cmd.exe", // 要启动的工具名称                                                                 
                FileName = "cmd.exe", // 要启动的工具名称                                                                 
                Arguments = $"/c {command}", // /c参数表示执行命令后关闭CMD                                                          
                WorkingDirectory = workDirectory, // 设置工作目录，使 exe 能找到同目录下的 dll/config 等依赖
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true // 不创建 CMD 窗口（子进程如果是 GUI 程序仍会显示自己的窗口）
            };
            try
            {
                // 创建 Process 对象
                using (var process = new Process { StartInfo = startInfo })
                {
                    // 订阅输出数据接收事件
                    process.OutputDataReceived += async (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            //Console.WriteLine("Output: " + args.Data);
                            //tmp += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + args.Data + "\n";
                            ResultString += args.Data + "\n";
                            await StationApiClient.SendLog(args.Data);
                        }
                    };

                    // 订阅错误数据接收事件
                    process.ErrorDataReceived += async (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            //Console.WriteLine("Error: " + args.Data);
                            await StationApiClient.SendLog(args.Data);
                        }
                    };

                    // 启动进程
                    process.Start();

                    // 开始异步读取输出和错误流
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 等待进程退出：优先使用 InputVariables 中的 ExecutionTimeout，默认 300 秒（5 分钟）
                    var timeoutVar = stationExecutionRequest.InputVariables
                        ?.FirstOrDefault(v => v.Name == "ExecutionTimeout")?.Value;
                    int timeoutMs = 60_000; // 默认 60 秒
                    if (!string.IsNullOrEmpty(timeoutVar) && int.TryParse(timeoutVar, out var parsed))
                        timeoutMs = parsed * 1000;
                    process.WaitForExit(timeoutMs);

                    // 输出进程退出代码
                    //Console.WriteLine("Exit Code: " + process.ExitCode);

                    await StationApiClient.SendLog("CMD执行完成。");
                }
            }
            catch (Exception e)
            {
                await StationApiClient.SendLog("启动工具失败："+e.Message);                
                return (ResultString, JobSubStatus.出错);
            }


            return (ResultString, JobSubStatus.图站执行完成);
        }

    }
}
