using System.Diagnostics;
using System.Net.NetworkInformation;
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

        // 检查是否运行中
        var proc = FindRunningUvncProcess();
        if (proc != null)
        {
            status.IsRunning = true;
            status.ProcessId = proc.Id;
        }
        else
        {
            status.IsRunning = IsPortInUse(5900);
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

            // 如果已经在运行，先停止
            var existing = FindRunningUvncProcess();
            if (existing != null)
                return (true, "UltraVNC 已在运行中");

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

            // 验证
            var running = FindRunningUvncProcess();
            if (running != null || IsPortInUse(5900))
            {
                Log.Information("UltraVNC 已启动, PID: {Pid}", process.Id);
                return (true, "UltraVNC 已启动");
            }

            return (false, "UltraVNC 启动后未检测到运行进程，请检查日志");
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

    #endregion
}
