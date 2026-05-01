using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CJ.Plug.Models.Station;
using StationSettingUI.Models;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// 工具注册组件 - 管理图站上的工具安装和配置
/// </summary>
public partial class ToolRegistration : UserControl
{
    private readonly StationConfigService _configService;
    private readonly StationApiService _apiService;
    private AppConfig _config;
    private bool _isInitialized;

    public ObservableCollection<ToolRegistrationModel> Tools { get; set; } = new();

    public ToolRegistration()
    {
        InitializeComponent();
        _configService = new StationConfigService();
        _config = _configService.LoadConfig();
        _apiService = new StationApiService(_config);
        GridTools.ItemsSource = Tools;
    }

    /// <summary>
    /// 初始化组件，加载工具列表
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        await RefreshToolsAsync();
    }

    /// <summary>
    /// 刷新工具列表
    /// </summary>
    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshToolsAsync();
    }

    /// <summary>
    /// 从主服务器加载工具列表
    /// </summary>
    private async Task RefreshToolsAsync()
    {
        TxtToolbarStatus.Text = "正在加载...";

        try
        {
            var tools = await _apiService.FetchToolsFromServerAsync();
            Tools.Clear();

            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    Tools.Add(new ToolRegistrationModel(tool));
                }
            }

            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
            TxtToolbarStatus.Text = tools != null ? "加载完成" : "服务器无响应，显示本地缓存";
        }
        catch (Exception ex)
        {
            TxtToolbarStatus.Text = $"加载失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 自动搜索本机已安装的工具
    /// </summary>
    private async void BtnAutoSearch_Click(object sender, RoutedEventArgs e)
    {
        BtnAutoSearch.IsEnabled = false;
        TxtToolbarStatus.Text = "正在搜索本机工具...";

        var foundTools = await Task.Run(() => AutoDetectLocalTools());

        if (foundTools.Count > 0)
        {
            foreach (var tool in foundTools)
            {
                // 避免重复添加
                if (!Tools.Any(t => t.ToolName == tool.ToolName && t.ToolVersion == tool.ToolVersion))
                {
                    Tools.Add(new ToolRegistrationModel(tool));
                }
            }
            TxtToolbarStatus.Text = $"搜索完成，找到 {foundTools.Count} 个新工具";
            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
        }
        else
        {
            TxtToolbarStatus.Text = "未发现新工具";
        }

        BtnAutoSearch.IsEnabled = true;
    }

    /// <summary>
    /// 从平台服务器下载工具配置
    /// </summary>
    private async void BtnFetchFromServer_Click(object sender, RoutedEventArgs e)
    {
        BtnFetchFromServer.IsEnabled = false;
        TxtToolbarStatus.Text = "正在从服务器获取工具列表...";

        try
        {
            var serverTools = await _apiService.FetchToolsFromServerAsync();
            if (serverTools != null && serverTools.Count > 0)
            {
                foreach (var tool in serverTools)
                {
                    // 下载每个工具的配置文件
                    if (tool.Id.HasValue)
                        await _apiService.DownloadToolConfigAsync(tool.Id.Value);

                    // 添加到本地列表
                    if (!Tools.Any(t => t.ToolName == tool.ToolName && t.ToolVersion == tool.ToolVersion))
                    {
                        Tools.Add(new ToolRegistrationModel(tool));
                    }
                }
                TxtToolbarStatus.Text = $"从服务器同步了 {serverTools.Count} 个工具";
                TxtToolCount.Text = $"共 {Tools.Count} 个工具";
            }
            else
            {
                TxtToolbarStatus.Text = "服务器上没有可用的工具配置";
            }
        }
        catch (Exception ex)
        {
            TxtToolbarStatus.Text = $"获取失败: {ex.Message}";
        }

        BtnFetchFromServer.IsEnabled = true;
    }

    /// <summary>
    /// 添加新工具
    /// </summary>
    private void BtnAddTool_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ToolEditDialog();
        if (dialog.ShowDialog() == true && dialog.Tool != null)
        {
            Tools.Add(new ToolRegistrationModel(dialog.Tool));
            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
            TxtToolbarStatus.Text = "工具已添加（点击「应用」保存）";
        }
    }

    /// <summary>
    /// 编辑选中工具
    /// </summary>
    private void BtnEditTool_Click(object sender, RoutedEventArgs e)
    {
        var selected = GridTools.SelectedItem as ToolRegistrationModel;
        if (selected == null)
        {
            TxtToolbarStatus.Text = "请先选择一个工具";
            return;
        }

        var dialog = new ToolEditDialog(selected.Tool);
        if (dialog.ShowDialog() == true && dialog.Tool != null)
        {
            // 替换列表中的项
            var index = Tools.IndexOf(selected);
            if (index >= 0)
            {
                Tools[index] = new ToolRegistrationModel(dialog.Tool);
            }
            TxtToolbarStatus.Text = "工具已修改（点击「应用」保存）";
        }
    }

    /// <summary>
    /// 删除选中工具
    /// </summary>
    private async void BtnDeleteTool_Click(object sender, RoutedEventArgs e)
    {
        var selected = GridTools.SelectedItem as ToolRegistrationModel;
        if (selected == null)
        {
            TxtToolbarStatus.Text = "请先选择一个工具";
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除工具 \"{selected.ToolName}\" 吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        if (selected.Id.HasValue && selected.Id > 0)
        {
            var deleted = await _apiService.DeleteToolAsync(selected.Id.Value);
            if (!deleted)
            {
                TxtToolbarStatus.Text = "删除失败，请检查服务连接";
                return;
            }
        }

        Tools.Remove(selected);
        TxtToolCount.Text = $"共 {Tools.Count} 个工具";
        TxtToolbarStatus.Text = "工具已删除";
    }

    /// <summary>
    /// 应用更改 - 将所有工具变更保存到 StationApiServer
    /// </summary>
    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        BtnApply.IsEnabled = false;
        TxtToolbarStatus.Text = "正在保存...";

        int successCount = 0;
        int failCount = 0;

        foreach (var toolModel in Tools)
        {
            var saved = await _apiService.SaveToolAsync(toolModel.Tool);
            if (saved)
                successCount++;
            else
                failCount++;
        }

        BtnApply.IsEnabled = true;
        TxtToolbarStatus.Text = $"保存完成: {successCount} 成功, {failCount} 失败";
        await RefreshToolsAsync();
    }

    /// <summary>
    /// DataGrid 选择变更
    /// </summary>
    private void GridTools_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        var selected = GridTools.SelectedItem as ToolRegistrationModel;
        if (selected != null)
        {
            TxtSelectedTool.Text = $"已选择: {selected.ToolName} v{selected.ToolVersion}";
        }
        else
        {
            TxtSelectedTool.Text = "";
        }
    }

    /// <summary>
    /// 自动检测本机安装的工具
    /// </summary>
    private static List<Tool> AutoDetectLocalTools()
    {
        var found = new List<Tool>();

        // 已知工具的检测规则
        var detectionRules = new (string Name, string Version, string[] Paths, string Exe, string Type)[]
        {
            // Python
            ("Python", "3.x", new[] { @"C:\Python3*", @"C:\Program Files\Python3*" }, 
                "python.exe", "桌面类_开源"),
            // Word (via Office)
            ("Word", "Office", new[] { @"C:\Program Files\Microsoft Office", @"C:\Program Files (x86)\Microsoft Office" }, 
                "WINWORD.EXE", "桌面类_商业"),
            // Excel
            ("Excel", "Office", new[] { @"C:\Program Files\Microsoft Office", @"C:\Program Files (x86)\Microsoft Office" }, 
                "EXCEL.EXE", "桌面类_商业"),
            // MATLAB
            ("MATLAB", "R202*", new[] { @"C:\Program Files\MATLAB" }, 
                "matlab.exe", "桌面类_商业"),
            // NX
            ("NX", "NX*", new[] { @"C:\Program Files\Siemens" }, 
                "ugraf.exe", "桌面类_工业"),
        };

        foreach (var rule in detectionRules)
        {
            foreach (var pathPattern in rule.Paths)
            {
                try
                {
                    var basePath = pathPattern.TrimEnd('*');
                    if (!Directory.Exists(basePath)) continue;

                    // 递归搜索可执行文件
                    var matchingFiles = Directory.GetFiles(basePath, rule.Exe,
                        SearchOption.AllDirectories);

                    if (matchingFiles.Length > 0)
                    {
                        var exePath = matchingFiles[0];
                        var dir = Path.GetDirectoryName(exePath) ?? basePath;

                        found.Add(new Tool
                        {
                            ToolName = rule.Name,
                            ToolVersion = rule.Version,
                            ToolPath = dir,
                            CommandParameter = $"[ToolPath]\\{rule.Exe} [Arguments]",
                            ToolType = rule.Type,
                            IsEnabled = true,
                        });

                        break; // 找到一个即可
                    }
                }
                catch { /* 权限不足或路径无效 */ }
            }
        }

        return found;
    }
}

/// <summary>
/// 简单的工具编辑对话框（内嵌在主窗口中）
/// </summary>
public class ToolEditDialog : Window
{
    public Tool? Tool { get; private set; }

    private readonly TextBox _txtName = new() { Width = 300, Margin = new Thickness(0, 3, 0, 5) };
    private readonly TextBox _txtVersion = new() { Width = 120, Margin = new Thickness(0, 3, 0, 5), Text = "1.0" };
    private readonly TextBox _txtPath = new() { Width = 350, Margin = new Thickness(0, 3, 0, 5) };
    private readonly TextBox _txtCommand = new() { Width = 350, Margin = new Thickness(0, 3, 0, 5), Text = "[ToolPath]" };
    private readonly TextBox _txtType = new() { Width = 200, Margin = new Thickness(0, 3, 0, 5), Text = "桌面类_商业" };
    private readonly CheckBox _chkEnabled = new() { Margin = new Thickness(0, 3, 0, 0), IsChecked = true };

    public ToolEditDialog(Tool? existing = null)
    {
        Title = existing != null ? "编辑工具" : "添加工具";
        Width = 450;
        Height = 370;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        if (existing != null)
        {
            _txtName.Text = existing.ToolName ?? "";
            _txtVersion.Text = existing.ToolVersion ?? "1.0";
            _txtPath.Text = existing.ToolPath ?? "";
            _txtCommand.Text = existing.CommandParameter ?? "[ToolPath]";
            _txtType.Text = existing.ToolType ?? "桌面类_商业";
            _chkEnabled.IsChecked = existing.IsEnabled;
        }

        var browseBtn = new Button { Content = "浏览...", Width = 60, Margin = new Thickness(5, 0, 0, 0) };
        var pathPanel = new DockPanel();
        DockPanel.SetDock(browseBtn, Dock.Right);
        pathPanel.Children.Add(browseBtn);
        pathPanel.Children.Add(_txtPath);

        browseBtn.Click += (s, e) =>
        {
            var path = StationSettingUI.Helpers.FolderBrowserHelper.ShowDialog(this, "选择工具安装目录", _txtPath.Text);
            if (!string.IsNullOrEmpty(path))
            {
                _txtPath.Text = path;
            }
        };

        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 6

        grid.Children.Add(new TextBlock { Text = "工具名称:", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(grid.Children[0], 0); Grid.SetColumn(grid.Children[0], 0);
        Grid.SetRow(_txtName, 0); Grid.SetColumn(_txtName, 1);
        grid.Children.Add(_txtName);

        grid.Children.Add(new TextBlock { Text = "版本:", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(grid.Children[1], 1); Grid.SetColumn(grid.Children[1], 0);
        Grid.SetRow(_txtVersion, 1); Grid.SetColumn(_txtVersion, 1);
        grid.Children.Add(_txtVersion);

        grid.Children.Add(new TextBlock { Text = "安装路径:", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(grid.Children[2], 2); Grid.SetColumn(grid.Children[2], 0);
        Grid.SetRow(pathPanel, 2); Grid.SetColumn(pathPanel, 1);
        grid.Children.Add(pathPanel);

        grid.Children.Add(new TextBlock { Text = "启动命令:", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(grid.Children[3], 3); Grid.SetColumn(grid.Children[3], 0);
        Grid.SetRow(_txtCommand, 3); Grid.SetColumn(_txtCommand, 1);
        grid.Children.Add(_txtCommand);

        grid.Children.Add(new TextBlock { Text = "工具类型:", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(grid.Children[4], 4); Grid.SetColumn(grid.Children[4], 0);
        Grid.SetRow(_txtType, 4); Grid.SetColumn(_txtType, 1);
        grid.Children.Add(_txtType);

        Grid.SetRow(_chkEnabled, 5); Grid.SetColumn(_chkEnabled, 1);
        _chkEnabled.Content = "启用此工具";
        grid.Children.Add(_chkEnabled);

        // 按钮
        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var okBtn = new Button { Content = "确定", Width = 70, Margin = new Thickness(0, 0, 10, 0) };
        var cancelBtn = new Button { Content = "取消", Width = 70 };

        okBtn.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("请输入工具名称", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Tool = new Tool
            {
                Id = existing?.Id,
                ToolName = _txtName.Text.Trim(),
                ToolVersion = _txtVersion.Text.Trim(),
                ToolPath = _txtPath.Text.Trim(),
                CommandParameter = _txtCommand.Text.Trim(),
                ToolType = _txtType.Text.Trim(),
                ToolCompany = existing?.ToolCompany ?? "CJ",
                ToolLocation = existing?.ToolLocation ?? "图站",
                IsEnabled = _chkEnabled.IsChecked == true,
            };

            DialogResult = true;
            Close();
        };

        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };

        buttonsPanel.Children.Add(okBtn);
        buttonsPanel.Children.Add(cancelBtn);

        Grid.SetRow(buttonsPanel, 6); Grid.SetColumnSpan(buttonsPanel, 2);
        grid.Children.Add(buttonsPanel);

        Content = grid;
    }
}
