using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Serilog;

namespace CJ.Plug.StationApiServer.Services
{
    /// <summary>
    /// 远程桌面服务
    /// 用于检测和管理 Station 上的 VNC/SSH 服务
    /// VNC 部分委托给 UltraVncService 管理
    /// </summary>
    public class RemoteDesktopService
    {
        private readonly UltraVncService _uvncService;

        public RemoteDesktopService(UltraVncService uvncService)
        {
            _uvncService = uvncService;
        }

        /// <summary>
        /// 远程服务状态
        /// </summary>
        public class RemoteServiceStatus
        {
            public bool VncInstalled { get; set; }
            public bool VncRunning { get; set; }
            public int VncPort { get; set; } = 5900;
            public string? VncProcessName { get; set; }
            public bool VncIsPortable { get; set; }

            public bool SshInstalled { get; set; }
            public bool SshRunning { get; set; }
            public int SshPort { get; set; } = 22;
            public string? SshProcessName { get; set; }

            public bool IsWindows { get; set; }
        }

        /// <summary>
        /// 获取远程服务状态
        /// </summary>
        public RemoteServiceStatus GetStatus()
        {
            var status = new RemoteServiceStatus
            {
                IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            };

            if (status.IsWindows)
            {
                CheckWindowsVnc(status);
                CheckWindowsSsh(status);
            }
            else
            {
                CheckLinuxVnc(status);
                CheckLinuxSsh(status);
            }

            return status;
        }

        /// <summary>
        /// 启动 VNC 服务 (委托给 UltraVncService)
        /// </summary>
        public async Task<bool> StartVncAsync(int port = 5900)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // 优先使用 UltraVncService（支持 portable 部署）
                    var uvncStatus = _uvncService.GetStatus();
                    if (uvncStatus.IsDeployed)
                    {
                        var (success, msg) = await _uvncService.StartAsync();
                        Log.Information("UltraVNC 启动结果: {Success}, {Message}", success, msg);
                        return success;
                    }

                    // 降级：尝试已安装的 VNC
                    return await StartWindowsVncFallbackAsync(port);
                }
                else
                {
                    return await StartLinuxVncAsync(port);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "启动 VNC 服务失败");
                return false;
            }
        }

        /// <summary>
        /// 停止 VNC 服务
        /// </summary>
        public (bool Success, string Message) StopVnc()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var uvncStatus = _uvncService.GetStatus();
                if (uvncStatus.IsRunning || uvncStatus.IsDeployed)
                {
                    return _uvncService.Stop();
                }
            }

            // 降级：杀死所有 VNC 进程
            try
            {
                var vncProcesses = new[] { "winvnc", "vncserver", "tvnserver", "uvnc_service" };
                var killed = false;
                foreach (var procName in vncProcesses)
                {
                    foreach (var proc in Process.GetProcessesByName(procName))
                    {
                        proc.Kill();
                        killed = true;
                    }
                }
                return (true, killed ? "VNC 服务已停止" : "VNC 未在运行");
            }
            catch (Exception ex)
            {
                return (false, $"停止失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动 SSH 服务
        /// </summary>
        public async Task<bool> StartSshAsync(int port = 22)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return await StartWindowsSshAsync(port);
                }
                else
                {
                    return await StartLinuxSshAsync(port);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "启动 SSH 服务失败");
                return false;
            }
        }

        #region Windows

        private void CheckWindowsVnc(RemoteServiceStatus status)
        {
            // 检查 UltraVNC portable 部署
            var uvncStatus = _uvncService.GetStatus();
            if (uvncStatus.IsDeployed)
            {
                status.VncInstalled = true;
                status.VncIsPortable = true;
                status.VncProcessName = "winvnc (UltraVNC)";
                status.VncRunning = uvncStatus.IsRunning;
                status.VncPort = uvncStatus.Port;
                return;
            }

            // 检查常见的 VNC 服务
            var vncProcesses = new[] { "winvnc", "vncserver", "tvnserver", "uvnc_service" };
            foreach (var procName in vncProcesses)
            {
                var processes = Process.GetProcessesByName(procName);
                if (processes.Length > 0)
                {
                    status.VncInstalled = true;
                    status.VncRunning = true;
                    status.VncProcessName = procName;
                    break;
                }
            }

            // 检查端口
            if (!status.VncRunning)
            {
                status.VncRunning = IsPortInUse(5900);
            }

            // 检查已安装的 VNC 路径
            if (!status.VncInstalled)
            {
                status.VncInstalled = FindInstalledVncPath() != null;
            }
        }

        private void CheckWindowsSsh(RemoteServiceStatus status)
        {
            // 检查 OpenSSH Server
            var sshdProcesses = Process.GetProcessesByName("sshd");
            if (sshdProcesses.Length > 0)
            {
                status.SshInstalled = true;
                status.SshRunning = true;
                status.SshProcessName = "sshd";
            }

            // 检查端口
            if (!status.SshRunning)
            {
                status.SshRunning = IsPortInUse(22);
            }
        }

        private async Task<bool> StartWindowsVncFallbackAsync(int port)
        {
            // 尝试启动已安装的 VNC
            var vncPath = FindInstalledVncPath();
            if (vncPath != null)
            {
                Process.Start(vncPath);
                Log.Information("已启动已安装的 VNC: {Path}", vncPath);
                await Task.Delay(2000);
                return true;
            }

            Log.Warning("未找到已安装的 VNC 服务，也未部署 UltraVNC portable");
            return false;
        }

        private static string? FindInstalledVncPath()
        {
            var vncPaths = new[]
            {
                @"C:\Program Files\TightVNC\winvnc.exe",
                @"C:\Program Files\RealVNC\VNC Server\vncserver.exe",
                @"C:\Program Files\uvnc bvba\UltraVNC\winvnc.exe",
                @"C:\Program Files\UltraVNC\winvnc.exe"
            };
            return vncPaths.FirstOrDefault(File.Exists);
        }

        private async Task<bool> StartWindowsSshAsync(int port)
        {
            try
            {
                // 尝试启动 Windows OpenSSH 服务
                var startInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "start sshd",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Log.Information("已启动 SSH 服务");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "启动 SSH 服务失败");
            }

            return false;
        }

        #endregion

        #region Linux

        private void CheckLinuxVnc(RemoteServiceStatus status)
        {
            // 检查 VNC 进程
            var vncProcesses = new[] { "Xvnc", "vncserver", "x11vnc" };
            foreach (var procName in vncProcesses)
            {
                var processes = Process.GetProcessesByName(procName);
                if (processes.Length > 0)
                {
                    status.VncInstalled = true;
                    status.VncRunning = true;
                    status.VncProcessName = procName;
                    break;
                }
            }

            // 检查端口
            if (!status.VncRunning)
            {
                status.VncRunning = IsPortInUse(5900);
            }
        }

        private void CheckLinuxSsh(RemoteServiceStatus status)
        {
            // 检查 SSH 进程
            var sshdProcesses = Process.GetProcessesByName("sshd");
            if (sshdProcesses.Length > 0)
            {
                status.SshInstalled = true;
                status.SshRunning = true;
                status.SshProcessName = "sshd";
            }

            // 检查端口
            if (!status.SshRunning)
            {
                status.SshRunning = IsPortInUse(22);
            }
        }

        private async Task<bool> StartLinuxVncAsync(int port)
        {
            try
            {
                // 尝试使用 vncserver 启动
                var startInfo = new ProcessStartInfo
                {
                    FileName = "vncserver",
                    Arguments = $":{port - 5900} -geometry 1920x1080 -depth 24",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Log.Information("已启动 VNC 服务，端口: {Port}", port);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "启动 VNC 服务失败");
            }

            return false;
        }

        private async Task<bool> StartLinuxSshAsync(int port)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "systemctl start sshd",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Log.Information("已启动 SSH 服务");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "启动 SSH 服务失败");
            }

            return false;
        }

        #endregion

        #region Helpers

        private static bool IsPortInUse(int port)
        {
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
                return tcpConnInfoArray.Any(endpoint => endpoint.Port == port);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
