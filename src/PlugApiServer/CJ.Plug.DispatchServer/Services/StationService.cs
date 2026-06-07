using CJ.Plug.DispatchServer.Contracts;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CJ.Plug.DispatchServer.Services
{
    public class StationService : IStationService
    {
        private HubConnectionManagerService? HubConnectionManagerService {  get; set; }
        private int i { get; set; }

        public StationService(HubConnectionManagerService? hubConnectionManagerService)
        {
            HubConnectionManagerService = hubConnectionManagerService;
            i = 0;
        }


        /// <summary>
        /// 分发图站执行节点
        /// </summary>
        /// <returns></returns>
        public async Task<string?> GetStationToExecute()
        {
            Console.WriteLine($"start to get stationStatus");

            var stationLists = await GetAllOnlineStation();
            var stationStatus= stationLists?.LastOrDefault();
            Console.WriteLine(stationStatus);
            
            if (string.IsNullOrEmpty(stationStatus))
            {
                CLog.Error("No online station available.");
                return null;
            }

            if (stationStatus.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                stationStatus.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return stationStatus;
            }
            else if (stationStatus.StartsWith("://"))
            {
                return "http" + stationStatus;
            }
            else
            {
                return "http://" + stationStatus;
            }
        }

        public async Task<List<string>?> GetAllOnlineStation()
        {
            Console.WriteLine($"start to get all stationStatus");
            HubConnectionManagerService.StationList.Clear();
            await HubConnectionManagerService.ConnectAsync();
            await HubConnectionManagerService.InvokeAsync<string>("TellMeStatus", null);
            //留1秒钟事件给长连接进行通信
            await Task.Delay(3000);
            var stationStatus = HubConnectionManagerService.StationList;
            
            if (stationStatus == null || stationStatus.Count == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine($"retry times:{i}");
                    await HubConnectionManagerService.InvokeAsync<string>("TellMeStatus", null);
                    await Task.Delay(3000);
                    stationStatus = HubConnectionManagerService.StationList;
                    if (stationStatus != null && stationStatus.Count > 0)
                    {
                        Console.WriteLine("get it!");
                        break;
                    }
                }                
            }
            Console.WriteLine(JsonSerializer.Serialize(stationStatus));
            return stationStatus;
        }


        /// <summary>
        /// 考虑后续分布式服务的部署，通过调度层分发API服务器节点
        /// </summary>
        /// <returns></returns>
        public async Task<string?> GetApiServer()
        {
            return GlobalData.MainApiServer;
        }

        /// <summary>
        /// 考虑后续分布式服务的部署，通过调度层分发API服务器节点
        /// </summary>
        /// <returns></returns>
        public async Task<string?> GetElsaEngineServer()
        {
            return GlobalData.ElsaEngineServer;
        }

        public async Task<string?> GetElsaEngineApiKey()
        {
            return GlobalData.ElsaEngineApiKey;
        }
    }
}
