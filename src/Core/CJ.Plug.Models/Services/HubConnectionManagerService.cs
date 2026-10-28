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

    // 首次连接的重试节奏（0s、2s、5s、10s；第 0 次立即尝试）
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)
    };

    public HubConnectionManagerService()
    {
        _baseAddress = new Uri(GlobalData.MainDispatcherServer);
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

            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine($"====================success connected to hub server:{_baseAddress}====================");
                return;
            }
            catch (Exception ex)
            {
                LogHub($"第 {attempt + 1}/{RetryDelays.Length} 次连接失败：{ex.Message}");
                if (attempt == RetryDelays.Length - 1)
                {
                    Console.WriteLine($"====================failed connected to hub server:{_baseAddress}====================");
                    Console.WriteLine(ex);
                }
            }
        }
    }

    public async Task ConnectAsync()
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                //Console.WriteLine(1);
                await _hubConnection.StartAsync();
                //Console.WriteLine(2);
                Console.WriteLine($"====================success connected to hub server:{_baseAddress}====================");
            }
            else if (_hubConnection.State == HubConnectionState.Reconnecting)
            {
                // 自动重连进行中时再调 StartAsync 会抛 InvalidOperationException，这里直接跳过
                LogHub("当前处于自动重连中，跳过重复 StartAsync");
            }
        }
        catch (Exception ex)
        {
            // 保持原语义：不向外抛（DispatchServer 的 StationService 依赖它不抛异常）
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
        await _hubConnection.StopAsync();
    }
}

