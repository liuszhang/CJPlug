using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using StationSettingUI.Helpers;
using StationSettingUI.Models;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// 服务配置组件 - 管理平台连接和图站服务状态
/// </summary>
public partial class ServiceSetting : UserControl
{
    private readonly StationSettingUI.Services.StationConfigService _configService;
    private readonly StationSettingUI.Services.StationApiService _apiService;
    private AppConfig _config;
    private Process? _serviceProcess;
    private bool _isInitialized;
    private readonly DispatcherTimer _autoSaveTimer;
    private bool _suppressAutoSave;
    private bool _existingLogLoaded;

    // StationApiServer 的可能进程名
    private static readonly string[] StationProcessNames = new[]
    {
        "CJ.Plug.StationApiServer",
        "CJ.Plug.StationApiServer.exe",
    };

    public ServiceSetting()
    {
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;
        _suppressAutoSave = true;

        InitializeComponent();

        _configService = new StationSettingUI.Services.StationConfigService();
        _config = _configService.LoadConfig();
        _apiService = new StationApiService(_config);

        _suppressAutoSave = false;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _suppressAutoSave = true;

        TxtMainServerUrl.Text = _config.MainServerUrl;
        TxtStationFolder.Text = _config.StationApiFolder;
        TxtStationPort.Text = _config.StationApiPort.ToString();
        TxtToolsRootPath.Text = _config.ToolsRootPath;
        TxtVersion.Text = AppConfig.AppVersion;
        ChkAutoStart.IsChecked = _config.AutoStartService;
        TxtStationApiUrl.Text = $"({_config.StationApiUrl})";

        _suppressAutoSave = false;

        await RefreshServiceStatusAsync();
    }

    private async Task RefreshServiceStatusAsync()
    {
        var isRunning = await _apiService.TestStationApiAsync();

        if (isRunning)
        {
            StatusDot.Fill = new SolidColorBrush(Colors.LimeGreen);
            TxtServiceStatus.Text = "本地服务: 运行中";
            TxtServiceStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 0));
            BtnStartService.IsEnabled = false;
            BtnStopService.IsEnabled = true;
            BtnRestartService.IsEnabled = true;

            // 加载已有日志 — 仅当进程不是由本工具启动时
            if (_serviceProcess == null && !_existingLogLoaded)
            {
                LoadExistingLogFile();
                _existingLogLoaded = true;
            }

            var connStatus = await _apiService.GetConnectionStatusAsync();
            if (connStatus != null)
            {
                if (connStatus.HubConnected)
                {
                    HubStatusDot.Fill = new SolidColorBrush(Colors.LimeGreen);
                    TxtHubStatus.Text = $"平台连接: 已连接 ({connStatus.MainServerUrl})";
                    TxtHubStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                    SetStatusMessage("运行正常，平台已连接", false);
                }
                else
                {
                    HubStatusDot.Fill = new SolidColorBrush(Colors.Orange);
                    TxtHubStatus.Text = $"平台连接: 未连接 ({connStatus.MainServerUrl})";
                    TxtHubStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    SetStatusMessage("本地运行中，平台未连接", true);
                }
            }
            else
            {
                HubStatusDot.Fill = new SolidColorBrush(Colors.Gray);
                TxtHubStatus.Text = "平台连接: 状态未知";
                TxtHubStatus.Foreground = new SolidColorBrush(Colors.Gray);
                SetStatusMessage("运行正常", false);
            }
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(Colors.Red);
            TxtServiceStatus.Text = "本地服务: 已停止";
            TxtServiceStatus.Foreground = new SolidColorBrush(Colors.Red);
            BtnStartService.IsEnabled = true;
            BtnStopService.IsEnabled = false;
            BtnRestartService.IsEnabled = false;

            HubStatusDot.Fill = new SolidColorBrush(Colors.Gray);
            TxtHubStatus.Text = "平台连接: --";
            TxtHubStatus.Foreground = new SolidColorBrush(Colors.Gray);
            SetStatusMessage("图站服务未运行", true);
        }
    }

    // ==================== 按钮事件 ====================

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _config.MainServerUrl = TxtMainServerUrl.Text.Trim();
            _config.StationApiFolder = TxtStationFolder.Text.Trim();
            if (int.TryParse(TxtStationPort.Text.Trim(), out var port))
                _config.StationApiPort = port;
            _config.ToolsRootPath = TxtToolsRootPath.Text.Trim();
            _configService.SaveConfig();
            _apiService.UpdateBaseAddress();
            TxtStationApiUrl.Text = $"({_config.StationApiUrl})";
            SetStatusMessage("配置已保存", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"保存失败: {ex.Message}", true);
        }
    }

    private void ConfigField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressAutoSave) return;
        StartAutoSaveTimer();
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "导入配置",
            Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var imported = JsonSerializer.Deserialize<AppConfig>(json);
            if (imported == null)
            {
                SetStatusMessage("导入失败: JSON 格式不正确或配置数据为空", true);
                return;
            }

            // 基本校验
            if (string.IsNullOrWhiteSpace(imported.MainServerUrl))
            {
                SetStatusMessage("导入失败: 缺少必填字段 MainServerUrl", true);
                return;
            }
            if (imported.StationApiPort <= 0 || imported.StationApiPort > 65535)
            {
                SetStatusMessage("导入失败: StationApiPort 端口号无效", true);
                return;
            }

            // 更新配置
            _config.MainServerUrl = imported.MainServerUrl;
            _config.StationApiPort = imported.StationApiPort;
            _config.StationApiFolder = imported.StationApiFolder ?? "";
            _config.ToolsRootPath = imported.ToolsRootPath ?? @"C:\Program Files\CJTools";
            _config.AutoStartService = imported.AutoStartService;
            _config.LastKnownVersion = imported.LastKnownVersion;

            // 更新 UI（抑制自动保存）
            _suppressAutoSave = true;
            TxtMainServerUrl.Text = _config.MainServerUrl;
            TxtStationFolder.Text = _config.StationApiFolder;
            TxtStationPort.Text = _config.StationApiPort.ToString();
            TxtToolsRootPath.Text = _config.ToolsRootPath;
            ChkAutoStart.IsChecked = _config.AutoStartService;
            _suppressAutoSave = false;

            _configService.SaveConfig();
            SetStatusMessage("配置导入成功", false);
        }
        catch (JsonException)
        {
            SetStatusMessage("导入失败: JSON 格式不正确", true);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"导入失败: {ex.Message}", true);
        }
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        // 先将当前 UI 值同步到配置对象
        _config.MainServerUrl = TxtMainServerUrl.Text.Trim();
        _config.StationApiFolder = TxtStationFolder.Text.Trim();
        if (int.TryParse(TxtStationPort.Text.Trim(), out var port))
            _config.StationApiPort = port;
        _config.ToolsRootPath = TxtToolsRootPath.Text.Trim();

        var dlg = new SaveFileDialog
        {
            Title = "导出配置",
            Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json",
            FileName = "StationConfig.json"
        };

        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(dlg.FileName, json);
            SetStatusMessage($"配置已导出到: {dlg.FileName}", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"导出失败: {ex.Message}", true);
        }
    }

    private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        BtnTestConnection.IsEnabled = false;
        SetStatusMessage("正在测试平台连接...", false);
        var (success, message) = await _apiService.TestMainServerAsync();
        BtnTestConnection.IsEnabled = true;
        SetStatusMessage(message, !success);
        await RefreshServiceStatusAsync();
    }

    private async void BtnStartService_Click(object sender, RoutedEventArgs e)
    {
        BtnStartService.IsEnabled = false;
        SetStatusMessage("正在启动图站服务...", false);

        var logService = ((App)Application.Current).LogService;

        try
        {
            var stationPath = FindStationApiExe();
            if (stationPath == null)
            {
                SetStatusMessage("未找到 StationApiServer，请确认已部署", true);
                BtnStartService.IsEnabled = true;
                return;
            }

            var isDll = stationPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
            var fileName = isDll ? "dotnet" : stationPath;
            var portArg = $"--port {_config.StationApiPort}";
            var arguments = isDll ? $"{stationPath} {portArg}" : portArg;

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(stationPath) ?? "",
            };

            logService.AppendLine($"========== 启动 StationApiServer ==========");
            logService.AppendLine($"命令: {fileName} {arguments}");
            logService.AppendLine($"端口: {_config.StationApiPort}");
            logService.AppendLine($"工作目录: {startInfo.WorkingDirectory}");

            _serviceProcess = Process.Start(startInfo)!;

            // 立即开始读取子进程的控制台输出（避免缓冲区堵塞）
            StartReadingProcessOutput(_serviceProcess);

            // 等待启动完成
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(500);
                if (_serviceProcess.HasExited)
                {
                    logService.AppendLine("进程异常退出", isError: true);
                    SetStatusMessage("启动失败，进程已退出", true);
                    BtnStartService.IsEnabled = true;
                    await RefreshServiceStatusAsync();
                    return;
                }
                if (await _apiService.TestStationApiAsync())
                {
                    logService.AppendLine($"[OK] StationApiServer HTTP 已就绪");
                    SetStatusMessage("启动成功", false);
                    await RefreshServiceStatusAsync();
                    return;
                }
            }
            logService.AppendLine("[WARN] 启动超时，请检查端口", isError: true);
            SetStatusMessage("启动超时，请检查端口占用", true);
        }
        catch (Exception ex)
        {
            logService.AppendLine($"[ERROR] {ex.Message}", isError: true);
            SetStatusMessage($"启动失败: {ex.Message}", true);
        }

        BtnStartService.IsEnabled = true;
        await RefreshServiceStatusAsync();
    }

    private async void BtnStopService_Click(object sender, RoutedEventArgs e)
    {
        BtnStopService.IsEnabled = false;
        BtnRestartService.IsEnabled = false;
        SetStatusMessage("正在停止图站服务...", false);

        var killed = await KillStationServiceAsync(_config.StationApiPort);

        await Task.Delay(500);
        await RefreshServiceStatusAsync();

        if (killed)
            SetStatusMessage("图站服务已停止", false);
        else
            SetStatusMessage("未找到运行中的服务进程", false);
    }

    private async void BtnRestartService_Click(object sender, RoutedEventArgs e)
    {
        BtnRestartService.IsEnabled = false;
        BtnStartService.IsEnabled = false;
        BtnStopService.IsEnabled = false;
        SetStatusMessage("正在重启图站服务...", false);

        await KillStationServiceAsync(_config.StationApiPort);
        await Task.Delay(1000);

        BtnStartService_Click(sender, e);
    }

    private async void BtnServiceTest_Click(object sender, RoutedEventArgs e)
    {
        BtnServiceTest.IsEnabled = false;
        SetStatusMessage("正在测试...", false);
        var running = await _apiService.TestStationApiAsync();
        SetStatusMessage(running ? "本地服务响应正常" : "本地服务无响应", !running);
        BtnServiceTest.IsEnabled = true;
        await RefreshServiceStatusAsync();
    }

    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnCheckUpdate.IsEnabled = false;
        BtnDownloadUpdate.Visibility = Visibility.Collapsed;
        TxtUpdateStatus.Text = "正在检查...";
        TxtUpdateStatus.Foreground = new SolidColorBrush(Colors.RoyalBlue);

        var (hasUpdate, latestVersion, message) = await _apiService.CheckUpdateAsync();

        BtnCheckUpdate.IsEnabled = true;
        TxtUpdateStatus.Text = message;

        if (hasUpdate)
        {
            TxtUpdateStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
            BtnDownloadUpdate.Visibility = Visibility.Visible;
        }
        else
        {
            TxtUpdateStatus.Foreground = new SolidColorBrush(Colors.Green);
            BtnDownloadUpdate.Visibility = Visibility.Collapsed;
        }
    }

    private CancellationTokenSource? _downloadCts;

    private async void BtnDownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnDownloadUpdate.IsEnabled = false;
        BtnCheckUpdate.IsEnabled = false;
        PanelDownloadProgress.Visibility = Visibility.Visible;
        ProgressDownload.Value = 0;
        TxtProgressPercent.Text = "0%";
        TxtProgressLabel.Text = "正在生成部署包并下载...";

        _downloadCts = new CancellationTokenSource();

        try
        {
            var platform = Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86";
            var saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            // 下载进度回调（服务端打包期间进度条保持为 0，响应头到达后才有 ContentLength 追踪下载进度）
            var downloadProgress = new Progress<int>(percent =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressDownload.Value = percent;
                    TxtProgressPercent.Text = $"{percent}%";
                    TxtProgressLabel.Text = percent < 100
                        ? $"正在下载... {percent}%"
                        : "下载完成";
                });
            });

            // 一步完成：服务端打包完成后直接返回文件流
            var filePath = await _apiService.DownloadStationPackageDirectAsync(
                platform, saveDir, downloadProgress, _downloadCts.Token);

            if (string.IsNullOrEmpty(filePath))
            {
                SetStatusMessage("下载部署包失败，请确认主服务端运行正常", true);
                ResetDownloadUi();
                return;
            }

            ProgressDownload.Value = 100;
            TxtProgressPercent.Text = "100%";
            TxtProgressLabel.Text = "下载完成";
            SetStatusMessage($"部署包已下载到: {filePath}", false);

            // 询问是否打开下载目录
            var result = System.Windows.MessageBox.Show(
                $"图站部署包已下载完成。\n\n文件: {filePath}\n\n是否打开下载目录?",
                "下载完成",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }
        catch (OperationCanceledException)
        {
            SetStatusMessage("下载已取消", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"下载失败: {ex.Message}", true);
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
            ResetDownloadUi();
        }
    }

    private void ResetDownloadUi()
    {
        PanelDownloadProgress.Visibility = Visibility.Collapsed;
        BtnDownloadUpdate.IsEnabled = true;
        BtnCheckUpdate.IsEnabled = true;
    }

    private void BtnBrowsePath_Click(object sender, RoutedEventArgs e)
    {
        var path = FolderBrowserHelper.ShowDialog(Window.GetWindow(this), "选择工具安装根目录", TxtToolsRootPath.Text);
        if (!string.IsNullOrEmpty(path)) TxtToolsRootPath.Text = path;
    }

    private void BtnBrowseStation_Click(object sender, RoutedEventArgs e)
    {
        var path = FolderBrowserHelper.ShowDialog(Window.GetWindow(this), "选择 StationApiServer 所在文件夹", TxtStationFolder.Text);
        if (!string.IsNullOrEmpty(path)) TxtStationFolder.Text = path;
    }

    private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressAutoSave) return;
        StartAutoSaveTimer();
    }

    // ==================== 自动保存（防抖 500ms）====================

    private void StartAutoSaveTimer()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTimerTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();

        try
        {
            _config.MainServerUrl = TxtMainServerUrl.Text.Trim();
            _config.StationApiFolder = TxtStationFolder.Text.Trim();
            if (int.TryParse(TxtStationPort.Text.Trim(), out var port))
                _config.StationApiPort = port;
            _config.ToolsRootPath = TxtToolsRootPath.Text.Trim();
            _config.AutoStartService = ChkAutoStart.IsChecked == true;

            _configService.SaveConfig();
            _apiService.UpdateBaseAddress();
            TxtStationApiUrl.Text = $"({_config.StationApiUrl})";
            SetStatusMessage("配置已自动保存", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"自动保存失败: {ex.Message}", true);
        }
    }

    // ==================== 服务进程管理 ====================

    /// <summary>
    /// 杀掉 StationApiServer 进程（多种策略尝试）
    /// </summary>
    internal static async Task<bool> KillStationServiceAsync(int port = 7660)
    {
        // 策略 1: 按进程名杀
        foreach (var name in StationProcessNames)
        {
            try
            {
                var procs = Process.GetProcessesByName(name);
                foreach (var p in procs)
                {
                    if (!p.HasExited)
                    {
                        p.Kill(entireProcessTree: true);
                        await p.WaitForExitAsync();
                        return true;
                    }
                }
            }
            catch { /* 继续下一个 */ }
        }

        // 策略 2: 杀 dotnet 进程中可能是 StationApiServer 的（用 wmic 查命令行）
        try
        {
            var dotnetProcs = Process.GetProcessesByName("dotnet");
            foreach (var p in dotnetProcs)
            {
                try
                {
                    if (p.HasExited) continue;
                    var cmdLine = GetCommandLineViaWmic(p.Id);
                    if (cmdLine != null &&
                        cmdLine.Contains("StationApiServer", StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kill(entireProcessTree: true);
                        await p.WaitForExitAsync();
                        return true;
                    }
                }
                catch { }
            }
        }
        catch { }

        // 策略 3: 用 taskkill 暴力杀（通过端口去查 PID）
        try { return await KillByPortAsync(port); }
        catch { }

        return false;
    }

    /// <summary>
    /// 通过 wmic 获取进程命令行（不依赖 System.Management）
    /// </summary>
    private static string? GetCommandLineViaWmic(int pid)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = $"process where processid={pid} get commandline /format:list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);

            // 输出格式: "CommandLine=dotnet CJ.Plug.StationApiServer.dll"
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("CommandLine=", StringComparison.OrdinalIgnoreCase))
                    return line["CommandLine=".Length..].Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 通过端口号杀进程（调 taskkill）
    /// </summary>
    private static async Task<bool> KillByPortAsync(int port)
    {
        // netstat -ano | findstr ":{port} "
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c netstat -ano | findstr \":{port} \"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        if (proc == null) return false;

        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        // 提取所有 LISTENING 行的 PID
        var pids = new HashSet<int>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains("LISTENING")) continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[^1], out var pid))
                pids.Add(pid);
        }

        foreach (var pid in pids)
        {
            try { Process.GetProcessById(pid).Kill(entireProcessTree: true); }
            catch { }
        }

        // 等一会儿确认端口释放
        await Task.Delay(500);
        return pids.Count > 0;
    }

    // ==================== 工具方法 ====================

    private void SetStatusMessage(string message, bool isError)
    {
        TxtStatusMessage.Text = message;
        TxtStatusMessage.Foreground = isError
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// 异步读取子进程的 stdout/stderr 并推送到日志服务
    /// </summary>
    private void StartReadingProcessOutput(Process process)
    {
        var logService = ((App)Application.Current).LogService;

        // 读取 stdout
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    logService.AppendLine(line);
                }
            }
            catch (Exception ex)
            {
                logService.AppendLine($"[stdout read error] {ex.Message}", isError: true);
            }
        });

        // 读取 stderr
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line == null) break;
                    logService.AppendLine(line, isError: true);
                }
            }
            catch (Exception ex)
            {
                logService.AppendLine($"[stderr read error] {ex.Message}", isError: true);
            }
        });
    }

    private string? FindStationApiExe()
    {
        // 优先使用配置的文件夹
        if (!string.IsNullOrWhiteSpace(_config.StationApiFolder))
        {
            var configured = Path.GetFullPath(_config.StationApiFolder);
            // 尝试 .exe
            var exe = Path.Combine(configured, "CJ.Plug.StationApiServer.exe");
            if (File.Exists(exe)) return exe;
            // 尝试 .dll（dotnet 启动）
            var dll = Path.Combine(configured, "CJ.Plug.StationApiServer.dll");
            if (File.Exists(dll)) return dll;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exePaths = new[]
        {
            // 优先找 .exe
            Path.Combine(baseDir, "CJ.Plug.StationApiServer.exe"),
            Path.Combine(baseDir, "..", "CJ.Plug.StationApiServer", "CJ.Plug.StationApiServer.exe"),
            // 开发环境: 找 .dll
            Path.Combine(baseDir, "..", "..", "..", "..", "PlugStation", "CJ.Plug.StationApiServer"),
        };

        foreach (var path in exePaths)
        {
            try
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full)) return full;

                if (Directory.Exists(full))
                {
                    var dll = Path.Combine(full, "CJ.Plug.StationApiServer.dll");
                    if (File.Exists(dll)) return dll;
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// 加载 StationApiServer 已有的 StationLogs/log.txt 日志文件内容
    /// 仅当进程不是由本工具启动时调用，处理文件不存在的情况（静默跳过）
    /// </summary>
    private void LoadExistingLogFile()
    {
        if (string.IsNullOrWhiteSpace(_config.StationApiFolder))
            return;

        var logService = ((App)Application.Current).LogService;
        var logFile = Path.Combine(_config.StationApiFolder, "StationLogs", "log.txt");

        if (!File.Exists(logFile))
            return;

        try
        {
            var lines = File.ReadAllLines(logFile, System.Text.Encoding.UTF8);
            logService.AppendLine("========== 已加载服务历史日志 ==========");
            foreach (var line in lines.TakeLast(200))
            {
                logService.AppendLine(line);
            }
            logService.AppendLine($"========== 共 {Math.Min(lines.Length, 200)} 行 ==========");
        }
        catch (Exception ex)
        {
            logService.AppendLine($"[WARN] 读取日志文件失败: {ex.Message}", isError: true);
        }
    }
}
