using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using CJ.Plug.Models.Station;
using StationSettingUI.Models;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// 工具注册组件 - 管理图站上的工具安装和配置
/// 从工具安装根目录（ToolsRootPath）扫描子文件夹，每个文件夹视为一个工具。
/// 优先读取各文件夹下的 tool.config.json，其次自动探测可执行文件。
/// </summary>
public partial class ToolRegistration : UserControl
{
    private readonly StationSettingUI.Services.StationConfigService _configService;
    private readonly StationApiService _apiService;
    private AppConfig _config;
    private bool _isInitialized;

    public ObservableCollection<ToolRegistrationModel> Tools { get; set; } = new();

    public ToolRegistration()
    {
        InitializeComponent();
        _configService = new StationSettingUI.Services.StationConfigService();
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

        // 初始化时从服务器获取工具列表
        await Task.Run(async () =>
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var serverTools = await _apiService.FetchToolsFromServerAsync();
                    if (serverTools != null && serverTools.Count > 0)
                    {
                        Tools.Clear();
                        foreach (var tool in serverTools)
                        {
                            var model = new ToolRegistrationModel(tool);
                            await UpdateToolActionStatusAsync(model);
                            Tools.Add(model);
                        }
                        TxtToolCount.Text = $"共 {Tools.Count} 个工具";
                        TxtToolbarStatus.Text = $"已加载 {Tools.Count} 个工具";
                    }
                }
                catch (Exception)
                {
                    // 初始化失败静默处理，用户可手动刷新
                }
            });
        });
    }

    /// <summary>
    /// 从工具安装根目录（ToolsRootPath）加载工具列表。
    /// 每个子文件夹视为一个工具；优先读取文件夹下的 tool.config.json，
    /// 否则自动探测文件夹中的主可执行文件。
    /// </summary>
    private async Task RefreshToolsAsync()
    {
        TxtToolbarStatus.Text = "正在扫描工具安装目录...";

        try
        {
            var tools = await Task.Run(() => ScanToolsFromRootPath(_config.ToolsRootPath));
            Tools.Clear();

            foreach (var tool in tools)
            {
                Tools.Add(new ToolRegistrationModel(tool));
            }

            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
            TxtToolbarStatus.Text = tools.Count > 0
                ? $"扫描完成，发现 {tools.Count} 个工具"
                : $"工具目录 \"{_config.ToolsRootPath}\" 下未发现工具";
        }
        catch (Exception ex)
        {
            TxtToolbarStatus.Text = $"扫描失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 自动搜索 — 与刷新相同，扫描工具安装根目录
    /// </summary>
    private async void BtnAutoSearch_Click(object sender, RoutedEventArgs e)
    {
        BtnAutoSearch.IsEnabled = false;
        TxtToolbarStatus.Text = "正在搜索工具安装目录...";

        try
        {
            var foundTools = await Task.Run(() => ScanToolsFromRootPath(_config.ToolsRootPath));

            int added = 0;
            foreach (var tool in foundTools)
            {
                if (!Tools.Any(t => t.ToolName == tool.ToolName && t.ToolPath == tool.ToolPath))
                {
                    Tools.Add(new ToolRegistrationModel(tool));
                    added++;
                }
            }

            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
            TxtToolbarStatus.Text = added > 0 ? $"发现 {added} 个新工具" : "未发现新工具";
        }
        catch (Exception ex)
        {
            TxtToolbarStatus.Text = $"搜索失败: {ex.Message}";
        }

        BtnAutoSearch.IsEnabled = true;
    }

    /// <summary>
    /// 扫描 ToolsRootPath 下的所有子文件夹，每个文件夹为一个工具
    /// </summary>
    private static List<Tool> ScanToolsFromRootPath(string rootPath)
    {
        var result = new List<Tool>();

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return result;

        foreach (var dir in Directory.GetDirectories(rootPath))
        {
            try
            {
                var tool = BuildToolFromFolder(dir);
                if (tool != null)
                    result.Add(tool);
            }
            catch
            {
                // 跳过无法访问的文件夹
            }
        }

        return result;
    }

    /// <summary>
    /// 从单个工具文件夹构建 Tool 对象。
    /// 优先读取 tool.config.json；否则自动探测文件夹内可执行文件。
    /// </summary>
    private static Tool? BuildToolFromFolder(string folderPath)
    {
        var folderName = Path.GetFileName(folderPath);

        // 1. 尝试读取 tool.config.json
        var configFile = Path.Combine(folderPath, "tool.config.json");
        if (File.Exists(configFile))
        {
            try
            {
                var json = File.ReadAllText(configFile);
                var tool = JsonSerializer.Deserialize<Tool>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (tool != null && !string.IsNullOrWhiteSpace(tool.ToolName))
                {
                    // 确保路径为当前文件夹
                    if (string.IsNullOrWhiteSpace(tool.ToolPath))
                        tool.ToolPath = folderPath;
                    return tool;
                }
            }
            catch
            {
                // JSON 解析失败则降级为自动探测
            }
        }

        // 2. 自动探测：搜索文件夹内的常见可执行文件后缀
        var exeExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1" };
        string? detectedExe = null;

        foreach (var ext in exeExtensions)
        {
            // 优先根目录
            var files = Directory.GetFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                detectedExe = files[0];
                break;
            }
        }

        if (detectedExe == null)
        {
            // 没有可执行文件也创建工具条目（可能是配置型工具/脚本集）
            // 但跳过明显不是工具的文件夹（如只有 .dll / .xml 等）
            var hasContent = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Length > 0;
            if (!hasContent) return null;
        }

        return new Tool
        {
            ToolName = folderName,
            ToolVersion = "1.0",
            ToolPath = folderPath,
            CommandParameter = detectedExe != null
                ? $"\"{detectedExe}\" [Arguments]"
                : "[ToolPath]",
            ToolType = "桌面类_商业",
            IsEnabled = true,
        };
    }

    /// <summary>
    /// 刷新工具列表 - 从服务器获取工具列表并与本地配置比对
    /// </summary>
    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnRefresh.IsEnabled = false;
        TxtToolbarStatus.Text = "正在从服务器获取工具列表...";

        try
        {
            // 先检查本地 StationApiServer 是否运行
            var localOk = await _apiService.TestStationApiAsync();
            if (!localOk)
            {
                TxtToolbarStatus.Text = "本地图站服务未运行，请先启动服务";
                return;
            }

            var serverTools = await _apiService.FetchToolsFromServerAsync();
            if (serverTools == null || serverTools.Count == 0)
            {
                TxtToolbarStatus.Text = "服务器上没有可用的工具";
                return;
            }

            Tools.Clear();

            foreach (var tool in serverTools)
            {
                var model = new ToolRegistrationModel(tool);
                await UpdateToolActionStatusAsync(model);
                Tools.Add(model);
            }

            TxtToolbarStatus.Text = $"从服务器获取了 {serverTools.Count} 个工具";
            TxtToolCount.Text = $"共 {Tools.Count} 个工具";
        }
        catch (Exception ex)
        {
            TxtToolbarStatus.Text = $"获取失败: {ex.Message}";
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
        }
    }

    /// <summary>
    /// 更新工具的操作状态（根据本地安装情况）
    /// </summary>
    private async Task UpdateToolActionStatusAsync(ToolRegistrationModel model)
    {
        if (model.Tool.SkipDownloadToStation)
        {
            // 无需下载至图站的工具，在本地逐层查找文件
            if (string.IsNullOrWhiteSpace(model.ToolPath))
            {
                model.ActionStatus = ToolActionStatus.NotFound;
            }
            else
            {
                // 1. 直接检查原始路径（绝对路径或相对当前目录）
                var exists = File.Exists(model.ToolPath);
                // 2. 相对路径：尝试从 ToolsRootPath 解析
                if (!exists && !Path.IsPathRooted(model.ToolPath))
                    exists = File.Exists(Path.Combine(_config.ToolsRootPath, model.ToolPath));
                // 3. 通过 StationApiServer 检查（作为兜底）
                if (!exists)
                    exists = await _apiService.CheckFileExistsAsync(model.ToolPath);
                model.ActionStatus = exists ? ToolActionStatus.Ready : ToolActionStatus.NotFound;
            }
        }
        else
        {
            // 需要下载的工具，本地安装目录 = ToolsRootPath/ToolName
            var localPath = Path.Combine(_config.ToolsRootPath, model.ToolName ?? "unknown");

            if (Directory.Exists(localPath))
            {
                var hasFiles = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories).Length > 0;
                model.ActionStatus = hasFiles ? ToolActionStatus.CanRedownload : ToolActionStatus.CanDownload;
            }
            else
            {
                model.ActionStatus = ToolActionStatus.CanDownload;
            }
        }
    }

    /// <summary>
    /// 执行工具操作（下载/重新下载）
    /// </summary>
    private async void ExecuteToolAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ToolRegistrationModel model)
        {
            if (model.ActionStatus != ToolActionStatus.CanDownload &&
                model.ActionStatus != ToolActionStatus.CanRedownload)
                return;

            button.IsEnabled = false;
            TxtToolbarStatus.Text = $"正在{(model.ActionStatus == ToolActionStatus.CanRedownload ? "重新" : "")}下载工具 {model.ToolName}...";

            try
            {
                // 确定目标安装目录：图站上统一放在 ToolsRootPath/ToolName 下
                var targetPath = Path.Combine(_config.ToolsRootPath, model.ToolName ?? "unknown");

                // 确保目标目录存在
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                // 调用下载接口（通过StationApiServer代理），传递服务端工具包根目录路径用于打包
                var success = await _apiService.DownloadToolToStationAsync(
                    model.ToolName ?? "",
                    model.ToolVersion ?? "1.0",
                    targetPath,
                    model.Tool.ToolBasePath
                );

                if (success)
                {
                    // 验证文件是否实际下载成功
                    if (Directory.Exists(targetPath) && Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories).Length > 0)
                    {
                        TxtToolbarStatus.Text = $"工具 {model.ToolName} 下载完成";
                        model.ActionStatus = ToolActionStatus.CanRedownload;
                    }
                    else
                    {
                        TxtToolbarStatus.Text = $"工具 {model.ToolName} 下载失败：服务端未找到工具文件";
                        model.ActionStatus = ToolActionStatus.CanDownload;
                    }
                }
                else
                {
                    TxtToolbarStatus.Text = $"工具 {model.ToolName} 下载失败";
                }
            }
            catch (Exception ex)
            {
                TxtToolbarStatus.Text = $"下载异常: {ex.Message}";
            }

            button.IsEnabled = true;
        }
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
    /// 打开工具安装路径文件夹
    /// </summary>
    private void BtnOpenInstallPath_Click(object sender, RoutedEventArgs e)
    {
        var path = _config.ToolsRootPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            TxtToolbarStatus.Text = "未配置工具安装路径";
            return;
        }

        if (!Directory.Exists(path))
        {
            TxtToolbarStatus.Text = $"安装路径不存在: {path}";
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
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

    }
