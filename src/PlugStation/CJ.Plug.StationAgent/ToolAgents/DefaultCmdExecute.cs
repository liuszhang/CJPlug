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

                    // 立即快照 PID（此刻进程必活着）：轮询线程全程使用快照，
                    // 避免 cmd 秒退后 using 块 Dispose 了 process，后台线程再访问
                    // process.Id 抛 "No process is associated with this object"
                    int toolPid = process.Id;

                    // 开始异步读取输出和错误流
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 单窗口 RFB VNC（子方案1，主A）：启动后轮询工具窗口出现，上报真实 PID 绑定
                    if (stationExecutionRequest.RemoteViewMode == "window")
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // 工具进程可能秒退（CMD 类）：访问 StartTime 需防 Win32 异常。
                                // 兜底基准必须早于所有候选窗口（cmd 秒退时 notepad 已启动，用 Now 会误过滤）。
                                // 轮询窗口 10×500ms=5 秒，用 Now-5s 保证任何工具启动的窗口都晚于该基准。
                                DateTime cmdStart;
                                try { cmdStart = process.StartTime; }
                                catch { cmdStart = DateTime.Now.AddSeconds(-5); }
                                int? boundPid = null;
                                for (int i = 0; i < 10; i++)
                                {
                                    await Task.Delay(500);
                                    var target = FindNewToolWindow(cmdStart, toolPid);
                                    if (target != null)
                                    {
                                        boundPid = target.Value.Id;
                                        await StationApiClient.BindWindowAsync(
                                            target.Value.Id,
                                            stationExecutionRequest.RemoteViewProcessName,
                                            stationExecutionRequest.ExecuteResultData?.Ids?.ToolJobCorrelationId);
                                        await StationApiClient.SendLog($"已绑定单窗口VNC目标: {target.Value.Name}(PID={target.Value.Id})");
                                        break;
                                    }
                                }
                                if (boundPid == null && !string.IsNullOrEmpty(stationExecutionRequest.RemoteViewProcessName))
                                {
                                    // 辅B兜底：窗口轮询失败时按配置进程名绑定
                                    await StationApiClient.BindWindowAsync(
                                        null,
                                        stationExecutionRequest.RemoteViewProcessName,
                                        stationExecutionRequest.ExecuteResultData?.Ids?.ToolJobCorrelationId);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("VNC window bind error: " + ex.Message);
                            }
                        });
                    }

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

        /// <summary>
        /// 查找 cmd 启动后新出现的 GUI 工具窗口所属进程（排除系统/自身进程）。
        /// 返回结构体而非 Process 对象：进程退出后再访问 Process 属性会抛
        /// "No process is associated with this object"，结构体在枚举时已取好值。
        /// </summary>
        private static (int Id, string Name)? FindNewToolWindow(DateTime afterStart, int excludePid)
        {
            try
            {
                var procs = Process.GetProcesses();
                // 诊断：第一次调用时打印候选环境（会话/桌面差异定位）
                if (!_diagPrinted)
                {
                    _diagPrinted = true;
                    try
                    {
                        Console.WriteLine($"[BindDiag] 基准={afterStart:HH:mm:ss.fff} 排除PID={excludePid} 当前进程={Environment.ProcessId} 总进程数={procs.Length}");
                        Console.WriteLine($"[BindDiag] 会话/桌面: 交互={Environment.UserInteractive} 用户={Environment.UserName} 机器={Environment.MachineName}");
                        foreach (var p in procs)
                        {
                            try
                            {
                                var hwnd = p.MainWindowHandle;
                                if (hwnd == IntPtr.Zero) continue;
                                Console.WriteLine($"[BindDiag]   有窗进程 PID={p.Id,-6} 名={p.ProcessName,-24} 句柄=0x{hwnd.ToInt64():X} 启动={p.StartTime:HH:mm:ss.fff}");
                            }
                            catch { }
                        }
                    }
                    catch (Exception dex) { Console.WriteLine($"[BindDiag] 诊断失败: {dex.Message}"); }
                }
                return procs
                    .Where(p =>
                    {
                        if (p.Id == excludePid || p.Id == Environment.ProcessId) return false;
                        string name;
                        try
                        {
                            name = p.ProcessName;
                            if (IsIgnoredProcessName(name)) return false;
                            return p.MainWindowHandle != IntPtr.Zero && p.StartTime >= afterStart;
                        }
                        catch
                        {
                            return false; // 访问已退出进程的属性会抛异常
                        }
                    })
                    .OrderBy(p => SafeStartTime(p))
                    .Select(p => SafeProcessInfo(p))
                    .FirstOrDefault(x => x != null);
            }
            catch
            {
                return null;
            }
        }

        private static bool _diagPrinted;

        /// <summary>安全读取进程启动时间（进程已退出时返回当前时间，避免 Win32 异常）。</summary>
        private static DateTime SafeStartTime(Process p)
        {
            try { return p.StartTime; }
            catch { return DateTime.Now; }
        }

        /// <summary>安全读取进程信息快照（进程已退出时返回 null）。</summary>
        private static (int Id, string Name)? SafeProcessInfo(Process p)
        {
            try { return (p.Id, p.ProcessName); }
            catch { return null; }
        }

        private static bool IsIgnoredProcessName(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            name = name.ToLowerInvariant();
            return name is "cmd" or "conhost" or "explorer" or "dwm" or "winlogon" or "csrss"
                or "cj.plug.stationagent" or "cj.plug.stationapiserver" or "svchost"
                or "taskhostw" or "sihost" or "runtimebroker" or "searchapp" or "textinputhost"
                or "winvnc";
        }

    }
}
