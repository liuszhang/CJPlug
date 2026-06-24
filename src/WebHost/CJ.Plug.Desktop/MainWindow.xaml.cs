using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace CJ.Plug.Desktop;

public partial class MainWindow : Window
{
    private const string WindowStateFileName = "window-state.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private bool _isExiting;
    private readonly Services.AppHostLauncher _appHostLauncher;

    private readonly ViewModels.ServiceControlViewModel _serviceControlVm;
    private readonly ViewModels.DatabaseConfigViewModel _databaseConfigVm;

    public MainWindow(
        ViewModels.MainViewModel viewModel,
        ViewModels.ServiceControlViewModel serviceControlVm,
        ViewModels.DatabaseConfigViewModel databaseConfigVm,
        Services.AppHostLauncher appHostLauncher)
    {
        _appHostLauncher = appHostLauncher;
        _serviceControlVm = serviceControlVm;
        _databaseConfigVm = databaseConfigVm;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModels.MainViewModel)DataContext;

        // 初始化主 WebView2
        await MainWebView.EnsureCoreWebView2Async(null);
        MainWebView.CoreWebView2.Navigate(vm.CurrentUrl);

        // WebView2 完整页面导航时更新地址栏
        MainWebView.CoreWebView2.NavigationStarting += (s, args) =>
        {
            vm.AddressBarUrl = args.Uri;
        };

        // 捕获 Blazor 客户端路由（history.pushState/replaceState/popstate）导致的 URL 变化，
        // 同步到 CurrentUrl 触发 OnCurrentUrlChanged → UpdateBreadcrumb。
        // 同时检测 /ProcessEdit/ 路由，若缺少 hideMenu=true 则追加参数重定向，
        // 使其使用 EmbeddedLayout（无菜单按钮），与流程管理列表页保持一致。
        MainWebView.CoreWebView2.HistoryChanged += (s, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                var url = MainWebView.CoreWebView2.Source;
                vm.CurrentUrl = url;

                if (url.Contains("/ProcessEdit/", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("hideMenu=true", StringComparison.OrdinalIgnoreCase))
                {
                    var sep = url.Contains('?') ? "&" : "?";
                    MainWebView.CoreWebView2.Navigate(url + sep + "hideMenu=true");
                }
            });
        };

        // 初始化服务管理视图（仪表盘 WebView2 + 控制面板 + 数据库配置）
        await ServiceManagementView.InitializeAsync(_serviceControlVm, _databaseConfigVm, "http://localhost:15288", "http://localhost:5066/SystemConfig");
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        RestoreWindowState();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveWindowState();

        if (_isExiting)
            return;

        if (_appHostLauncher.IsRunning)
        {
            e.Cancel = true; // 先阻止窗口关闭，待用户确认后再处理

            var result = MessageBox.Show(
                "AppHost 服务正在后台运行。\n\n是否一并关闭后台 AppHost 服务？\n\n选择「是」将停止所有服务并退出程序。\n选择「否」将返回窗口。",
                "CJ.Plug - 关闭确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                _isExiting = true;
                await _appHostLauncher.StopAsync();
                Close();
            }
            // 选择「否」则 e.Cancel=true 保持窗口不关闭
            return;
        }

        // AppHost 未运行，最小化到托盘
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// 强制退出应用（由托盘菜单"退出"调用）。
    /// </summary>
    public async void ForceExit()
    {
        _isExiting = true;
        if (_appHostLauncher.IsRunning)
            await _appHostLauncher.StopAsync();
        Close();
    }

    /// <summary>
    /// 从托盘恢复窗口显示。
    /// </summary>
    public void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private string GetWindowStatePath()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(dir, WindowStateFileName);
    }

    private void SaveWindowState()
    {
        try
        {
            var state = new WindowStateData
            {
                Width = RestoreBounds.Width > 0 ? RestoreBounds.Width : Width,
                Height = RestoreBounds.Height > 0 ? RestoreBounds.Height : Height,
                Left = RestoreBounds.Left > 0 ? RestoreBounds.Left : Left,
                Top = RestoreBounds.Top > 0 ? RestoreBounds.Top : Top,
                IsMaximized = WindowState == WindowState.Maximized
            };

            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(GetWindowStatePath(), json);
        }
        catch
        {
            // 静默忽略保存错误
        }
    }

    private void RestoreWindowState()
    {
        var statePath = GetWindowStatePath();
        if (!File.Exists(statePath)) return;

        try
        {
            var json = File.ReadAllText(statePath);
            var state = JsonSerializer.Deserialize<WindowStateData>(json, JsonOptions);
            if (state == null) return;

            if (state.Left > -10000 && state.Top > -10000
                && state.Width >= MinWidth && state.Height >= MinHeight)
            {
                Left = state.Left;
                Top = state.Top;
                Width = state.Width;
                Height = state.Height;
                WindowStartupLocation = WindowStartupLocation.Manual;

                if (state.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
            }
        }
        catch
        {
            // 静默忽略读取错误，使用默认值
        }
    }
}

internal class WindowStateData
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
}
