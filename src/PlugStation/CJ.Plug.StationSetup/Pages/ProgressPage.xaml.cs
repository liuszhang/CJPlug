using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CJ.Plug.StationSetup.Services;
using CJ.Plug.StationSetup.ViewModels;

namespace CJ.Plug.StationSetup.Pages;

public partial class ProgressPage : Page
{
    private readonly SetupViewModel _vm;

    public ProgressPage(SetupViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
    }

    public async void StartInstallation()
    {
        _vm.IsInstalling = true;
        _vm.ProgressValue = 0;

        var mainWindow = Window.GetWindow(this) as MainWindow;
        var installService = new InstallService();

        try
        {
            // 阶段1: 复制组件到安装目录
            _vm.StatusText = "正在复制组件文件...";
            _vm.ProgressValue = 10;
            await Task.Run(() => installService.Install(
                _vm.InstallPath,
                _vm.MainServerUrl,
                _vm.CreateDesktopShortcut,
                _vm.AutoStartService));
            _vm.ProgressValue = 100;

            _vm.StatusText = "安装完成！";
            _vm.InstallSucceeded = true;
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"安装失败: {ex.Message}";
            _vm.InstallSucceeded = false;
            Debug.WriteLine(ex.ToString());
        }
        finally
        {
            _vm.IsInstalling = false;
            _vm.InstallCompleted = true;
            mainWindow?.OnInstallationComplete();
        }
    }
}
