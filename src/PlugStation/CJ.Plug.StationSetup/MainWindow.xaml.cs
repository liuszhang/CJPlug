using System.Windows;
using System.Windows.Controls;
using CJ.Plug.StationSetup.Pages;
using CJ.Plug.StationSetup.ViewModels;

namespace CJ.Plug.StationSetup;

public partial class MainWindow : Window
{
    private readonly SetupViewModel _vm = new();
    private Page? _currentPage;
    private WelcomePage? _welcomePage;
    private InstallPathPage? _installPathPage;
    private ProgressPage? _progressPage;
    private CompletePage? _completePage;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        NavigateToWelcome();
    }

    private void NavigateToWelcome()
    {
        _welcomePage ??= new WelcomePage();
        _currentPage = _welcomePage;
        PageFrame.Content = _currentPage;
        BtnBack.Visibility = Visibility.Collapsed;
        BtnNext.Content = "下一步";
        BtnNext.IsEnabled = true;
    }

    private void NavigateToInstallPath()
    {
        _installPathPage ??= new InstallPathPage(_vm);
        _currentPage = _installPathPage;
        PageFrame.Content = _currentPage;
        BtnBack.Visibility = Visibility.Visible;
        BtnNext.Content = "安装";
    }

    private void NavigateToProgress()
    {
        _progressPage ??= new ProgressPage(_vm);
        _currentPage = _progressPage;
        PageFrame.Content = _currentPage;
        BtnBack.Visibility = Visibility.Collapsed;
        BtnNext.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        _progressPage.StartInstallation();
    }

    private void NavigateToComplete()
    {
        _completePage ??= new CompletePage(_vm);
        _currentPage = _completePage;
        PageFrame.Content = _currentPage;
        BtnBack.Visibility = Visibility.Collapsed;
        BtnNext.Content = "完成";
        BtnNext.IsEnabled = true;
        BtnCancel.Visibility = Visibility.Collapsed;
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigateToWelcome();
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage is WelcomePage)
        {
            NavigateToInstallPath();
        }
        else if (_currentPage is InstallPathPage)
        {
            NavigateToProgress();
        }
        else if (_currentPage is CompletePage)
        {
            Close();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.IsInstalling)
        {
            var result = MessageBox.Show("安装正在进行中，确定要取消吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
        }
        Close();
    }

    /// <summary>
    /// 由 ProgressPage 在安装完成后调用
    /// </summary>
    public void OnInstallationComplete()
    {
        Dispatcher.Invoke(NavigateToComplete);
    }
}
