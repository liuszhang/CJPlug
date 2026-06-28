using System.Windows;
using System.Windows.Input;
using CJ.Plug.Desktop.ViewModels;

namespace CJ.Plug.Desktop.Views;

public partial class UpgradeDialog : Window
{
    private readonly UpgradeViewModel _viewModel;

    public UpgradeDialog(UpgradeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.UpgradeCompleted += OnUpgradeCompleted;
        Loaded += async (_, _) => await _viewModel.CreateOrderCommand.ExecuteAsync(null);
    }

    private void OnUpgradeCompleted()
    {
        Dispatcher.Invoke(() => DialogResult = true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StopPolling();
        DialogResult = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            _viewModel.StopPolling();
            DialogResult = false;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.StopPolling();
        _viewModel.UpgradeCompleted -= OnUpgradeCompleted;
        base.OnClosed(e);
    }
}
