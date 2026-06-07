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

    public HubConnectionManagerService()
    {
        var BaseAddress =new Uri(GlobalData.MainDispatcherServer);
        //_httpHostIp = httpHostIp;
        Console.WriteLine($"--------------HUB connect server ip:{BaseAddress}---------------");
        //var baseUrl = BaseAddress?.ToString();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{BaseAddress}mainHub")
            .Build();

        _hubConnection.Closed += async (error) =>
        {
            Console.WriteLine("HUB CONNECT SHUT DOWN");
            int isConnected = 0;
            while (isConnected < 3)
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                Console.WriteLine($"TRY TO RECONNECT{isConnected}");
                try
                {
                    await _hubConnection.StartAsync(); // 尝试重新连接
                    Console.WriteLine("RECONNECTED");
                    isConnected = 5;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"......RECONNECTED FAIL,CONTINUE TRYING{isConnected}......");
                }
                isConnected++;
            }
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

        try
        {
            _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"====================failed connected to hub server:{BaseAddress}====================");
            Console.WriteLine(ex);
        }
        Console.WriteLine($"====================success connected to hub server:{BaseAddress}====================");
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
            }
            else
            {
                //Console.WriteLine(3);
            }

        }
        catch (Exception ex)
        {
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

