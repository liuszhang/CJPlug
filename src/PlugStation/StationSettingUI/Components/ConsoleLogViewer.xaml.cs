using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// StationApiServer 控制台日志查看器 — 实时展示子进程的 stdout/stderr
/// </summary>
public partial class ConsoleLogViewer : UserControl
{
    private readonly ConsoleLogService _logService;

    public ConsoleLogViewer()
    {
        InitializeComponent();
        _logService = ((App)Application.Current).LogService;

        // 绑定数据源
        LogListBox.ItemsSource = _logService.LogEntries;

        // 监听新增日志 → 自动滚动
        _logService.LogEntries.CollectionChanged += OnLogEntriesChanged;

        // 更新计数
        _logService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConsoleLogService.TotalCount))
                TxtLogCount.Text = $"共 {_logService.TotalCount} 条";
        };

        // 初始状态
        TxtStatus.Text = _logService.LogEntries.Count > 0
            ? $"已记录 {_logService.LogEntries.Count} 条日志"
            : "等待服务启动...";
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && ChkAutoScroll.IsChecked == true)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LogListBox.Items.Count > 0)
                {
                    var lastItem = LogListBox.Items[^1];
                    LogListBox.ScrollIntoView(lastItem);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        TxtStatus.Text = $"已记录 {_logService.LogEntries.Count} 条日志";
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _logService.Clear();
        TxtStatus.Text = "日志已清空";
    }
}
