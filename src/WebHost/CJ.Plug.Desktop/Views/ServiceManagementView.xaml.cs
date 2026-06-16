using System.Windows.Controls;

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
        string dashboardUrl)
    {
        ControlPanelView.DataContext = serviceControlVm;
        DatabaseConfigView.SetViewModel(databaseConfigVm);
        await DashboardWebView.EnsureCoreWebView2Async(null);
        DashboardWebView.CoreWebView2.Navigate(dashboardUrl);
    }
}
