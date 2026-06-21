using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace CJ.Plug.Desktop.Views;

public partial class ServiceManagementView : UserControl
{
    public ServiceManagementView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化服务管理视图：设置控制面板 ViewModel、数据库配置 ViewModel 并导航仪表盘 WebView2。
    /// </summary>
    public async Task InitializeAsync(
        ViewModels.ServiceControlViewModel serviceControlVm,
        ViewModels.DatabaseConfigViewModel databaseConfigVm,
        string dashboardUrl,
        string systemConfigUrl)
    {
        ControlPanelView.DataContext = serviceControlVm;
        DatabaseConfigView.SetViewModel(databaseConfigVm);
        await DashboardWebView.EnsureCoreWebView2Async(null);
        DashboardWebView.CoreWebView2.Navigate(dashboardUrl);

        await SystemConfigWebView.EnsureCoreWebView2Async(null);

        // 注册文件浏览 JS 函数，通过 WebView2 消息通信实现 WPF 文件对话框
        SystemConfigWebView.CoreWebView2.WebMessageReceived += OnSystemConfigWebMessage;
        await SystemConfigWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            window.systemConfig = {
                browseFile: function() {
                    return new Promise(function(resolve) {
                        window._systemConfigResolve = resolve;
                        window.chrome.webview.postMessage('browseFile');
                    });
                }
            };
            window.chrome.webview.addEventListener('message', function(e) {
                if (window._systemConfigResolve) {
                    window._systemConfigResolve(e.data);
                    window._systemConfigResolve = null;
                }
            });
        ");

        SystemConfigWebView.CoreWebView2.Navigate(systemConfigUrl);
    }

    private void OnSystemConfigWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.WebMessageAsJson?.Contains("browseFile") == true)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                Title = "选择 CJ.Plug.ExecuteApp.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            if (dlg.ShowDialog() == true)
            {
                SystemConfigWebView.Dispatcher.Invoke(() =>
                {
                    SystemConfigWebView.CoreWebView2.PostWebMessageAsString(dlg.FileName);
                });
            }
            else
            {
                SystemConfigWebView.Dispatcher.Invoke(() =>
                {
                    SystemConfigWebView.CoreWebView2.PostWebMessageAsString("");
                });
            }
        }
    }
}
