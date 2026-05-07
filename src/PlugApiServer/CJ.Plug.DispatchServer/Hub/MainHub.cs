
using CJ.Plug.DispatchServer.Contracts;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using Microsoft.AspNetCore.SignalR;
using System.Threading;


//Hub广播消息的方法类
//需要保证方法名称和事件名称一致
public class MainHub(IStationService stationService):Hub
{
    public async Task TellMeStatus()
    {
        //Console.WriteLine("except ToolAgent status");
        await Clients.All.SendAsync("TellMeStatus");
    }

    public async Task SendStationStatus(string ip, string status)
    {
        //将状态新增或更新到持久化存储中
        string encodedIp = Uri.EscapeDataString(ip);
        var host = await stationService.GetApiServer();
        var httpClient = new HttpClient { BaseAddress = new Uri(host) };
        var exist = await httpClient.GetFromJsonAsync<Station?>($"api/Station/GetStationByIp/{encodedIp}");
        if (exist != null)
        {
            exist.UpdateTime = DateTime.Now.ToString();
            //exist.IsStarted = true;
            await httpClient.PutAsJsonAsync("api/Station/UpdateStation", exist);
            Console.WriteLine($"Update {ip} Status To {status}");
        }
        else
        {
            exist = new Station();
            exist.UpdateTime = DateTime.Now.ToString();
            exist.StationIp = ip;
            //exist.IsStarted = true;
            await httpClient.PostAsJsonAsync("api/Station/CreateStation", exist);
            Console.WriteLine($"Create {ip}({status})");
        }
        //Console.WriteLine("获取ToolAgent状态：" + ip + ":" + status);
        await Clients.All.SendAsync("StatusInfo", ip, status);

    }

    //public async Task SendLog(string log)
    //{
    //    //Console.WriteLine("获取ToolAgent状态：" + ip + ":" + status);
    //    await Clients.All.SendAsync("ReceiveLog", log);
    //    //Console.WriteLine("end send ToolAgent status---" + ip + ":" + status);
    //}

    public async Task CommonLog(string context,string log)
    {
        //var eventName = (HubEventNameEnum.CommonLog.ToString() + context).Trim();
        //await Clients.All.SendAsync(eventName, log);
        await Clients.All.SendAsync(LogTypeEnum.CommonLog.ToString(),context, log);
    }

    public async Task ActivityStatusNow(string PDZId,string DefinitionId,string status)
    {
        await Clients.All.SendAsync(LogTypeEnum.ActivityStatusNow.ToString()+PDZId, PDZId, DefinitionId, status);
    }

    public async Task SendResumeElsaProcess(string bookmarkInfo)
    {
        await Clients.All.SendAsync("ResumeElsaProcess", bookmarkInfo);
    }

    public async Task CompleteActivityContext(string activityContext)
    {
        Console.WriteLine($"prepare to log CompleteActivityContext 2:{activityContext}");
        await Clients.All.SendAsync("CompleteActivityContext", activityContext);
    }
    
    public async Task PDZUpdatedInfo(string PDZId)
    {
        Console.WriteLine($"prepare to log PDZUpdatedInfo:{PDZId}");
        await Clients.All.SendAsync("PDZUpdatedInfo", PDZId);
    }

    public async Task PlugUpdated(string PlugDefiniitonId)
    {
        Console.WriteLine($"prepare to log PlugUpdated:{PlugDefiniitonId}");
        await Clients.All.SendAsync(LogTypeEnum.PlugUpdated.ToString(), PlugDefiniitonId);
    }

        public async Task JobStatusUpdated(string JobCorrelationId)
        {
            await Clients.All.SendAsync(LogTypeEnum.JobStatusUpdated.ToString(), JobCorrelationId);
        }

        /// <summary>
        /// 图站开始执行通知 (用于触发 Guacamole 远程桌面)
        /// </summary>
        public async Task StationExecuting(string PDZId, string PlugDefinitionId, string StationIp)
        {
            Console.WriteLine($"StationExecuting: {PlugDefinitionId} on {StationIp}");
            await Clients.All.SendAsync(LogTypeEnum.StationExecuting.ToString(), PDZId, PlugDefinitionId, StationIp);
        }
    }

