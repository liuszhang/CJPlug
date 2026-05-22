using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using Serilog;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CJ.Plug_Aspire.StationApiService.Services.ToolActionExecute
{
    public class ExecuteActions
    {
        /// <summary>
        /// 异步方式调用StationAgent执行命令
        /// </summary>
        /// <param name="stationExecutionRequest"></param>
        /// <returns></returns>
        public static async Task<int> InvokeStationAgentAsync(PlugExecutionRequest stationExecutionRequest)
        {

            var workDirectory = string.IsNullOrEmpty(stationExecutionRequest.ToolFullPath) ? null : new FileInfo(stationExecutionRequest.ToolFullPath).DirectoryName;
            //Log.Information("WorkingDirectory:" + workDirectory);
            //编码为 Base64 字符串，以解决命令行参数中引号的冲突问题
            var args = JsonSerializer.Serialize(stationExecutionRequest);
            string base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes(args));

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //Log.Information("BaseDirectory:" + baseDirectory);

            string combinedPath = Path.Combine(baseDirectory, @"..\..\..\CJ.Plug.StationAgent\Debug\net10.0\CJ.Plug.StationAgent.exe");
            string targetExePath = Path.GetFullPath(combinedPath);
            //Log.Information("TargetExePath:" + targetExePath);
            // 创建 ProcessStartInfo 对象
            var startInfo = new ProcessStartInfo
            {
                //FileName = @"D:\Pro\CJ.Plug-Aspire\PlugStation\CJ.Plug.StationAgent\bin\Debug\net9.0\\CJ.Plug.StationAgent.exe",
                //FileName = @"D:\99_Pro\CJ.Plug-Aspire\PlugStation\CJ.Plug.StationAgent\bin\Debug\net9.0\CJ.Plug.StationAgent.exe",
                FileName = targetExePath,
                Arguments = base64Json, // 工具的命令行参数                                                                          
                //WorkingDirectory = workDirectory, // 设置工作目录，可根据实际情况修改
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                //UseShellExecute = false,
                //CreateNoWindow = false // 不创建窗口
            };
            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    return process.Id;
                }
            }
            catch (Exception e)
            {
                CLog.Error(e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 同步方式调用StationAgent，等待结束并获取结果
        /// </summary>
        /// <param name="stationExecutionRequest"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultData> InvokeStationAgent(PlugExecutionRequest stationExecutionRequest)
        {
            string ResultString = "";
            var workDirectory = string.IsNullOrEmpty(stationExecutionRequest.ToolFullPath) ? null : new FileInfo(stationExecutionRequest.ToolFullPath).DirectoryName;
            //编码为 Base64 字符串，以解决命令行参数中引号的冲突问题
            var args = JsonSerializer.Serialize(stationExecutionRequest);
            string base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes(args));

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string combinedPath = Path.Combine(baseDirectory, @"..\..\..\CJ.Plug.StationAgent\Debug\net10.0\CJ.Plug.StationAgent.exe");
            string targetExePath = Path.GetFullPath(combinedPath);
            // 创建 ProcessStartInfo 对象
            var startInfo = new ProcessStartInfo
            {
                FileName = targetExePath,
                Arguments = base64Json, // 工具的命令行参数                                                                          
                //WorkingDirectory = workDirectory, // 设置工作目录，可根据实际情况修改
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                //UseShellExecute = false,
                //CreateNoWindow = false // 不创建窗口
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
                            ResultString += args.Data + "\n";
                        }
                    };

                    // 启动进程
                    process.Start();

                    // 开始异步读取输出和错误流
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                CLog.Error(e.Message);
            }

            var erd = new ExecuteResultData()
            {
                ResultString = ResultString,
                ExecuteSubStatus = JobSubStatus.已完成,
                ExecuteStatus = JobStatus.完成
            };
            return erd;
        }
    }
}
