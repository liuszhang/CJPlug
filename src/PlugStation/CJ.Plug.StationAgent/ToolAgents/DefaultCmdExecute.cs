using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.StationAgent.Contracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

                    // 单窗口 RFB VNC（子方案1，主A改版）：启动后立即枚举 cmd 子进程拿真实 PID 绑定。
                    // 不依赖窗口出现（VNC 界面先开/程序先开都能稳定连接）：
                    //   CaptureFrame 每帧枚举目标 PID 的窗口，窗口一出现立即渲染。
                    if (stationExecutionRequest.RemoteViewMode == "window")
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // 1) 主 A：ToolHelp32 立即枚举 cmd 的子进程（工具真实 PID），0.2s×15=3s
                                //    进程创建早于窗口出现，VNC 先开也能立即拿到目标。
                                var bound = await BindByChildProcessAsync(
                                    StationApiClient,
                                    toolPid,
                                    stationExecutionRequest.RemoteViewProcessName,
                                    stationExecutionRequest.ExecuteResultData?.Ids?.ToolJobCorrelationId);

                                // 2) 兜底：子进程枚举失败（工具是脚本/内联命令等）→ 窗口轮询
                                if (!bound)
                                {
                                    DateTime cmdStart;
                                    try { cmdStart = process.StartTime; }
                                    catch { cmdStart = DateTime.Now.AddSeconds(-5); }
                                    for (int i = 0; i < 10; i++)
                                    {
                                        await Task.Delay(500);
                                        var target = FindNewToolWindow(cmdStart, toolPid);
                                        if (target != null)
                                        {
                                            await StationApiClient.BindWindowAsync(
                                                target.Value.Id,
                                                stationExecutionRequest.RemoteViewProcessName,
                                                stationExecutionRequest.ExecuteResultData?.Ids?.ToolJobCorrelationId);
                                            await StationApiClient.SendLog($"已绑定单窗口VNC目标: {target.Value.Name}(PID={target.Value.Id})");
                                            bound = true;
                                            break;
                                        }
                                    }
                                }

                                // 3) 辅 B 兜底：按配置进程名绑定（无 PID 时由服务端按名枚举）
                                if (!bound && !string.IsNullOrEmpty(stationExecutionRequest.RemoteViewProcessName))
                                {
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

        /// <summary>
        /// 主 A：用 ToolHelp32 枚举 cmd 的**全部后代进程**（含孙子，覆盖 Store 版 stub 架构：
        /// notepad.exe 是 stub 无窗口，实际窗口在它 spawn 的子进程），
        /// **优先选有 MainWindowHandle 的进程**（窗口真实所有者），找到即立即绑定。
        /// 进程创建远早于窗口出现，因此无论 VNC 界面先开还是程序先开都能稳定连接。
        /// </summary>
        private static async Task<bool> BindByChildProcessAsync(
            StationApiClient StationApiClient,
            int cmdPid,
            string? processName,
            string? sessionKey)
        {
            for (int i = 0; i < 15; i++) // 0.2s×15 = 3 秒
            {
                var target = FindDescendantWindowProcess(cmdPid);
                if (target != null)
                {
                    await StationApiClient.BindWindowAsync(
                        target.Value.Id,
                        processName,
                        sessionKey);
                    await StationApiClient.SendLog($"已绑定单窗口VNC目标: {target.Value.Name}(PID={target.Value.Id})");
                    Console.WriteLine($"[BindDiag] ToolHelp32 后代窗口进程绑定成功: {target.Value.Name}(PID={target.Value.Id})");
                    return true;
                }
                await Task.Delay(200);
            }
            Console.WriteLine("[BindDiag] ToolHelp32 未找到带窗口的后代进程（3 秒超时），转入窗口轮询兜底");
            return false;
        }

        /// <summary>
        /// ToolHelp32 快照：查找 cmd 全部后代进程中**有 MainWindowHandle 的**（窗口真实所有者）。
        /// 优先窗口进程（排除忽略名单）；若全部无窗口，退回第一个非忽略后代（等窗口出现）。
        /// </summary>
        private static (int Id, string Name)? FindDescendantWindowProcess(int rootPid)
        {
            var descendants = GetDescendantPids(rootPid);
            if (descendants.Count == 0)
                return null;

            // 第一轮：优先找有主窗口的后代（窗口真实所有者，覆盖 stub 架构）
            foreach (var pid in descendants)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    if (p.MainWindowHandle != IntPtr.Zero && !IsIgnoredProcessName(p.ProcessName))
                        return (pid, p.ProcessName);
                }
                catch { /* 进程已退出 */ }
            }

            // 第二轮：全部无窗口 → 取第一个非忽略后代（stub 或启动中的进程，等窗口出现）
            foreach (var pid in descendants)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    if (!IsIgnoredProcessName(p.ProcessName))
                        return (pid, p.ProcessName);
                }
                catch { }
            }
            return null;
        }

        /// <summary>ToolHelp32 快照：获取指定进程的全部后代 PID（BFS，含孙子）。</summary>
        private static List<int> GetDescendantPids(int rootPid)
        {
            var result = new List<int>();
            var parentMap = new Dictionary<int, List<int>>();
            try
            {
                IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (snapshot == IntPtr.Zero || snapshot == (IntPtr)INVALID_HANDLE_VALUE)
                    return result;
                try
                {
                    var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
                    if (Process32First(snapshot, ref entry))
                    {
                        do
                        {
                            if (!parentMap.TryGetValue((int)entry.th32ParentProcessID, out var list))
                            {
                                list = new List<int>();
                                parentMap[(int)entry.th32ParentProcessID] = list;
                            }
                            list.Add((int)entry.th32ProcessID);
                        } while (Process32Next(snapshot, ref entry));
                    }
                }
                finally
                {
                    CloseHandle(snapshot);
                }

                // BFS：root → 直接子 → 孙子...
                var queue = new Queue<int>();
                queue.Enqueue(rootPid);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (parentMap.TryGetValue(current, out var children))
                    {
                        foreach (var child in children)
                        {
                            if (child == rootPid) continue;
                            result.Add(child);
                            queue.Enqueue(child);
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        // ToolHelp32 P/Invoke
        private const uint TH32CS_SNAPPROCESS = 0x00000002;
        private const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW")]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "Process32NextW")]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

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
