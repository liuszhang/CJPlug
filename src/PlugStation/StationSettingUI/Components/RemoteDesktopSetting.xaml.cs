using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StationSettingUI.Models;
using StationSettingUI.Services;

namespace StationSettingUI.Components
{
    public partial class RemoteDesktopSetting : UserControl, INotifyPropertyChanged
    {
        private readonly StationSettingUI.Services.StationConfigService _configService;
        private readonly StationApiService _apiService;
        private readonly AppConfig _config;

        public RemoteDesktopSetting()
        {
            InitializeComponent();
            DataContext = this;
            _configService = new StationSettingUI.Services.StationConfigService();
            _config = _configService.LoadConfig();
            _apiService = new StationApiService(_config);
        }

        #region 属性

        private string _vncStatusText = "检测中...";
        public string VncStatusText
        {
            get => _vncStatusText;
            set { _vncStatusText = value; OnPropertyChanged(nameof(VncStatusText)); }
        }

        private Brush _vncStatusColor = Brushes.Gray;
        public Brush VncStatusColor
        {
            get => _vncStatusColor;
            set { _vncStatusColor = value; OnPropertyChanged(nameof(VncStatusColor)); }
        }

        private bool _vncCanStart = false;
        public bool VncCanStart
        {
            get => _vncCanStart;
            set { _vncCanStart = value; OnPropertyChanged(nameof(VncCanStart)); }
        }

        private bool _vncCanStop = false;
        public bool VncCanStop
        {
            get => _vncCanStop;
            set { _vncCanStop = value; OnPropertyChanged(nameof(VncCanStop)); }
        }

        private string _vncDescription = "VNC (Virtual Network Computing) 允许远程访问图形桌面。\n安装后可通过浏览器查看远程桌面。";
        public string VncDescription
        {
            get => _vncDescription;
            set { _vncDescription = value; OnPropertyChanged(nameof(VncDescription)); }
        }

        private string _sshStatusText = "检测中...";
        public string SshStatusText
        {
            get => _sshStatusText;
            set { _sshStatusText = value; OnPropertyChanged(nameof(SshStatusText)); }
        }

        private Brush _sshStatusColor = Brushes.Gray;
        public Brush SshStatusColor
        {
            get => _sshStatusColor;
            set { _sshStatusColor = value; OnPropertyChanged(nameof(SshStatusColor)); }
        }

        private bool _sshCanStart = false;
        public bool SshCanStart
        {
            get => _sshCanStart;
            set { _sshCanStart = value; OnPropertyChanged(nameof(SshCanStart)); }
        }

        private bool _sshCanStop = false;
        public bool SshCanStop
        {
            get => _sshCanStop;
            set { _sshCanStop = value; OnPropertyChanged(nameof(SshCanStop)); }
        }

        private string _sshDescription = "SSH (Secure Shell) 允许远程命令行访问。\n安装后可通过浏览器访问终端。";
        public string SshDescription
        {
            get => _sshDescription;
            set { _sshDescription = value; OnPropertyChanged(nameof(SshDescription)); }
        }

        private string _vncButtonText = "安装 VNC 服务";
        public string VncButtonText
        {
            get => _vncButtonText;
            set { _vncButtonText = value; OnPropertyChanged(nameof(VncButtonText)); }
        }

        private bool _isInstalling = false;
        public bool IsInstalling
        {
            get => _isInstalling;
            set { _isInstalling = value; OnPropertyChanged(nameof(IsInstalling)); }
        }

        #endregion

        #region 事件

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async void InstallVnc_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已部署 UltraVNC
            var status = await GetRemoteStatusAsync();
            if (status?.VncInstalled == true && status.VncIsPortable)
            {
                MessageBox.Show("UltraVNC 已部署。如需重新部署，请先删除 %ProgramData%\\CJStation\\uvnc 目录。",
                    "已部署", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "即将部署 UltraVNC Portable 到本机。\n\n" +
                "部署内容:\n" +
                "- UltraVNC Server (winvnc.exe)\n" +
                "- 自动配置 Loopback 连接\n" +
                "- 无需网络下载，零外部依赖\n\n" +
                "是否继续？",
                "部署 UltraVNC",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await DeployUvncAsync();
        }

        private async void InstallSsh_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "即将安装 OpenSSH Server 服务。\n\n需要管理员权限，安装完成后服务将自动启动。\n\n是否继续？",
                "安装 SSH",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await InstallSshAsync();
        }

        private async void StartVnc_Click(object sender, RoutedEventArgs e)
        {
            await StartVncAsync();
        }

        private async void StopVnc_Click(object sender, RoutedEventArgs e)
        {
            await StopVncAsync();
        }

        private async void StartSsh_Click(object sender, RoutedEventArgs e)
        {
            await StartSshAsync();
        }

        private async void StopSsh_Click(object sender, RoutedEventArgs e)
        {
            await StopSshAsync();
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var vncPort = 5900;
            var sshPort = 22;

            // 使用 System.Net 检查端口
            var vncOk = IsPortInUse(vncPort);
            var sshOk = IsPortInUse(sshPort);

            var message = $"连接测试结果:\n\n" +
                         $"VNC (端口 {vncPort}): {(vncOk ? "✓ 可用" : "✗ 不可用")}\n" +
                         $"SSH (端口 {sshPort}): {(sshOk ? "✓ 可用" : "✗ 不可用")}\n\n" +
                         $"本机 IP: {GetLocalIpAddress()}";

            MessageBox.Show(message, "连接测试", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 方法

        private async Task<StationApiService.RemoteServiceStatus?> GetRemoteStatusAsync()
        {
            return await _apiService.GetRemoteServiceStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            var status = await GetRemoteStatusAsync();

            if (status == null)
            {
                // StationApiServer 未运行，使用本地检测
                var localVncRunning = IsVncRunning();
                VncStatusText = localVncRunning ? "运行中 (本地检测)" : "未运行";
                VncStatusColor = localVncRunning ? Brushes.Green : Brushes.Gray;
                VncCanStart = !localVncRunning;
                VncCanStop = localVncRunning;
                VncButtonText = "安装 VNC 服务";

                var localSshRunning = IsSshRunning();
                SshStatusText = localSshRunning ? "运行中 (本地检测)" : "未运行";
                SshStatusColor = localSshRunning ? Brushes.Green : Brushes.Gray;
                SshCanStart = !localSshRunning;
                SshCanStop = localSshRunning;

                VncDescription = "StationApiServer 未运行，无法通过 API 管理 VNC。\n请先在「服务配置」页启动图站服务。";
                return;
            }

            // 通过 API 获取的状态
            VncStatusText = status.VncRunning ? "运行中" : (status.VncInstalled ? "已安装" : "未安装");
            VncStatusColor = status.VncRunning ? Brushes.Green : (status.VncInstalled ? Brushes.Orange : Brushes.Gray);
            VncCanStart = !status.VncRunning;
            VncCanStop = status.VncRunning;

            if (status.VncIsPortable)
            {
                VncButtonText = "重新部署";
                VncDescription = "使用 UltraVNC Portable 模式。\n" +
                                $"进程: {status.VncProcessName ?? "winvnc"}\n" +
                                $"端口: {status.VncPort}";
            }
            else if (status.VncInstalled)
            {
                VncButtonText = "重新部署";
                VncDescription = $"已安装 VNC 服务。\n" +
                                $"进程: {status.VncProcessName ?? "未知"}\n" +
                                $"端口: {status.VncPort}";
            }
            else
            {
                VncButtonText = "部署 UltraVNC";
                VncDescription = "VNC (Virtual Network Computing) 允许远程访问图形桌面。\n" +
                                "点击「部署 UltraVNC」一键安装，无需网络下载。";
            }

            SshStatusText = status.SshRunning ? "运行中" : (status.SshInstalled ? "已安装" : "未安装");
            SshStatusColor = status.SshRunning ? Brushes.Green : (status.SshInstalled ? Brushes.Orange : Brushes.Gray);
            SshCanStart = !status.SshRunning;
            SshCanStop = status.SshRunning;

            if (status.SshInstalled)
            {
                SshDescription = $"已安装 SSH 服务。\n" +
                                $"进程: {status.SshProcessName ?? "sshd"}\n" +
                                $"端口: {status.SshPort}";
            }
            else
            {
                SshDescription = "SSH (Secure Shell) 允许远程命令行访问。\n安装后可通过浏览器访问终端。";
            }
        }

        private async Task DeployUvncAsync()
        {
            IsInstalling = true;
            VncButtonText = "部署中...";
            try
            {
                var (success, message) = await _apiService.DeployUvncAsync();
                if (success)
                {
                    MessageBox.Show($"UltraVNC 部署成功！\n\n{message}\n\n现在可以启动 VNC 服务。",
                        "部署完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"部署失败:\n\n{message}",
                        "部署失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                await RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"部署异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsInstalling = false;
                VncButtonText = "部署 UltraVNC";
            }
        }

        private async Task InstallSshAsync()
        {
            try
            {
                // 使用 PowerShell 安装 OpenSSH Server
                var script = @"
                    # 检查是否已安装
                    $sshInstalled = Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Server*'
                    
                    if ($sshInstalled.State -ne 'Installed') {
                        # 安装 OpenSSH Server
                        Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
                    }
                    
                    # 启动服务
                    Start-Service sshd
                    
                    # 设置自动启动
                    Set-Service -Name sshd -StartupType Automatic
                    
                    Write-Output 'SSH_INSTALL_SUCCESS'
                ";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    RedirectStandardOutput = false,
                    CreateNoWindow = false
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    MessageBox.Show("SSH 服务安装成功！\n\n服务已自动启动。\n\n默认端口: 22", "安装完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"安装可能失败，请检查 PowerShell 输出。\n\n错误代码: {process.ExitCode}",
                        "安装提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartVncAsync()
        {
            var (success, message) = await _apiService.StartVncAsync();
            MessageBox.Show(message, success ? "启动成功" : "启动失败",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RefreshStatusAsync();
        }

        private async Task StopVncAsync()
        {
            var (success, message) = await _apiService.StopVncAsync();
            MessageBox.Show(message, success ? "停止成功" : "停止失败",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RefreshStatusAsync();
        }

        private async Task StartSshAsync()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "start sshd",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                await process.WaitForExitAsync();
                await RefreshStatusAsync();

                if (process.ExitCode == 0)
                    MessageBox.Show("SSH 服务已启动！", "启动成功", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("启动失败，请检查是否已安装 OpenSSH Server。", "启动失败",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StopSshAsync()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "stop sshd",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                await process.WaitForExitAsync();

                await RefreshStatusAsync();

                if (process.ExitCode == 0)
                    MessageBox.Show("SSH 服务已停止！", "停止成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsVncRunning()
        {
            // 检查 VNC 进程
            var vncProcesses = new[] { "winvnc", "vncserver", "tvnserver", "uvnc_service" };
            foreach (var procName in vncProcesses)
            {
                if (System.Diagnostics.Process.GetProcessesByName(procName).Length > 0)
                    return true;
            }

            // 检查端口
            return IsPortInUse(5900);
        }

        private bool IsSshRunning()
        {
            if (System.Diagnostics.Process.GetProcessesByName("sshd").Length > 0)
                return true;
            return IsPortInUse(22);
        }

        private bool IsPortInUse(int port)
        {
            try
            {
                var ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
                foreach (var endpoint in tcpConnInfoArray)
                {
                    if (endpoint.Port == port)
                        return true;
                }
            }
            catch { }
            return false;
        }

        private string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch { }
            return "未知";
        }

        #endregion
    }
}
