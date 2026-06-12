using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace StationSettingUI.Services;

/// <summary>
/// 共享的控制台日志服务 — 收集 StationApiServer 的 stdout/stderr，
/// 供 ConsoleLogViewer 页签实时展示。
/// </summary>
public class ConsoleLogService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const int MaxLogEntries = 500;

    public ObservableCollection<ConsoleLogEntry> LogEntries { get; } = new();

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set { _totalCount = value; OnPropertyChanged(nameof(TotalCount)); }
    }

    public void AppendLine(string text, bool isError = false, string source = "Station")
    {
        var entry = new ConsoleLogEntry
        {
            Timestamp = DateTime.Now,
            Text = text,
            IsError = isError,
            Source = source,
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(entry);
            TotalCount++;

            // 超出上限时移除旧条目
            while (LogEntries.Count > MaxLogEntries)
                LogEntries.RemoveAt(0);
        });
    }

    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Clear();
            TotalCount = 0;
        });
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ConsoleLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Text { get; set; } = "";
    public bool IsError { get; set; }
    public string Source { get; set; } = "Station";

    /// <summary>
    /// 绑定时使用的显示颜色（WPF binding 需要实例属性）
    /// </summary>
    public Brush DisplayColor => IsError
        ? new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47))  // 红色
        : new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)); // 浅灰白
}
