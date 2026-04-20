using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services.ToolActionExecute;
using MediatR;
using OpenTelemetry.Trace;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace CJ.Plug_Aspire.StationApiService.Services
{
    public class DefaultStationExecuteService : IStationExecuteService
    {
        private MainApiClient MainApiClient { get; set; }

        public DefaultStationExecuteService(MainApiClient mainApiClient)
        {
            MainApiClient = mainApiClient;
        }

        public async Task<ExecuteResultData?> ExecuteRequestCommand(PlugExecutionRequest stationExecutionRequest)
        {
            //var tmp = JsonSerializer.Serialize(stationExecutionRequest);

            //Log.Information(stationExecutionRequest.ExecutionToolType+"-"+stationExecutionRequest.ToolName);
            //Log.Information($"开始执行IDs: {JsonSerializer.Serialize(stationExecutionRequest.ExecuteResultData.Ids)}");
            //Log.Information("开始执行CMD命令");                    
            //执行CMD脚本并返回结果
            if (stationExecutionRequest.ExecuteMode == ExecuteMode.Standalone)
            {
                return await ExecuteActions.InvokeStationAgent(stationExecutionRequest);
            }
            await ExecuteActions.InvokeStationAgentAsync(stationExecutionRequest);
            //Log.Information("完成提交CMD命令");
            return null;
        }


        /// <summary>
        /// 供StationAgent调用的执行结果上报方法
        /// </summary>
        /// <param name="executeResultData"></param>
        /// <returns></returns>
        public async Task ReportExecuteResult(ExecuteResultData executeResultData)
        {
            //Log.Information($"开始汇报执行结果: {JsonSerializer.Serialize(executeResultData.Ids.ToolJobCorrelationId)}");
            var toolJob = await MainApiClient.GetToolJobByCorrelationIdAsync(executeResultData.Ids.ToolJobCorrelationId);
            if (toolJob == null)
            {
                CLog.Error($"未找到对应的作业，CorrelationId: {executeResultData.Ids.ToolJobCorrelationId}");
                return;
            }
            //Log.Information($"找到对应的作业:{JsonSerializer.Serialize(toolJob)}");
            if (toolJob.JobCategory != JobCategoryEnum.ToolJob.ToString())
            {
                CLog.Error($"作业类型不匹配，预期 ToolJob，但实际为{toolJob.JobCategory}");
                return;
            }
            toolJob.JobStatus = executeResultData.ExecuteStatus.ToString();
            toolJob.JobSubStatus= executeResultData.ExecuteSubStatus.ToString();
            toolJob.ExecuteResultData = JsonSerializer.Serialize(executeResultData);
            //Log.Information($"更新作业执行结果数据: {toolJob.ExecuteResultData}");
            var result = await MainApiClient.UpdateToolJobAsync(toolJob);
            //Log.Information($"更新作业结果成功: {JsonSerializer.Serialize(result)}");
            //汇报作业更新状态，主要用于通知前端，如获取NX参数执行完毕后，通知前端去相关作业上取数据
            StatusReporter.JobStatusUpdated(executeResultData.Ids.ToolJobCorrelationId);
            if (!string.IsNullOrEmpty(executeResultData.Ids.PlugDefinitionId))
            {
                //汇报执行结果状态，主要用于触发后端的后续操作
                await MainApiClient.ExecuteResultReport(executeResultData);
            }
            
        }

        public async Task SendLog(LogModel log)
        {
            Log.Information(log.Description);
        }
    }
}
