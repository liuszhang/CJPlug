using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.StationAgent.Contracts;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CJ.Plug.StationAgent.Services
{
    [Obsolete]
    public class ClientApiService : IClientApiService
    {
        private StationApiClient StationApiClient;
        //private MainApiClient MainApiClient;

        public ClientApiService(StationApiClient stationApiClient)
        {
            StationApiClient = stationApiClient;
            //MainApiClient = mainApiClient;
        }

        public async Task SendLog(object logString, string? logLevel = "Information")
        {
            try
            {
                var Log = new LogModel
                {
                    Description = logString.ToString(),
                    Type = logLevel,
                    Author = "StationAgent",
                };
                await StationApiClient.SendLog(Log);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending log3: " + ex.Message);
            }
        }

        public async Task SendResult(PlugExecutionRequest executeRequest, string? resultString, JobSubStatus? status)
        {
            //SendLog($"工具执行请求：{JsonSerializer.Serialize(executeRequest)}");
            //SendLog($"工具执行结果：{resultString}");
            //SendLog($"工具执行状态：{status.ToString()}");
            //SendLog($"开始汇报执行结果1: {JsonSerializer.Serialize(executeRequest.ExecuteResultData.Ids)}");

            await StationApiClient.SendResult(executeRequest, resultString, status);
        }
    }
}
