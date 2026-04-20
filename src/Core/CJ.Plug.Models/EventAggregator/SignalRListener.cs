using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CJ.Plug.Models.EventAggregator
{
    public static class SignalRListener
    {
        //public static HubConnectionManagerService ListenActivityStatus(this HubConnectionManagerService HubConnectionManagerService,
        //    string PDZId,string DefinitionId,string status)
        //{
        //    HubConnectionManagerService._hubConnection.Remove(HubEventNameEnum.ActivityStatusNow.ToString());
        //    //通过长连接添加活动状态监听
        //    HubConnectionManagerService._hubConnection.On<string, string, string>(HubEventNameEnum.ActivityStatusNow.ToString(), (PDZId, DefinitionId, status) =>
        //    {
        //        Console.WriteLine($"NewStatus:{PDZId}-{DefinitionId}-{status}");
        //            var tmpStats = JsonSerializer.Deserialize<ActivityStats>(status);
        //            ActivityStats[DefinitionId] = tmpStats ?? new ActivityStats();

        //    });
        //}


        //public static void SubscribeActivityStatus(
        //HubConnection hubConnection,
        //string pdzId,
        //Dictionary<string, ActivityStats> activityStats,
        //Action stateHasChangedAction)
        //{
        //    var eventName = HubEventNameEnum.ActivityStatusNow.ToString();

        //    // 先移除旧监听（避免重复注册）
        //    hubConnection.Remove(eventName);

        //    // 添加新监听
        //    hubConnection.On<string, string, string>(eventName, (PDZId, DefinitionId, status) =>
        //    {
        //        if (PDZId != pdzId) return;

        //        Console.WriteLine($"NewStatus:{PDZId}-{DefinitionId}-{status}");

        //        // 更新状态并触发UI刷新
        //        stateHasChangedAction?.Invoke(() =>
        //        {
        //            var tmpStats = JsonSerializer.Deserialize<ActivityStats>(status);
        //            activityStats[DefinitionId] = tmpStats ?? new ActivityStats();
        //        });
        //    });
        //}
    }
}
