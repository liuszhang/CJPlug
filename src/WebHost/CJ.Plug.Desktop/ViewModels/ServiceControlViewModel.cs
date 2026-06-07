using System;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CJ.Plug.Desktop.Services;

namespace CJ.Plug.Desktop.ViewModels;

public partial class ServiceControlViewModel : ObservableObject
{
    private readonly AppHostLauncher _launcher;

    [ObservableProperty]
    private string _consoleOutput = string.Empty;

    [ObservableProperty]
    private bool _isAppHostRunning;

    [ObservableProperty]
    private string _statusText = "已停止";

    [ObservableProperty]
    private string _statusColor = "Red";

    public ServiceControlViewModel(AppHostLauncher launcher)
    {
        _launcher = launcher;
        _launcher.OutputReceived += OnOutputReceived;

        var buffered = _launcher.GetBufferedOutput();
        if (!string.IsNullOrEmpty(buffered))
            ConsoleOutput = buffered;

        UpdateStatus();
    }

    private void OnOutputReceived(string line)
    {
        Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => ConsoleOutput += line + Environment.NewLine);
    }

    private void UpdateStatus()
    {
        IsAppHostRunning = _launcher.IsRunning;
        if (IsAppHostRunning)
        {
            StatusText = "运行中";
            StatusColor = "Green";
        }
        else
        {
            StatusText = "已停止";
            StatusColor = "Red";
        }
    }

    /// <summary>外部（如 App 启动后）通知 ViewModel 刷新状态。</summary>
    internal void RefreshStatus() => UpdateStatus();

    [RelayCommand]
    private async Task Start()
    {
        StatusText = "启动中...";
        StatusColor = "Yellow";
        try
        {
            await _launcher.StartAsync();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            StatusText = "启动失败";
            StatusColor = "Red";
            ConsoleOutput += $"[ERROR] {ex.Message}{Environment.NewLine}";
        }
    }

    [RelayCommand]
    private async Task Stop()
    {
        try
        {
            await _launcher.StopAsync();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            ConsoleOutput += $"[ERROR] {ex.Message}{Environment.NewLine}";
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        ConsoleOutput = string.Empty;
    }

    [RelayCommand]
    private async Task Restart()
    {
        StatusText = "重启中...";
        StatusColor = "Yellow";
        try
        {
            await _launcher.RestartAsync();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            StatusText = "重启失败";
            StatusColor = "Red";
            ConsoleOutput += $"[ERROR] {ex.Message}{Environment.NewLine}";
        }
    }
}
