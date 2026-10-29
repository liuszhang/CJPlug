using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

public class HubConnectionManagerService: IHubConnectionManager
{
    public List<string> StationList = new List<string>();

    public HubConnection _hubConnection;
    //private readonly GlobalData _httpHostIp;

    private readonly Uri _baseAddress;

    /// <summary>串行化 Start/Stop，防止「构造期重试」与「外部 ConnectAsync」并发撞状态机。</summary>
    private readonly SemaphoreSlim _connectGate = new(1, 1);

    // 首次连接的重试节奏（0s、2s、5s、10s；第 0 次立即尝试）
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)
    };

    public HubConnectionManagerService()
    {
        _baseAddress = new Uri(GlobalData.MainDispatcherServer);
        // 幂等；确保 SignalR 内部 HttpClient 连本机环回时不走系统代理（2026-10-29，文档 K.7）。
        LoopbackProxyBypass.Apply();
        //_httpHostIp = httpHostIp;
        Console.WriteLine($"--------------HUB connect server ip:{_baseAddress}---------------");
        //var baseUrl = BaseAddress?.ToString();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseAddress}mainHub")
            // 断线重连交给 SignalR 自己：原先在 Closed 里手写重连循环是 fire-and-forget，
            // 且 isConnected 计数器在成功后还会再加一次（2026-10-07 修正）
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnecting += error =>
        {
            LogHub($"连接断开，正在自动重连……（{Describe(error)}）");
            return Task.CompletedTask;
        };
        _hubConnection.Reconnected += connectionId =>
        {
            LogHub($"已重连成功（connectionId={connectionId}）");
            return Task.CompletedTask;
        };
        _hubConnection.Closed += error =>
        {
            LogHub($"连接已关闭，不会再自动重连（{Describe(error)}）");
            return Task.CompletedTask;
        };

        _hubConnection.On<string, string, string>("StatusInfo", (ip, status, toolsRootPath) =>
        {
            //var TextValue = $"[客户端收到消息]图站地址：{ip}，状态：{status}";
            StationList.Add(ip);
            //Console.WriteLine(TextValue);
            var HashTextValues = new HashSet<string>(StationList);
            StationList = HashTextValues.ToList();
            //_httpHostIp.ToolAgentHostIps = HashTextValues.ToList();
            //HttpHostIp.ToolAgentHostIps.Add("http://" + ip);
        });

        // ⚠ 构造函数不能 await，所以这里显式等首次连接的真实结果、再按结果打印日志。
        //   原实现 StartAsync() 不 await（外层 try/catch 形同虚设），随后无条件打印
        //   "success connected to hub server" —— hub 根本没起也照打，是假日志；
        //   2026-10-07 排查服务起不来时被它误导过（三次测试全绿实际全断）。
        _ = ConnectWithRetryAsync();
    }

    private static void LogHub(string message) => Console.WriteLine($"[HUB] {message}");

    private static string Describe(Exception? error) => error?.Message ?? "无异常信息";

    /// <summary>
    /// 串行化 <c>StartAsync</c>。
    ///
    /// 为什么必须有这把锁（2026-10-29）：构造函数里 <see cref="ConnectWithRetryAsync"/> 是 fire-and-forget，
    /// 外部业务随时可能再调 <see cref="ConnectAsync"/>。两个入口并发时第二个必然撞上
    /// <c>InvalidOperationException: The HubConnection cannot be started if it is not in the Disconnected state</c>。
    /// 且 <c>Connecting</c> 状态下重复 Start 同样非法 —— 这是原先只判 Reconnecting 漏掉的分支。
    /// </summary>
    private async Task<(bool Started, bool Skipped, Exception? Error)> TryStartAsyncAsync()
    {
        await _connectGate.WaitAsync();
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                return (true, false, null);
            }

            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                // Connecting / Reconnecting：本轮跳过（不是失败，不消耗重试次数）
                return (false, true, null);
            }

            await _hubConnection.StartAsync();
            return (true, false, null);
        }
        catch (Exception ex)
        {
            return (false, false, ex);
        }
        finally
        {
            _connectGate.Release();
        }
    }

    /// <summary>
    /// 首次连接：按 <see cref="RetryDelays"/> 有限重试，并按**真实结果**打印
    /// success / failed（失败时把异常一并打出）。本方法吞掉异常，不向外抛。
    /// </summary>
    private async Task ConnectWithRetryAsync()
    {
        for (var attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(RetryDelays[attempt]);
            }

            var (started, skipped, error) = await TryStartAsyncAsync();

            if (started)
            {
                Console.WriteLine($"====================success connected to hub server:{_baseAddress}====================");
                return;
            }

            if (skipped)
            {
                LogHub("当前已有连接在建立中，跳过本次重复 StartAsync");
                continue;
            }

            LogHub($"第 {attempt + 1}/{RetryDelays.Length} 次连接失败：{Describe(error)}");
            if (attempt == RetryDelays.Length - 1)
            {
                Console.WriteLine($"====================failed connected to hub server:{_baseAddress}====================");
                Console.WriteLine(error);
            }
        }
    }

    public async Task ConnectAsync()
    {
        try
        {
            var (started, skipped, error) = await TryStartAsyncAsync();

            if (started && _hubConnection.State == HubConnectionState.Connected)
            {
                Console.WriteLine($"====================success connected to hub server:{_baseAddress}====================");
            }
            else if (skipped)
            {
                // 自动重连进行中 / 已有连接在建立：直接跳过，不再抛 InvalidOperationException
                LogHub("当前处于连接中或自动重连中，跳过重复 StartAsync");
            }
            else if (error is not null)
            {
                // 保持原语义：不向外抛（DispatchServer 的 StationService 依赖它不抛异常）
                Console.WriteLine($"====================failed connected to hub server:{_baseAddress}====================");
                Console.WriteLine(error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"====================failed connected to hub server:{_baseAddress}====================");
            Console.WriteLine(ex);
        }
    }

    public async Task InvokeAsync<T>(string methodName, T? arguments)
    {
        try
        {
            //Console.WriteLine($"开始执行{methodName}");
            if (arguments == null)
            {
                //Console.WriteLine("无参");
                await _hubConnection.InvokeAsync(methodName);
            }
            else
            {
                //Console.WriteLine("有参");
                //Console.WriteLine((string)arguments.ToString());
                await _hubConnection.InvokeAsync<string>(methodName, arguments);
            }

            //Console.WriteLine($"执行完成{methodName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"执行{methodName}出错：{ex}");
        }

    }

    public async Task DisconnectAsync()
    {
        await _connectGate.WaitAsync();
        try
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
            }
        }
        finally
        {
            _connectGate.Release();
        }
    }
}

