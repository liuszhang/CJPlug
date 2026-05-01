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
    private readonly StationConfigService _configService;
    private readonly StationApiService _apiService;
    private AppConfig _config;
    private bool _isInitialized;

    public TaskList()
    {
        InitializeComponent();
        _configService = new StationConfigService();
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
        GridTasks.Items.Clear();

        if (tasks != null)
        {
            foreach (var t in tasks)
                GridTasks.Items.Add(t);
        }

        UpdateSummary();
        TxtStatus.Text = tasks != null ? "加载完成" : "服务无响应";
    }

    private void UpdateSummary()
    {
        var count = GridTasks.Items.Count;
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

        TxtSummary.Text = $"共 {count} 个任务 | 执行中: {running} | 已完成: {completed} | 失败: {failed}";
    }
}
