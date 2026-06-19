using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CJ.Plug.Desktop.Models;

namespace CJ.Plug.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentUrl = "http://localhost:15288";

    [ObservableProperty]
    private bool _isServiceManagement;

    [ObservableProperty]
    private string _addressBarUrl = "http://localhost:15288";

    [ObservableProperty]
    private bool _showBreadcrumb;

    /// <summary>
    /// 左侧菜单是否折叠（收起为仅图标模式）。
    /// </summary>
    [ObservableProperty]
    private bool _isMenuCollapsed;

    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = [];

    /// <summary>
    /// CurrentUrl 变化时自动解析 URL 更新面包屑（由 CommunityToolkit.Mvvm 源生成器调用）。
    /// </summary>
    partial void OnCurrentUrlChanged(string value)
    {
        AddressBarUrl = value;
        UpdateBreadcrumb(value);
    }

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
                }
                else
                {
                    AddressBarUrl = "";
                    UpdateBreadcrumb("");
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
            Url = "http://localhost:5066/TAS?hideMenu=true",
            Icon = "\u2699"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "流程管理",
            Url = "http://localhost:5066/ProcessManageList?hideMenu=true",
            Icon = "\u2395"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "MCP Tool管理",
            Url = "http://localhost:5066/MCPToolManage?hideMenu=true",
            Icon = "\u2328"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "服务管理",
            Url = "http://localhost:15288",
            Icon = "\u2691"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "图站管理",
            Url = "http://localhost:5066/StationManage?hideMenu=true",
            Icon = "\u2606"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "工具资源",
            Url = "http://localhost:5066/ToolResource?hideMenu=true",
            Icon = "\u2692"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "AI对话",
            Url = "http://localhost:5066/AskAI?hideMenu=true",
            Icon = "\u2601"
        });
        MenuItems.Add(new MenuItemViewModel
        {
            Name = "LLM配置",
            Url = "http://localhost:5066/LlmConfig?hideMenu=true",
            Icon = "\u2693"
        });

        SelectedMenuItem = MenuItems[3];
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuCollapsed = !IsMenuCollapsed;
    }

    [RelayCommand]
    private void Navigate(MenuItemViewModel? item)
    {
        if (item == null) return;
        SelectedMenuItem = item;
    }

    /// <summary>
    /// 根据 WebView2 当前 URL 更新面包屑导航。
    /// </summary>
    public void UpdateBreadcrumb(string url)
    {
        BreadcrumbItems.Clear();
        ShowBreadcrumb = false;

        if (string.IsNullOrEmpty(url)) return;

        // 流程编辑页：/ProcessEdit/{DefinitionId}
        var editMatch = Regex.Match(url, @"/ProcessEdit/([^/?#]+)", RegexOptions.IgnoreCase);
        if (editMatch.Success)
        {
            var processId = editMatch.Groups[1].Value;
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Text = "流程管理",
                NavigateUrl = "http://localhost:5066/ProcessManageList?hideMenu=true",
                IsFirst = true
            });
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Text = $"流程编辑({processId})",
                IsFirst = false
            });
            ShowBreadcrumb = true;
            return;
        }

        // 流程清单页：/ProcessManageList
        if (url.Contains("/ProcessManageList", StringComparison.OrdinalIgnoreCase))
        {
            BreadcrumbItems.Add(new BreadcrumbItem
            {
                Text = "流程管理",
                IsFirst = true
            });
            ShowBreadcrumb = true;
        }
    }

    [RelayCommand]
    private void NavigateBreadcrumb(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        CurrentUrl = url;
    }
}
