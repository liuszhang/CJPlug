using CommunityToolkit.Mvvm.ComponentModel;

namespace CJ.Plug.Desktop.ViewModels;

public partial class MenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
