using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CJ.Plug.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentUrl = "http://localhost:15288";

    [ObservableProperty]
    private bool _isServiceManagement;

    [ObservableProperty]
    private string _addressBarUrl = "http://localhost:15288";

    private MenuItemViewModel? _selectedMenuItem;

    public MenuItemViewModel? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value) && value != null)
            {
                IsServiceManagement = value.Name == "服务管理";
                if (!IsServiceManagement)
                {
                    CurrentUrl = value.Url;
                    AddressBarUrl = value.Url;
                }
                else
                {
                    AddressBarUrl = "";
                }
                foreach (var item in MenuItems)
                    item.IsSelected = item == value;
            }
        }
    }

    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = [];

    public MainViewModel()
    {
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "插头管理",
            Url = "http://localhost:5066/TAS?hideMenu=true"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "流程管理",
            Url = "http://localhost:5066/ProcessManageList?hideMenu=true"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "MCP Tool管理",
            Url = "http://localhost:5066/MCPToolManage?hideMenu=true"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "服务管理",
            Url = "http://localhost:15288"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "图站与工具",
            Url = "http://localhost:5066/StationAndTool?hideMenu=true"
        });

        SelectedMenuItem = MenuItems[3];
    }

    [RelayCommand]
    private void Navigate(MenuItemViewModel? item)
    {
        if (item == null) return;
        SelectedMenuItem = item;
    }
}
