using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StationSettingUI.Components;

namespace StationSettingUI;

/// <summary>
/// 主窗口 - 图站管理工具
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;

    public MainWindow()
    {
        InitializeComponent();

        // 时钟更新
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30),
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        UpdateClock();
        _clockTimer.Start();
    }

    /// <summary>
    /// 窗口加载完成后初始化子组件
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            GlobalStatusText.Text = "正在初始化...";

            // 初始化子组件
            await Task.WhenAll(
                ServiceSettingCtrl.InitializeAsync(),
                ToolRegistrationCtrl.InitializeAsync(),
                TaskListCtrl.InitializeAsync()
            );

            GlobalStatusText.Text = "就绪";
        }
        catch (Exception ex)
        {
            GlobalStatusText.Text = $"初始化失败: {ex.Message}";
            MessageBox.Show($"初始化时发生错误:\n{ex.Message}", "初始化失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 窗口关闭前清理
    /// </summary>
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        _clockTimer.Stop();

        // 检查 StationApiServer 是否在运行
        var stationProcessNames = new[] { "CJ.Plug.StationApiServer", "CJ.Plug.StationApiServer.exe" };
        bool stationRunning = false;
        foreach (var name in stationProcessNames)
        {
            try
            {
                var procs = Process.GetProcessesByName(name);
                if (procs.Any(p => !p.HasExited))
                {
                    stationRunning = true;
                    break;
                }
            }
            catch { }
        }

        if (stationRunning)
        {
            var result = MessageBox.Show(
                "是否一并关闭 StationApiServer？",
                "关闭确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await ServiceSetting.KillStationServiceAsync();
            }
        }
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>
    /// 更新全局状态栏消息
    /// </summary>
    public void SetGlobalStatus(string message)
    {
        Dispatcher.Invoke(() =>
        {
            GlobalStatusText.Text = message;
        });
    }
}
