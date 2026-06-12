using System.Windows;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.LogModels;
using System.Text.Json;
using StationSettingUI.Services;

namespace StationSettingUI;

/// <summary>
/// App 入口，全局异常处理
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 全局共享的日志服务（供各组件订阅 StationApiServer 的控制台输出）
    /// </summary>
    public ConsoleLogService LogService { get; } = new();

    /// <summary>
    /// 用于接收 SignalR CommonLog 推送的 Hub 连接
    /// StationApiServer 通过 SignalRLogSink 将日志推送到 MainHub/CommonLog，
    /// StationSettingUI 订阅此事件以实时获取日志，无论服务由谁启动
    /// </summary>
    public HubConnection? LogHubConnection { get; private set; }

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

    protected override async void OnStartup(StartupEventArgs e)
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

        // 连接 MainHub 订阅 CommonLog 事件，实时接收 StationApiServer 推送的日志
        _ = ConnectToLogHubAsync();
    }

    /// <summary>
    /// 连接 MainHub 并订阅 CommonLog 事件
    /// StationApiServer 通过 SignalRLogSink 推送 CommonLog (receiverId, logJson)
    /// logJson 为 LogModel 的 JSON：{"Type":"Information","Description":"xxx","Date":"HH:mm:ss.fff","Author":"Station"}
    /// </summary>
    private async Task ConnectToLogHubAsync()
    {
        try
        {
            LogHubConnection = new HubConnectionBuilder()
                .WithUrl($"{GlobalData.MainDispatcherServer}/mainHub")
                .Build();

            LogHubConnection.On<string, string>("CommonLog", (receiverId, logJson) =>
            {
                try
                {
                    var log = JsonSerializer.Deserialize<LogModel>(logJson);
                    if (log != null)
                    {
                        // 日志来源标识：优先使用 receiverId（SignalR 推送的第一个参数），
                        // receiverId 为空时回退到 LogModel.Author（SignalRLogSink 中的 _loggerName）
                        var source = !string.IsNullOrWhiteSpace(receiverId) ? receiverId : log.Author;
                        if (string.IsNullOrWhiteSpace(source))
                        {
                            LogService.AppendLine($"[{log.Type}] {log.Description}");
                        }
                        else
                        {
                            LogService.AppendLine($"[{log.Type}] {log.Description}", source: source);
                        }
                    }
                }
                catch
                {
                    // JSON 解析失败时静默忽略该条日志
                }
            });

            await LogHubConnection.StartAsync();
        }
        catch
        {
            // Hub 不可用时静默忽略 — 方案 B（读取日志文件）作为兜底
        }
    }
}
