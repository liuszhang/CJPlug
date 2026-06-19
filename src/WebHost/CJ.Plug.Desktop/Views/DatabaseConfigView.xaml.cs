using System.Windows.Controls;

namespace CJ.Plug.Desktop.Views;

public partial class DatabaseConfigView : UserControl
{
    private ViewModels.DatabaseConfigViewModel? _viewModel;

    public DatabaseConfigView()
    {
        InitializeComponent();
    }

    private void OnApiPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && _viewModel != null)
        {
            _viewModel.ApiPassword = pb.Password;
        }
    }

    private void OnElsaPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && _viewModel != null)
        {
            _viewModel.ElsaPassword = pb.Password;
        }
    }

    /// <summary>
    /// 设置 ViewModel 并保存引用，用于 PasswordBox 事件回写。
    /// </summary>
    internal void SetViewModel(ViewModels.DatabaseConfigViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }
}
