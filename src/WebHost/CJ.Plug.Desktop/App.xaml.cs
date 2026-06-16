using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CJ.Plug.Desktop;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;
    private MainWindow? _mainWindow;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    public App()
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog((context, config) =>
            {
                config
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Path.Combine(logDir, "app-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddSingleton<ViewModels.ServiceControlViewModel>();
                services.AddSingleton<ViewModels.DatabaseConfigViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<Services.AppHostLauncher>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _host.StartAsync().GetAwaiter().GetResult();

        CreateNotifyIcon();

        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Show();

        // 窗口显示后再异步启动 AppHost，避免阻塞 UI
        var launcher = _host.Services.GetRequiredService<Services.AppHostLauncher>();
        var serviceControlVm = _host.Services.GetRequiredService<ViewModels.ServiceControlViewModel>();
        _ = StartAppHostInBackground(launcher, serviceControlVm);

        base.OnStartup(e);
    }

    private async Task StartAppHostInBackground(
        Services.AppHostLauncher launcher,
        ViewModels.ServiceControlViewModel serviceControlVm)
    {
        try
        {
            await launcher.StartAsync();
            if (!launcher.OwnsProcess && launcher.IsRunning)
                Log.Information("AppHost 检测到外部已有实例，直接复用");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppHost 启动失败（找不到 DLL 或进程异常退出）");
        }
        finally
        {
            Dispatcher.Invoke(() => serviceControlVm.RefreshStatus());
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();

        var launcher = _host.Services.GetRequiredService<Services.AppHostLauncher>();
        try
        {
            if (launcher.IsRunning)
                await launcher.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppHost 关闭时出错");
        }

        Log.CloseAndFlush();
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private void CreateNotifyIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "CJ.Plug 桌面启动器",
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => _mainWindow?.RestoreFromTray();

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(new System.Windows.Forms.ToolStripMenuItem(
            "显示主窗口", null, (_, _) => _mainWindow?.RestoreFromTray()));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(new System.Windows.Forms.ToolStripMenuItem(
            "退出", null, (_, _) => ExitApplication()));

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ExitApplication()
    {
        _notifyIcon!.Visible = false;
        _mainWindow?.ForceExit();
    }
}
