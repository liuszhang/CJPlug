using System.Windows.Controls;

namespace CJ.Plug.Desktop.Views;

public partial class ServiceManagementView : UserControl
{
    public ServiceManagementView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化服务管理视图：设置控制面板 ViewModel 并导航仪表盘 WebView2。
    /// </summary>
    public async Task InitializeAsync(
        ViewModels.ServiceControlViewModel serviceControlVm,
        string dashboardUrl)
    {
        ControlPanelView.DataContext = serviceControlVm;
        await DashboardWebView.EnsureCoreWebView2Async(null);
        DashboardWebView.CoreWebView2.Navigate(dashboardUrl);
    }
}
