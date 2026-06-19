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

    /// <summary>
    /// 折叠模式下显示的图标字符（如 Unicode 符号或首字）。
    /// </summary>
    [ObservableProperty]
    private string _icon = string.Empty;
}
