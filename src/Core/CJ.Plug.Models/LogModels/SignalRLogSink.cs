using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json;
using System.Threading.Channels;


/// <summary>
/// 把 Serilog 日志转发到主服务 <c>/mainHub</c> 的 Sink。
///
/// 2026-10-29 重构（缺陷修复，见文档 K.7）—— 原实现有三个叠加问题，导致日志刷屏：
///   1. 构造函数里裸调 <c>_hubConnection.StartAsync()</c>（不 await、不接异常）⇒ 启动期 Hub 还没起来时
///      连接失败无人观察，状态停在 <c>Connecting</c>。
///   2. <c>Emit</c> 是 <c>async void</c>，且在日志路径上 <c>await StartAsync()</c>；守卫写的是
///      <c>State != Connected</c> —— <c>Connecting</c> 也满足 ⇒ 重复 StartAsync ⇒
///      抛 <c>InvalidOperationException: The HubConnection cannot be started if it is not in the Disconnected state</c>。
///   3. 异常被 catch 后 <c>Console.WriteLine(ex)</c> 全量打印 ⇒ 每条日志都刷一段堆栈。
///      这就是用户长期反馈的「异常重复刷屏」的真正源头。
///
/// 现在的结构：
///   · <see cref="Emit"/> 只做「投递到有界队列」，绝不阻塞、绝不抛、绝不等网络。
///   · 单消费者后台泵串行发送；连接管理收敛到 <see cref="EnsureConnectedAsync"/>，
///     只有 <c>Disconnected</c> 才允许 Start，失败按指数退避，提示限流打印。
/// </summary>
public class SignalRLogSink : ILogEventSink, IDisposable
{
    /// <summary>队列容量。日志是"尽力送达"通道，积压时丢最旧的、保最新的。</summary>
    private const int QueueCapacity = 2048;

    /// <summary>连接失败退避上限。</summary>
    private static readonly TimeSpan MaxConnectBackoff = TimeSpan.FromSeconds(30);

    /// <summary>同一条告警的最小打印间隔，防止退避期间刷屏。</summary>
    private const long WarnThrottleMs = 5000;

    private readonly HubConnection _hubConnection;
    private readonly string _loggerName;
    private readonly Channel<LogEvent> _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly Task _pump;

    private long _lastWarnTicks;
    private long _connectBlockedUntilTicks;
    private int _consecutiveConnectFailures;
    private long _connectFailureTotal;
    private int _disposed;

    public SignalRLogSink(string loggerName)
    {
        _loggerName = loggerName;

        _queue = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        // 幂等；确保 SignalR 内部 new 出来的 HttpClient 也吃「环回不走代理」。
        LoopbackProxyBypass.Apply();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{GlobalData.MainDispatcherServer}/mainHub")
            // 托管组件侧也要自愈：原先只有手写重试，没有自动重连，
            // 断线后整条日志通道静默失效（现象是"日志突然没了"）。
            .WithAutomaticReconnect()
            .Build();

        _pump = Task.Run(PumpAsync);
    }

    /// <summary>
    /// Serilog 同步调用入口。这里**只允许**做内存入队：
    /// 任何阻塞/异常都会被 Serilog 的同步管道放大成整体性能问题。
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        if (Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        // DropOldest 模式下 TryWrite 恒为 true（满了会挤掉最旧的），这正是我们要的语义：
        // 日志宁可丢旧的，也不能拖住业务线程。
        _queue.Writer.TryWrite(logEvent);
    }

    private async Task PumpAsync()
    {
        var ct = _cts.Token;

        try
        {
            await foreach (var logEvent in _queue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    if (!await EnsureConnectedAsync(ct))
                    {
                        // 连不上：本轮丢弃。不排队、不重试单条，避免"积压 → 更慢 → 更积压"。
                        continue;
                    }

                    await SendAsync(logEvent, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    WarnThrottled($"发送日志失败：{ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出路径（IDisposable）
        }
        catch (Exception ex)
        {
            WarnThrottled($"日志泵异常退出：{ex.Message}");
        }
    }

    /// <summary>
    /// 连接管理唯一入口。返回 <c>false</c> 表示"本轮不可发送"。
    /// </summary>
    private async Task<bool> EnsureConnectedAsync(CancellationToken ct)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            return true;
        }

        // 退避期内直接跳过，避免每条日志都去撞一次连接。
        if (Environment.TickCount64 < Interlocked.Read(ref _connectBlockedUntilTicks))
        {
            return false;
        }

        // ⚠ 关键守卫：只有 Disconnected 才能 StartAsync。
        //   Connecting / Reconnecting 时调用必抛 InvalidOperationException —— 原实现缺的正是这一条。
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            return false;
        }

        await _connectGate.WaitAsync(ct);
        try
        {
            // 双重检查：等锁期间可能已被连上。
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                return true;
            }

            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                return false;
            }

            await _hubConnection.StartAsync(ct);

            if (Interlocked.Exchange(ref _consecutiveConnectFailures, 0) > 0)
            {
                Console.WriteLine($"[LOG-SINK:{_loggerName}] 已重新连上 {GlobalData.MainDispatcherServer}/mainHub" +
                                  $"（此前累计失败 {Interlocked.Read(ref _connectFailureTotal)} 次）");
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveConnectFailures);
            Interlocked.Increment(ref _connectFailureTotal);
            Interlocked.Exchange(ref _connectBlockedUntilTicks, Environment.TickCount64 + (long)ComputeBackoff(failures).TotalMilliseconds);
            WarnThrottled($"连接 {GlobalData.MainDispatcherServer}/mainHub 失败（连续第 {failures} 次，退避 {ComputeBackoff(failures).TotalSeconds:0}s）：{ex.Message}");
            return false;
        }
        finally
        {
            _connectGate.Release();
        }
    }

    private static TimeSpan ComputeBackoff(int failures)
    {
        var seconds = Math.Pow(2, Math.Min(Math.Max(failures, 1) - 1, 5)); // 1,2,4,8,16,32
        var span = TimeSpan.FromSeconds(seconds);
        return span > MaxConnectBackoff ? MaxConnectBackoff : span;
    }

    private async Task SendAsync(LogEvent logEvent, CancellationToken ct)
    {
        var renderedMessage = logEvent.RenderMessage();
        string? receiverId = null;
        string? logType = null;

        if (logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue))
        {
            receiverId = ExtractValue(receiverValue);
        }

        if (logEvent.Properties.TryGetValue(LogContextEnum.LogType.ToString(), out var typeValue))
        {
            logType = ExtractValue(typeValue);
        }

        if (string.IsNullOrEmpty(logType))
        {
            logType = LogTypeEnum.CommonLog.ToString();
        }

        var logModel = new LogModel
        {
            Date = logEvent.Timestamp.DateTime.ToString("HH:mm:ss.fff"),
            Type = logEvent.Level.ToString(),
            Author = _loggerName,
            Description = renderedMessage,
        };

        switch (logType)
        {
            case nameof(LogTypeEnum.ActivityStatusNow):
                await SendActivityStatusNow(logEvent, ct);
                break;

            case nameof(LogTypeEnum.CompleteActivityContext):
                await _hubConnection.InvokeAsync(LogTypeEnum.CompleteActivityContext.ToString(), renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.PDZUpdatedInfo):
                await _hubConnection.InvokeAsync(LogTypeEnum.PDZUpdatedInfo.ToString(), renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.PlugUpdated):
                await _hubConnection.InvokeAsync(LogTypeEnum.PlugUpdated.ToString(), renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.JobStatusUpdated):
                await _hubConnection.InvokeAsync(LogTypeEnum.JobStatusUpdated.ToString(), renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.StationExecuting):
                await SendStationExecuting(logEvent, renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.VncWindowClosed):
                await SendVncWindowClosed(renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.VncPidReady):
                await SendVncPidReady(renderedMessage, ct);
                break;

            case nameof(LogTypeEnum.CommonLog):
            default:
                await _hubConnection.InvokeAsync(
                    LogTypeEnum.CommonLog.ToString(), receiverId, JsonSerializer.Serialize(logModel), ct);
                break;
        }
    }

    /// <summary>
    /// 同一条告警 5 秒内只打一次。原实现每次异常都 <c>Console.WriteLine(ex.ToString())</c>，
    /// 在异常高频时自身就是刷屏源。
    /// </summary>
    private void WarnThrottled(string message)
    {
        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _lastWarnTicks);
        if (now - last < WarnThrottleMs)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastWarnTicks, now, last) != last)
        {
            return;
        }

        Console.WriteLine($"[LOG-SINK:{_loggerName}] {message}");
    }

    // 提取参数值（处理基本类型）
    private string? ExtractValue(LogEventPropertyValue? value)
    {
        if (value is ScalarValue scalar && scalar.Value != null)
        {
            return scalar.Value.ToString();
        }
        return string.Empty;
    }

    private async Task SendActivityStatusNow(LogEvent logEvent, CancellationToken ct)
    {
        logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue);
        var pdzId = ExtractValue(receiverValue);
        logEvent.Properties.TryGetValue(LogContextEnum.PlugDefinitionId.ToString(), out var plugDefinitionIdValue);
        var plugDefinitionId = ExtractValue(plugDefinitionIdValue);

        var status = logEvent.RenderMessage();
        await _hubConnection.InvokeAsync(LogTypeEnum.ActivityStatusNow.ToString(), pdzId, plugDefinitionId, status, ct);
    }

    private async Task SendStationExecuting(LogEvent logEvent, string rendered, CancellationToken ct)
    {
        logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue);
        var pdzId = ExtractValue(receiverValue);
        logEvent.Properties.TryGetValue(LogContextEnum.PlugDefinitionId.ToString(), out var plugDefValue);
        var plugDefinitionId = ExtractValue(plugDefValue);

        string stationIp;
        var protocol = "vnc";
        string? processName = null;
        try
        {
            var info = JsonSerializer.Deserialize<StationExecutingData>(rendered);
            stationIp = info?.StationIp ?? string.Empty;
            protocol = info?.Protocol ?? "vnc";
            processName = info?.ProcessName;
        }
        catch
        {
            stationIp = rendered;
        }

        await _hubConnection.InvokeAsync(
            LogTypeEnum.StationExecuting.ToString(), pdzId, plugDefinitionId, stationIp, protocol, processName, ct);
    }

    private async Task SendVncWindowClosed(string rendered, CancellationToken ct)
    {
        string stationIp;
        string? sessionKey = null;
        try
        {
            var info = JsonSerializer.Deserialize<VncWindowClosedData>(rendered);
            stationIp = info?.StationIp ?? string.Empty;
            sessionKey = info?.SessionKey;
        }
        catch
        {
            stationIp = rendered;
        }

        await _hubConnection.InvokeAsync(LogTypeEnum.VncWindowClosed.ToString(), stationIp, sessionKey, ct);
    }

    private async Task SendVncPidReady(string rendered, CancellationToken ct)
    {
        string stationIp;
        int? pid = null;
        string? processName = null;
        string? sessionKey = null;
        try
        {
            var info = JsonSerializer.Deserialize<VncPidReadyData>(rendered);
            stationIp = info?.StationIp ?? string.Empty;
            pid = info?.Pid;
            processName = info?.ProcessName;
            sessionKey = info?.SessionKey;
        }
        catch
        {
            stationIp = rendered;
        }

        await _hubConnection.InvokeAsync(
            LogTypeEnum.VncPidReady.ToString(), stationIp, pid, processName, sessionKey, ct);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            _queue.Writer.TryComplete();
            _cts.Cancel();
            _pump.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // 退出路径不因清理失败而抛。
        }
        finally
        {
            _cts.Dispose();
            _connectGate.Dispose();
        }
    }

    private class VncPidReadyData
    {
        public string? StationIp { get; set; }
        public int? Pid { get; set; }
        public string? ProcessName { get; set; }
        public string? SessionKey { get; set; }
    }

    private class VncWindowClosedData
    {
        public string? StationIp { get; set; }
        public string? SessionKey { get; set; }
    }

    private class StationExecutingData
    {
        public string? PlugDefinitionId { get; set; }
        public string? StationIp { get; set; }
        public string? Protocol { get; set; }
        public string? ProcessName { get; set; }
    }
}
