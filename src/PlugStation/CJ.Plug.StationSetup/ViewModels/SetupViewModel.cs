using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CJ.Plug.StationSetup.ViewModels;

public class SetupViewModel : INotifyPropertyChanged
{
    private string _installPath = @"C:\CJStation";
    private string _mainServerUrl = "http://192.168.1.100:8686";
    private string _statusText = "";
    private double _progressValue;
    private bool _isInstalling;
    private bool _installCompleted;
    private bool _installSucceeded;
    private bool _createDesktopShortcut = true;
    private bool _autoStartService = true;

    public string InstallPath
    {
        get => _installPath;
        set { _installPath = value; OnPropertyChanged(); }
    }

    public string MainServerUrl
    {
        get => _mainServerUrl;
        set { _mainServerUrl = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        set { _isInstalling = value; OnPropertyChanged(); }
    }

    public bool InstallCompleted
    {
        get => _installCompleted;
        set { _installCompleted = value; OnPropertyChanged(); }
    }

    public bool InstallSucceeded
    {
        get => _installSucceeded;
        set { _installSucceeded = value; OnPropertyChanged(); }
    }

    public bool CreateDesktopShortcut
    {
        get => _createDesktopShortcut;
        set { _createDesktopShortcut = value; OnPropertyChanged(); }
    }

    public bool AutoStartService
    {
        get => _autoStartService;
        set { _autoStartService = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
