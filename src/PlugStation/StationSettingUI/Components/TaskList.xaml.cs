using System.Windows;
using System.Windows.Controls;
using StationSettingUI.Models;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// 任务列表组件 - 显示图站当前及历史任务
/// </summary>
public partial class TaskList : UserControl
{
    private readonly StationSettingUI.Services.StationConfigService _configService;
    private readonly StationApiService _apiService;
    private AppConfig _config;
    private bool _isInitialized;

    /// <summary>全量任务数据（服务端原始数据）</summary>
    private List<StationTaskInfo> _allTasks = new();

    public TaskList()
    {
        InitializeComponent();
        _configService = new StationSettingUI.Services.StationConfigService();
        _config = _configService.LoadConfig();
        _apiService = new StationApiService(_config);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        await RefreshAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int taskId)
        {
            btn.IsEnabled = false;
            btn.Content = "...";
            var ok = await _apiService.StopTaskAsync(taskId);
            if (ok)
            {
                TxtStatus.Text = $"任务 #{taskId} 已停止";
            }
            else
            {
                TxtStatus.Text = $"停止任务 #{taskId} 失败";
                btn.IsEnabled = true;
                btn.Content = "停止";
            }
            await RefreshAsync();
        }
    }

    private async void BtnClearDone_Click(object sender, RoutedEventArgs e)
    {
        // 仅 UI 层面清除已完成/失败项（数据库保留，下次刷新会重新加载）
        var toRemove = new List<StationTaskInfo>();
        foreach (StationTaskInfo item in GridTasks.Items)
        {
            if (item.Status is "completed" or "failed")
                toRemove.Add(item);
        }

        foreach (var item in toRemove)
            GridTasks.Items.Remove(item);

        UpdateSummary();
        TxtStatus.Text = $"已隐藏 {toRemove.Count} 条已完成记录";
    }

    private async Task RefreshAsync()
    {
        TxtStatus.Text = "正在加载...";

        var tasks = await _apiService.GetTasksAsync();
        _allTasks = tasks ?? new List<StationTaskInfo>();

        ApplyFilter();
        TxtStatus.Text = tasks != null ? "加载完成" : "服务无响应";
    }

    /// <summary>
    /// 根据当前筛选条件过滤并刷新 DataGrid
    /// </summary>
    private void ApplyFilter()
    {
        // XAML 解析期间 CmbStatusFilter.IsSelected 会提前触发 Filter_Changed，
        // 此时后续控件尚未创建，需判空保护
        if (CmbStatusFilter == null || TxtToolFilter == null ||
            TxtTypeFilter == null || TxtKeywordFilter == null || GridTasks == null)
            return;

        var filtered = _allTasks.AsEnumerable();

        // 状态筛选
        var statusTag = (CmbStatusFilter.SelectedItem as ComboBoxItem)?.Tag as string;
        if (!string.IsNullOrEmpty(statusTag) && statusTag != "all")
            filtered = filtered.Where(t => t.Status == statusTag);

        // 工具名筛选（模糊匹配）
        var toolFilter = TxtToolFilter.Text?.Trim();
        if (!string.IsNullOrEmpty(toolFilter))
            filtered = filtered.Where(t =>
                t.ToolName?.Contains(toolFilter, StringComparison.OrdinalIgnoreCase) == true);

        // 类型筛选（模糊匹配）
        var typeFilter = TxtTypeFilter.Text?.Trim();
        if (!string.IsNullOrEmpty(typeFilter))
            filtered = filtered.Where(t =>
                t.PlugTypeKey?.Contains(typeFilter, StringComparison.OrdinalIgnoreCase) == true);

        // 关键词筛选（搜索命令、关联ID、子状态）
        var keyword = TxtKeywordFilter.Text?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(t =>
                (t.Command?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.CorrelationId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.SubStatus?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true));
        }

        GridTasks.Items.Clear();
        foreach (var t in filtered)
            GridTasks.Items.Add(t);

        UpdateSummary();
    }

    /// <summary>
    /// 筛选条件变更时重新过滤
    /// </summary>
    private void Filter_Changed(object sender, EventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// 清除所有筛选条件
    /// </summary>
    private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
    {
        CmbStatusFilter.SelectedIndex = 0;
        TxtToolFilter.Text = "";
        TxtTypeFilter.Text = "";
        TxtKeywordFilter.Text = "";
    }

    private void UpdateSummary()
    {
        var count = GridTasks.Items.Count;
        var total = _allTasks.Count;
        var running = 0;
        var completed = 0;
        var failed = 0;

        foreach (StationTaskInfo item in GridTasks.Items)
        {
            switch (item.Status)
            {
                case "running": running++; break;
                case "completed": completed++; break;
                case "failed": failed++; break;
            }
        }

        if (count == total)
            TxtSummary.Text = $"共 {count} 个任务 | 执行中: {running} | 已完成: {completed} | 失败: {failed}";
        else
            TxtSummary.Text = $"显示 {count}/{total} 个任务 | 执行中: {running} | 已完成: {completed} | 失败: {failed}";
    }
}
