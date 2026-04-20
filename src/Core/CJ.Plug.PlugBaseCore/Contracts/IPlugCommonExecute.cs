using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugCommonExecute
    {
        bool IsThisPlugTypeKey(string? PlugTypeKey);

        Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context);


        /// <summary>
        /// 执行结果上报通用方法
        /// </summary>
        /// <param name="MainApiClient"></param>
        /// <param name="plugToExecute"></param>
        /// <param name="plugExecutionRequest"></param>
        /// <param name="OutputResults"></param>
        /// <param name="JobStatus"></param>
        /// <param name="JobSubStatus"></param>
        /// <returns></returns>
        [Obsolete]
        public async Task<ExecuteResultData> ExecuteResultReport(
            MainApiClient MainApiClient,
            Plug.Models.Plug.Plug plugToExecute,
            PlugExecutionRequest? plugExecutionRequest,
            Dictionary<string, string>? OutputResults,
            JobStatus? JobStatus = JobStatus.执行中,
            JobSubStatus? JobSubStatus = JobSubStatus.执行中)
        {
            var result = new ExecuteResultData
            {
                Ids = plugExecutionRequest.ExecuteResultData.Ids,
                ResultString = JsonSerializer.Serialize(OutputResults),
                ExecuteStatus = JobStatus,
                ExecuteSubStatus = JobSubStatus
                //ExecuteResultMessage = resultMessage
            };
            await MainApiClient.ExecuteResultReport(result);
            
            //Log.Information($"{plugToExecute.Name}执行完成");

            return result;
        }


        [Obsolete]
        public async Task<ExecuteResultData> ExecuteResultReport(
            MainApiClient MainApiClient,
            ExecuteResultData executeResultData)
        {
            await MainApiClient.ExecuteResultReport(executeResultData);

            //Log.Information($"{plugToExecute.Name}执行完成");

            return executeResultData;
        }

    }

}
