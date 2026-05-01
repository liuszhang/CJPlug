
using CJ.Plug_Aspire.StationApiService.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Sockets;
using System.Net;
using CJ.Plug.Models.LogModels;

namespace CJ.Plug_Aspire.StationApiService.Services
{
    public class StationHubService : BackgroundService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private HubConnection _hubConnection;
        private readonly string _webApiBaseUrl = StaticData.MainServerHostIp;
        private volatile bool _isHubConnected;

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
            Console.WriteLine(">>>>>>>>>>>>start to connected to main server.");
            await ConnectToHubForever();
        }

        private async Task ConnectToHubForever()
        {
            // 获取本地主机名
            string hostName = Dns.GetHostName();

            // 获取主机名对应的IP地址列表
            IPAddress? ipv4 = null;
            IPAddress[] addresses = Dns.GetHostEntry(hostName).AddressList;
            foreach (var a in addresses)
            {
                Console.WriteLine(a.ToString());
                if(a.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }
                if (a.ToString().StartsWith("192"))
                {
                    ipv4 = a;
                    break;
                }
            }
            if(ipv4 == null)
            {
                ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            }
            // 遍历IP地址列表，找到第一个IPv4地址
            //IPAddress ipv4 = addresses.Where(a => a.ToString().StartsWith("192")).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            Console.WriteLine($"{ipv4}:{StaticData.ToolAgentServerHttpPort}");


            bool isConnected = false;
            //Console.WriteLine("主服务器地址："+_webApiBaseUrl);
            //Console.WriteLine("主服务器地址：" + StaticData.MainServerHostIp);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_webApiBaseUrl}/mainHub")
                //.WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += async (error) =>
            {
                Console.WriteLine("HUB Disconnected.");
                isConnected = false;
                _isHubConnected = false;
                while (!isConnected)
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    Console.WriteLine("retry connecting...");
                    try
                    {
                        await _hubConnection.StartAsync(); // 尝试重新连接
                        Console.WriteLine("reconnect success.");
                        isConnected = true;
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        Console.WriteLine("......failed to reconnect,retry connecting......");
                    }
                }
            };

            _hubConnection.On("TellMeStatus", async () =>
            {
                //Console.WriteLine($"收到获取状态请求");                
                try
                {                    
                    await _hubConnection.InvokeAsync("SendStationStatus", StaticData.ToolAgentServerHttpScheme + "://" + ipv4.ToString() + ":" + StaticData.ToolAgentServerHttpPort, "running");
                    //await _hubConnection.InvokeAsync("JobStatusInfo", ipv4.ToString() + ":" + StaticData.ToolAgentServerHttpPort, "running");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }
            });
                        
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("----------[success connected to main server]---------");
                isConnected = true;
                _isHubConnected = true;
                await _hubConnection.InvokeAsync("SendStationStatus", StaticData.ToolAgentServerHttpScheme + "://" + ipv4.ToString() + ":" + StaticData.ToolAgentServerHttpPort, "running");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine("try reconnecting...");
                await ConnectToHubForever();
            }
            finally
            {
                //await ConnectToHubForever();
            }
        }

    }
}
