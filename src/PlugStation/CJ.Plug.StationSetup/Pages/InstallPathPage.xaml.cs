using System.Windows;
using System.Windows.Controls;
using CJ.Plug.StationSetup.ViewModels;
using Microsoft.Win32;

namespace CJ.Plug.StationSetup.Pages;

public partial class InstallPathPage : Page
{
    private readonly SetupViewModel _vm;

    public InstallPathPage(SetupViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择安装路径",
            InitialDirectory = _vm.InstallPath
        };

        if (dialog.ShowDialog() == true)
        {
            _vm.InstallPath = dialog.FolderName;
        }
    }
}
