using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace CJ.Plug.ExecuteApp;

/// <summary>
/// 插头执行独立窗口 —— 通过 WebView2 加载 PlugExecuteStandalone 页面。
/// 命令行参数: CJ.Plug.ExecuteApp.exe <baseUrl> <PlugDefinitionId> [JobDefinitionId]
///   例: CJ.Plug.ExecuteApp.exe https://localhost:5001 P100 J200
/// </summary>
public partial class MainWindow : Window
{
    private string? _baseUrl;
    private string? _plugDefinitionId;
    private string? _jobDefinitionId;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ParseCommandLineArgs();

        if (string.IsNullOrEmpty(_plugDefinitionId))
        {
            MessageBox.Show(
                "缺少必要参数 PlugDefinitionId。\n" +
                "用法: CJ.Plug.ExecuteApp.exe <baseUrl> <PlugDefinitionId> [JobDefinitionId]",
                "参数错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        await InitializeWebViewAsync();
    }

    private void ParseCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();

        // args[0] 是 exe 路径，args[1..] 是用户参数
        var userArgs = args.Skip(1).ToList();

        // 尝试自动检测 baseUrl（如果第一个参数看起来像 URL）
        if (userArgs.Count >= 1 && IsUrl(userArgs[0]))
        {
            _baseUrl = userArgs[0].TrimEnd('/');
            userArgs.RemoveAt(0);
        }

        if (userArgs.Count >= 1)
            _plugDefinitionId = userArgs[0];

        if (userArgs.Count >= 2)
            _jobDefinitionId = userArgs[1];
    }

    private static bool IsUrl(string input)
        => input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private async Task InitializeWebViewAsync()
    {
        var env = await CoreWebView2Environment.CreateAsync();
        await WebView.EnsureCoreWebView2Async(env);

        // 构建目标 URL，格式对应 PlugExecuteStandalone.razor 的路由:
        // /process/{PlugDefinitionId}/execute/{JobDefinitionId?}
        var url = string.IsNullOrEmpty(_jobDefinitionId)
            ? $"{_baseUrl}/process/{_plugDefinitionId}/execute"
            : $"{_baseUrl}/process/{_plugDefinitionId}/execute/{_jobDefinitionId}";

        Title = $"插头执行 - {_plugDefinitionId}";

        // 禁止 WebView2 右键菜单和开发者工具
        WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = Debugger.IsAttached;

        WebView.CoreWebView2.Navigate(url);
    }
}
