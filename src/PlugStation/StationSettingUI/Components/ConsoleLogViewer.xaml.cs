using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using StationSettingUI.Services;

namespace StationSettingUI.Components;

/// <summary>
/// StationApiServer 控制台日志查看器 — 实时展示子进程的 stdout/stderr
/// </summary>
public partial class ConsoleLogViewer : UserControl
{
    private readonly ConsoleLogService _logService;
    private ICollectionView? _logView;

    public ConsoleLogViewer()
    {
        InitializeComponent();
        _logService = ((App)Application.Current).LogService;

        // 绑定数据源并创建过滤视图（默认仅显示 Station 来源）
        _logView = CollectionViewSource.GetDefaultView(_logService.LogEntries);
        _logView.Filter = FilterLogEntry;
        LogListBox.ItemsSource = _logView;

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
                if (_logView != null && !_logView.IsEmpty)
                {
                    var lastItem = _logView.Cast<object>().Last();
                    LogListBox.ScrollIntoView(lastItem);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        TxtStatus.Text = $"已记录 {_logService.LogEntries.Count} 条日志";
    }

    private bool FilterLogEntry(object item)
    {
        if (item is not ConsoleLogEntry entry) return false;

        var selectedItem = CmbLogSource.SelectedItem as ComboBoxItem;
        var selectedSource = selectedItem?.Content?.ToString() ?? "Station";

        if (selectedSource == "全部") return true;

        return entry.Source == selectedSource;
    }

    private void CmbLogSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _logView?.Refresh();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _logService.Clear();
        TxtStatus.Text = "日志已清空";
    }
}
