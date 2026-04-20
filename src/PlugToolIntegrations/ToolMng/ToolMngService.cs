using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using ToolMng.Models;

namespace ToolMng
{
    public class ToolMngService:BackgroundService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private HubConnection _hubConnection;
        private readonly string _webApiBaseUrl = StaticData.MainServerHostIp;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectToHubForever();
        }

        private async Task ConnectToHubForever()
        {
            bool isConnected=false;
            //Console.WriteLine("主服务器地址："+_webApiBaseUrl);
            //Console.WriteLine("主服务器地址：" + StaticData.MainServerHostIp);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_webApiBaseUrl}/toolAgentStatus")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += async (error) =>
            {
                Console.WriteLine("HUB连接中断");
                isConnected = false;
                while (!isConnected)
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    Console.WriteLine("正在尝试重连");
                    try
                    {
                        await _hubConnection.StartAsync(); // 尝试重新连接
                        Console.WriteLine("重连主机成功");
                        isConnected = true;
                    }
                    catch (Exception ex) 
                    { 
                        //Console.WriteLine(ex.ToString());
                        Console.WriteLine("......尝试失败，继续尝试......");
                    } 
                }                
            };
               
            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Console.WriteLine($"Received status: - {user}, Running - {message}");
            });

            _hubConnection.On("TellMeStatus", async () =>
            {
                //Console.WriteLine($"收到获取状态请求");                
                try
                {
                    Console.WriteLine($"收到主机状态请求，并发送适配器状态");
                    // 获取本地主机名
                    string hostName = Dns.GetHostName();
                    
                    // 获取主机名对应的IP地址列表
                    IPAddress[] addresses = Dns.GetHostEntry(hostName).AddressList;

                    // 遍历IP地址列表，找到第一个IPv4地址
                    IPAddress ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    await _hubConnection.InvokeAsync("ProcessStatusInfo", StaticData.ToolAgentServerHttpScheme+ "://" + ipv4.ToString()+":"+StaticData.ToolAgentServerHttpPort, "运行中");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }
            });

            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("----------【成功连接到主服务器】---------");
                isConnected = true;
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine("重试连接中...");
                await ConnectToHubForever();
            }
            finally 
            {
                //await ConnectToHubForever();
            } 
        }

    }
}
