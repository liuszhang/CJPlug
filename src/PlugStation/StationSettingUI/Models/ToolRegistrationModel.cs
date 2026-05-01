using CJ.Plug.Models.Station;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StationSettingUI.Models;

/// <summary>
/// 工具注册视图模型，包装 Tool 实体以支持 UI 绑定
/// </summary>
public class ToolRegistrationModel : INotifyPropertyChanged
{
    private Tool _tool;

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

    public bool SetupStatus
    {
        get => _tool.IsEnabled && !string.IsNullOrWhiteSpace(_tool.ToolPath) 
            && Directory.Exists(_tool.ToolPath);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
