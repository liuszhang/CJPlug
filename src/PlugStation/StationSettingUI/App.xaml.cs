using System.Windows;
using System.Windows.Threading;

namespace StationSettingUI;

/// <summary>
/// App 入口，全局异常处理
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 全局未处理异常捕获
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // 记录异常
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CJStation", "Logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
        }
        catch { /* 日志记录失败不影响主流程 */ }

        // 显示错误
        MessageBox.Show(
            $"发生未处理的错误:\n{e.Exception.Message}\n\n详细信息已记录到日志文件。",
            "应用程序错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 确保只运行一个实例
        var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);
        if (runningProcesses.Length > 1)
        {
            MessageBox.Show("图站管理工具已在运行中。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
    }
}
