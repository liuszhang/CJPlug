
using CJ.Plug_Aspire.StationApiService.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Sockets;
using System.Net;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;

namespace CJ.Plug_Aspire.StationApiService.Services
{
    public class StationHubService : BackgroundService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private HubConnection? _hubConnection;
        private readonly string _webApiBaseUrl = GlobalData.MainDispatcherServer;
        private volatile bool _isHubConnected;

        // 重试配置
        private const int InitialRetryDelayMs = 2000;
        private const int MaxRetryDelayMs = 30_000;
        private const double BackoffMultiplier = 2.0;

        /// <summary>
        /// 获取与平台主服务器的长连接状态
        /// </summary>
        public bool IsHubConnected => _isHubConnected;

        /// <summary>
        /// 获取主服务器地址
        /// </summary>
        public string MainServerUrl => _webApiBaseUrl;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(">>>>> 开始连接主服务器...");
            await ConnectToHubWithRetry(stoppingToken);
        }

        private async Task ConnectToHubWithRetry(CancellationToken stoppingToken)
        {
            var hostName = Dns.GetHostName();
            IPAddress? ipv4 = null;
            var addresses = Dns.GetHostEntry(hostName).AddressList;
            foreach (var a in addresses)
            {
                if (a.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (a.ToString().StartsWith("192"))
                {
                    ipv4 = a;
                    break;
                }
            }
            ipv4 ??= addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4 == null)
            {
                Console.WriteLine("[StationHub] 无法获取本机 IPv4 地址，使用 localhost");
                ipv4 = IPAddress.Loopback;
            }

            Console.WriteLine($"[StationHub] 本机 IP: {ipv4}:{StaticData.ToolAgentServerHttpPort}");

            var retryDelay = InitialRetryDelayMs;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl($"{_webApiBaseUrl}/mainHub")
                        .WithAutomaticReconnect(new RetryPolicy())
                        .Build();

                    // 连接断开后的重连（AutomaticReconnect 会处理，这里是兜底）
                    _hubConnection.Reconnecting += (error) =>
                    {
                        Console.WriteLine($"[StationHub] 连接断开，正在自动重连... ({error?.Message})");
                        _isHubConnected = false;
                        return Task.CompletedTask;
                    };

                    _hubConnection.Reconnected += async (connectionId) =>
                    {
                        Console.WriteLine($"[StationHub] 已重新连接: {connectionId}");
                        _isHubConnected = true;
                        await RegisterStation(ipv4);
                    };

                    _hubConnection.Closed += async (error) =>
                    {
                        Console.WriteLine($"[StationHub] 连接已关闭: {error?.Message}");
                        _isHubConnected = false;
                    };

                    // 注册 "告知状态" 回调
                    _hubConnection.On("TellMeStatus", async () =>
                    {
                        try
                        {
                            await _hubConnection.InvokeAsync("SendStationStatus",
                                StaticData.ToolAgentServerHttpScheme + "://" + ipv4.ToString() + ":" + StaticData.ToolAgentServerHttpPort,
                                "running");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[StationHub] TellMeStatus 回调异常: {ex.Message}");
                        }
                    });

                    Console.WriteLine($"[StationHub] 尝试连接 {_webApiBaseUrl}/mainHub ...");
                    await _hubConnection.StartAsync(stoppingToken);

                    Console.WriteLine("========== [StationHub] 已成功连接主服务器 ==========");
                    _isHubConnected = true;
                    retryDelay = InitialRetryDelayMs; // 重置延迟

                    await RegisterStation(ipv4);

                    // 保持连接直到取消
                    try
                    {
                        await Task.Delay(Timeout.Infinite, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[StationHub] 连接失败 ({ex.GetType().Name}): {ex.Message}");
                    Console.WriteLine($"[StationHub] {retryDelay / 1000} 秒后重试...");
                }

                try
                {
                    await Task.Delay(retryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // 指数退避，上限 30 秒
                retryDelay = Math.Min(
                    (int)(retryDelay * BackoffMultiplier),
                    MaxRetryDelayMs);
            }

            Console.WriteLine("[StationHub] 连接循环已退出");
        }

        private async Task RegisterStation(IPAddress ipv4)
        {
            try
            {
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    var stationUrl = StaticData.ToolAgentServerHttpScheme + "://" + ipv4.ToString() + ":" + StaticData.ToolAgentServerHttpPort;
                    await _hubConnection.InvokeAsync("SendStationStatus", stationUrl, "running");
                    Console.WriteLine($"[StationHub] 已注册图站: {stationUrl}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StationHub] 注册图站失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// SignalR 自动重连的重试策略（指数退避）
    /// </summary>
    internal class RetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
        };

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.PreviousRetryCount >= RetryDelays.Length)
                return TimeSpan.FromSeconds(30); // 持续每 30 秒重试

            return RetryDelays[retryContext.PreviousRetryCount];
        }
    }
}
