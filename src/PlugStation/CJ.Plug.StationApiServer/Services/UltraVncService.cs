using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace CJ.Plug.StationApiServer.Services;

/// <summary>
/// UltraVNC Portable 管理服务
/// 管理 UltraVNC 的部署、配置、启动、停止
/// </summary>
public class UltraVncService
{
    // UltraVNC 文件部署目录: %ProgramData%\CJStation\uvnc
    private static readonly string UvncDeployDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "CJStation", "uvnc");

    // 配置文件路径
    private static readonly string UvncIniPath = Path.Combine(UvncDeployDir, "ultravnc.ini");

    // UltraVNC 可执行文件
    private static readonly string WinVncExe = Path.Combine(UvncDeployDir, "winvnc.exe");

    // 源文件目录（嵌入式资源或随附文件夹）
    private static readonly string EmbeddedDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "uvnc-portable");

    /// <summary>
    /// UltraVNC 部署状态
    /// </summary>
    public class UvncStatus
    {
        public bool IsDeployed { get; set; }
        public bool IsRunning { get; set; }
        public int Port { get; set; } = 5900;
        public int HttpPort { get; set; } = 5800;
        public string? DeployPath { get; set; }
        public string? ExePath { get; set; }
        public int? ProcessId { get; set; }
        public bool LoopbackEnabled { get; set; }
    }

    /// <summary>
    /// 获取 UltraVNC 状态
    /// </summary>
    public UvncStatus GetStatus()
    {
        var status = new UvncStatus
        {
            DeployPath = UvncDeployDir
        };

        // 检查是否已部署
        status.IsDeployed = File.Exists(WinVncExe);
        status.ExePath = status.IsDeployed ? WinVncExe : FindInstalledUvnc();

        // 检查是否运行中：必须以“进程存在且确实在监听 5900”为准，
        // 不能仅凭进程存在判断（排除僵死进程）。
        var proc = FindRunningUvncProcess();
        if (proc != null && IsPortListenedByUvnc(5900, proc.Id))
        {
            status.IsRunning = true;
            status.ProcessId = proc.Id;
        }
        else
        {
            status.IsRunning = false;
        }

        // 读取配置
        if (status.IsDeployed)
        {
            var config = ReadIniConfig();
            status.Port = config.GetValueOrDefault("PortNumber", 5900);
            status.HttpPort = config.GetValueOrDefault("HttpPort", 5800);
            status.LoopbackEnabled = config.GetValueOrDefault("LoopbackConnections", 0) == 1;
        }

        return status;
    }

    /// <summary>
    /// 部署 UltraVNC portable 到 ProgramData
    /// 从随附文件夹复制 UltraVNC portable 文件
    /// </summary>
    public async Task<(bool Success, string Message)> DeployAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (false, "UltraVNC 仅支持 Windows 平台");

        try
        {
            // 检查源文件
            if (!Directory.Exists(EmbeddedDir))
                return (false, $"未找到 UltraVNC 源文件目录: {EmbeddedDir}。请将 winvnc.exe、VNCHooks.dll 放入 uvnc-portable 文件夹。");

            // 必须有 winvnc.exe
            if (!File.Exists(Path.Combine(EmbeddedDir, "winvnc.exe")))
                return (false, "缺少 winvnc.exe，请将其放入 uvnc-portable 文件夹。");

            // 创建目标目录
            Directory.CreateDirectory(UvncDeployDir);

            // 复制 uvnc-portable 目录下的所有文件（exe + dll + 其他依赖）
            var sourceFiles = Directory.GetFiles(EmbeddedDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".exe" or ".dll" or ".ini" or ".manifest";
                })
                .ToList();

            foreach (var src in sourceFiles)
            {
                var fileName = Path.GetFileName(src);
                var dst = Path.Combine(UvncDeployDir, fileName);
                File.Copy(src, dst, overwrite: true);
                Log.Debug("复制: {File}", fileName);
            }

            // 写入默认配置
            WriteDefaultIniConfig();

            Log.Information("UltraVNC 已部署到: {Dir}", UvncDeployDir);
            return (true, $"UltraVNC 已部署到: {UvncDeployDir}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "部署 UltraVNC 失败");
            return (false, $"部署失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动 UltraVNC
    /// </summary>
    public async Task<(bool Success, string Message)> StartAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (false, "UltraVNC 仅支持 Windows 平台");

        try
        {
            // 优先使用部署版本
            var exePath = File.Exists(WinVncExe) ? WinVncExe : FindInstalledUvnc();
            if (exePath == null)
                return (false, "未找到 UltraVNC。请先部署 UltraVNC portable。");

            // 确保配置正确
            if (File.Exists(WinVncExe))
                EnsureLoopbackEnabled();

            // 如果已经在运行，先彻底停止（清掉可能存在的僵死进程）
            var existing = FindRunningUvncProcess();
            if (existing != null)
                Stop();

            // 启动前检查目标端口是否被【其他进程】占用（含 LISTENING 与 BOUND 状态，
            // 后者用于捕获 Foxmail 这类端口泄漏场景——它只 bind 不进入 LISTENING）。
            var occupier = GetPortOccupier(5900);
            if (occupier != null)
            {
                var occupierName = GetProcessName(occupier.Value);
                var msg = $"UltraVNC 启动失败：端口 5900 已被进程 [{occupierName}] (PID={occupier.Value}) 占用，请先释放该端口";
                Log.Error(msg);
                return (false, msg);
            }

            // 启动进程
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-autoreconnect -run",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? UvncDeployDir
            };

            var process = Process.Start(startInfo);
            if (process == null)
                return (false, "无法启动 UltraVNC 进程");

            // 等待启动
            await Task.Delay(2000);

            // 验证：优先用真实 TCP 连接确认 5900 可被连接（比单纯判断进程存在更可靠，
            // 可排除“进程在但没监听/僵死”的假成功）。
            if (await IsPortConnectableAsync("127.0.0.1", 5900))
            {
                Log.Information("UltraVNC 已启动并监听端口 5900, PID: {Pid}", process.Id);
                return (true, "UltraVNC 已启动");
            }

            // 连接失败：进一步区分是被别的进程占了，还是本进程自己僵死
            var blocker = GetPortOccupier(5900);
            if (blocker != null && blocker != process.Id)
            {
                var blockerName = GetProcessName(blocker.Value);
                var msg = $"UltraVNC 启动失败：端口 5900 被进程 [{blockerName}] (PID={blocker.Value}) 抢占，请先释放该端口";
                Log.Error(msg);
                return (false, msg);
            }

            return (false, "UltraVNC 启动后未在端口 5900 监听，可能进程僵死或启动异常，请检查 winvnc 日志");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动 UltraVNC 失败");
            return (false, $"启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止 UltraVNC
    /// </summary>
    public (bool Success, string Message) Stop()
    {
        try
        {
            var processes = FindAllUvncProcesses();
            if (processes.Count == 0)
                return (true, "UltraVNC 未在运行");

            foreach (var proc in processes)
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                    Log.Information("已停止 UltraVNC 进程: {Pid}", proc.Id);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "停止进程 {Pid} 失败", proc.Id);
                }
            }

            // 等待端口释放
            Thread.Sleep(500);
            return (true, "UltraVNC 已停止");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "停止 UltraVNC 失败");
            return (false, $"停止失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新 UltraVNC 配置
    /// </summary>
    public (bool Success, string Message) UpdateConfig(int port = 5900, bool loopback = true)
    {
        if (!File.Exists(WinVncExe))
            return (false, "UltraVNC 未部署");

        try
        {
            var config = ReadIniConfig();
            config["PortNumber"] = port;
            config["HttpPort"] = port - 100; // HTTP 端口默认比 VNC 端口小 100
            config["LoopbackConnections"] = loopback ? 1 : 0;
            WriteIniConfig(config);
            Log.Information("UltraVNC 配置已更新: Port={Port}, Loopback={Loopback}", port, loopback);
            return (true, "配置已更新，重启 VNC 后生效");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新 UltraVNC 配置失败");
            return (false, $"配置更新失败: {ex.Message}");
        }
    }

    #region Private Methods

    private static string? FindInstalledUvnc()
    {
        var paths = new[]
        {
            @"C:\Program Files\uvnc bvba\UltraVNC\winvnc.exe",
            @"C:\Program Files\UltraVNC\winvnc.exe",
            @"C:\Program Files (x86)\uvnc bvba\UltraVNC\winvnc.exe",
            @"C:\Program Files (x86)\UltraVNC\winvnc.exe"
        };
        return paths.FirstOrDefault(File.Exists);
    }

    private static Process? FindRunningUvncProcess()
    {
        try
        {
            return Process.GetProcessesByName("winvnc").FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static List<Process> FindAllUvncProcesses()
    {
        try
        {
            return Process.GetProcessesByName("winvnc").ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void EnsureLoopbackEnabled()
    {
        try
        {
            var config = ReadIniConfig();
            if (config.GetValueOrDefault("LoopbackConnections", 0) != 1)
            {
                config["LoopbackConnections"] = 1;
                WriteIniConfig(config);
                Log.Information("已自动启用 Loopback 连接");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "检查 Loopback 配置失败");
        }
    }

    private static void WriteDefaultIniConfig()
    {
        var config = new Dictionary<string, int>
        {
            ["PortNumber"] = 5900,
            ["HttpPort"] = 5800,
            ["LoopbackConnections"] = 1,
            ["ConnectPriority"] = 0,
            ["AuthRequired"] = 1,
            ["AllowLoopback"] = 1,
            ["DebugMode"] = 0,
            ["FileTransferEnabled"] = 0,
            ["BlankScreen"] = 0
        };
        WriteIniConfig(config);
    }

    private static Dictionary<string, int> ReadIniConfig()
    {
        var config = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(UvncIniPath))
            return config;

        foreach (var line in File.ReadAllLines(UvncIniPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('[') || trimmed.StartsWith(';'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0) continue;

            var key = trimmed[..eqIndex].Trim();
            var valueStr = trimmed[(eqIndex + 1)..].Trim();
            if (int.TryParse(valueStr, out var value))
                config[key] = value;
        }
        return config;
    }

    private static void WriteIniConfig(Dictionary<string, int> config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[admin]");
        foreach (var kv in config.OrderBy(kv => kv.Key))
            sb.AppendLine($"{kv.Key}={kv.Value}");
        sb.AppendLine();
        sb.AppendLine("[poll]");
        sb.AppendLine("TurboMode=1");
        sb.AppendLine("PollFullScreen=1");
        sb.AppendLine("PollForeground=1");

        File.WriteAllText(UvncIniPath, sb.ToString());
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(ep => ep.Port == port);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断指定端口是否正被给定的 winvnc 进程监听（排除“进程在但无监听”的僵死态）。
    /// </summary>
    private static bool IsPortListenedByUvnc(int port, int processId)
    {
        try
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(ep => ep.Port == port && GetTcpListenerOwner(ep) == processId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 返回占用目标端口的【非本机 winvnc】进程 PID，覆盖 LISTENING 与 BOUND 两种状态。
    /// BOUND 用于捕获端口泄漏场景（如 Foxmail 仅 bind 不监听），常规 LISTENING 查询无法发现。
    /// </summary>
    private static int? GetPortOccupier(int port)
    {
        try
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            foreach (var listener in listeners)
            {
                if (listener.Port == port)
                {
                    var owner = GetTcpListenerOwner(listener);
                    if (owner.HasValue) return owner.Value;
                }
            }

            // 回退：用 GetExtendedTcpTable 查询 BOUND 状态（listen 查询漏掉的泄漏端口）
            foreach (var bound in GetBoundTcpPorts())
            {
                if (bound.Port == port) return bound.ProcessId;
            }
        }
        catch
        {
            // 忽略异常，交由调用方决定
        }

        return null;
    }

    /// <summary>
    /// 尝试真实连接目标端口，确认服务确有监听并能接受连接。
    /// </summary>
    private static async Task<bool> IsPortConnectableAsync(string host, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(host, port);
            if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                return false; // 超时
            await connectTask;
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetProcessName(int processId)
    {
        try
        {
            using var p = Process.GetProcessById(processId);
            return p.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    #region P/Invoke (TCP 监听/连接所有者与 BOUND 端口查询)

    private const int AF_INET = 2;
    private const int TCP_TABLE_OWNER_PID_LISTENER = 3;
    private const int TCP_TABLE_OWNER_PID_ALL = 5;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;  // 网络字节序，低 16 位为端口
        public uint RemoteAddr;
        public uint RemotePort; // 网络字节序，低 16 位为端口
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpTableOwnerPid
    {
        public uint DwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public MibTcpRowOwnerPid[] Table;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion,
        int tblEnumType, int reserved = 0);

    /// <summary>
    /// 将 native MIB_TCPROW_OWNER_PID 中以网络字节序存储的端口字段（低 16 位）转为主机序端口号。
    /// </summary>
    private static int NtoHSPort(uint rawPort)
    {
        var bytes = BitConverter.GetBytes(rawPort);
        // 端口只占低 16 位，且为网络字节序（big-endian），需翻转
        return (bytes[1] << 8) | bytes[0];
    }

    private static int? GetTcpListenerOwner(IPEndPoint listener)
    {
        var rows = GetAllTcpRows(TCP_TABLE_OWNER_PID_LISTENER);
        var listenerPort = listener.Port;
        var listenerAddr = listener.Address;
        foreach (var row in rows)
        {
            if (NtoHSPort(row.LocalPort) != listenerPort) continue;
            var localAddr = new IPAddress(row.LocalAddr);
            if (localAddr.Equals(listenerAddr) ||
                (listenerAddr.Equals(IPAddress.Any) && (localAddr.Equals(IPAddress.Any) || localAddr.Equals(IPAddress.Loopback))) ||
                (localAddr.Equals(IPAddress.Any) && (listenerAddr.Equals(IPAddress.Any) || listenerAddr.Equals(IPAddress.Loopback))))
            {
                return (int)row.OwningPid;
            }
        }

        return null;
    }

    private static IEnumerable<(int Port, int ProcessId)> GetBoundTcpPorts()
    {
        // TCP_TABLE_OWNER_PID_ALL 包含全部状态（含 BOUND），按本地端口聚合，
        // 仅返回处于“已绑定但未监听”的端口（State=2 即 MIB_TCP_STATE_BOUND）。
        var rows = GetAllTcpRows(TCP_TABLE_OWNER_PID_ALL);
        foreach (var row in rows)
        {
            if (row.State == 2) // MIB_TCP_STATE_BOUND
                yield return (NtoHSPort(row.LocalPort), (int)row.OwningPid);
        }
    }

    private static IEnumerable<MibTcpRowOwnerPid> GetAllTcpRows(int tableType)
    {
        var rows = new List<MibTcpRowOwnerPid>();
        int bufSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufSize, true, AF_INET, tableType);
        if (result != ERROR_INSUFFICIENT_BUFFER) yield break;

        var buffer = Marshal.AllocHGlobal(bufSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufSize, true, AF_INET, tableType);
            if (result != 0) yield break;

            var table = Marshal.PtrToStructure<MibTcpTableOwnerPid>(buffer);
            var tableSize = Math.Min((int)table.DwNumEntries, table.Table.Length);
            for (var i = 0; i < tableSize; i++)
            {
                rows.Add(table.Table[i]);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        foreach (var row in rows) yield return row;
    }

    #endregion

    #endregion
}
