using CJ.Plug.Models.Station;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StationSettingUI.Models;

/// <summary>
/// 工具注册视图模型，包装 Tool 实体以支持 UI 绑定
/// </summary>
public class ToolRegistrationModel : INotifyPropertyChanged
{
    private Tool _tool;
    private ToolActionStatus _actionStatus;

    public ToolRegistrationModel(Tool tool)
    {
        _tool = tool;
    }

    public Tool Tool => _tool;

    public int? Id
    {
        get => _tool.Id;
        set { _tool.Id = value; OnPropertyChanged(); }
    }

    public string? ToolName
    {
        get => _tool.ToolName;
        set { _tool.ToolName = value; OnPropertyChanged(); }
    }

    public string? ToolVersion
    {
        get => _tool.ToolVersion;
        set { _tool.ToolVersion = value; OnPropertyChanged(); }
    }

    public string? ToolPath
    {
        get => _tool.ToolPath;
        set { _tool.ToolPath = value; OnPropertyChanged(); }
    }

    public string? CommandParameter
    {
        get => _tool.CommandParameter;
        set { _tool.CommandParameter = value; OnPropertyChanged(); }
    }

    public string? ToolType
    {
        get => _tool.ToolType;
        set { _tool.ToolType = value; OnPropertyChanged(); }
    }

    public bool IsEnabled
    {
        get => _tool.IsEnabled;
        set { _tool.IsEnabled = value; OnPropertyChanged(); }
    }

    public bool IsSystemInitTool => _tool.IsSystemInitTool;

    public bool SetupStatus
    {
        get => _tool.IsEnabled && !string.IsNullOrWhiteSpace(_tool.ToolPath)
            && (File.Exists(_tool.ToolPath) || Directory.Exists(_tool.ToolPath));
    }

    // ==================== 操作状态 ====================

    public ToolActionStatus ActionStatus
    {
        get => _actionStatus;
        set
        {
            _actionStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActionButtonText));
            OnPropertyChanged(nameof(IsActionButtonEnabled));
            OnPropertyChanged(nameof(VisibilityActionButton));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>操作列按钮文本</summary>
    public string ActionButtonText => ActionStatus switch
    {
        ToolActionStatus.CanDownload => "下载",
        ToolActionStatus.CanRedownload => "重新下载",
        ToolActionStatus.NotFound => "未找到",
        ToolActionStatus.Ready => "已就绪",
        _ => "-"
    };

    /// <summary>操作按钮是否可用</summary>
    public bool IsActionButtonEnabled =>
        ActionStatus == ToolActionStatus.CanDownload
        || ActionStatus == ToolActionStatus.CanRedownload;

    /// <summary>操作按钮是否可见</summary>
    public System.Windows.Visibility VisibilityActionButton =>
        ActionStatus != ToolActionStatus.None
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

    /// <summary>状态文本</summary>
    public string StatusText => ActionStatus switch
    {
        ToolActionStatus.CanDownload => "未安装",
        ToolActionStatus.CanRedownload => "已安装",
        ToolActionStatus.NotFound => "未找到",
        ToolActionStatus.Ready => "已就绪",
        _ => ""
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// 工具操作状态枚举
/// </summary>
public enum ToolActionStatus
{
    /// <summary>未比对</summary>
    None,
    /// <summary>可下载（本地未安装）</summary>
    CanDownload,
    /// <summary>可重新下载（本地已安装）</summary>
    CanRedownload,
    /// <summary>无需下载类型的工具在本地未找到</summary>
    NotFound,
    /// <summary>无需下载类型的工具已就绪</summary>
    Ready
}
