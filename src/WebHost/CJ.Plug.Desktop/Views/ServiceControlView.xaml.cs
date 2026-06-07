using System.Windows.Controls;

namespace CJ.Plug.Desktop.Views;

public partial class ServiceControlView : UserControl
{
    public ServiceControlView()
    {
        InitializeComponent();
    }

    private void OnConsoleTextChanged(object sender, TextChangedEventArgs e)
    {
        ConsoleTextBox.ScrollToEnd();
    }
}
