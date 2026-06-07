using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CJ.Plug.StationSetup.ViewModels;

namespace CJ.Plug.StationSetup.Pages;

public partial class CompletePage : Page
{
    private readonly SetupViewModel _vm;

    public CompletePage(SetupViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        if (_vm.InstallSucceeded)
        {
            TxtTitle.Text = "安装完成";
            TxtMessage.Text = "图站组件已成功安装到以下目录：";
            TxtInstallPath.Text = _vm.InstallPath;
        }
        else
        {
            TxtTitle.Text = "安装失败";
            TxtTitle.Foreground = System.Windows.Media.Brushes.Red;
            TxtMessage.Text = $"安装过程中出现错误：{_vm.StatusText}";
            TxtInstallPath.Text = "";
            ChkLaunchSettingUI.Visibility = Visibility.Collapsed;
        }
    }
}
