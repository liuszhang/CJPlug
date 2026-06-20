using System.Windows;

namespace CJ.Plug.ExecuteApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 确保 WebView2 运行时可用
        var webView2EnvPath = Microsoft.Web.WebView2.Core.CoreWebView2Environment
            .GetAvailableBrowserVersionString();
    }
}
