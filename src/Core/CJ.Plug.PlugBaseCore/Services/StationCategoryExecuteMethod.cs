using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApiClient;
using Elsa.Api.Client.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Services
{
    public class StationCategoryExecuteMethod
    {
        public static async Task<ExecuteResultData?> Execute(
            Plug.Models.Plug.Plug plug,
            PlugExecutionRequest? plugExecutionRequest,
            IPDZApiClient MainApiClient,
            IToolExecuteService ToolExecuteService)
        {
            var resultData = new ExecuteResultData() { Ids = plugExecutionRequest.ExecuteResultData.Ids };
            var status = plugExecutionRequest.ExecuteResultData?.ExecuteSubStatus;
            Log.Information($"准备执行的阶段：{status.ToString()}");

            if (status == JobSubStatus.提交 || string.IsNullOrEmpty(status?.ToString()))
            {
                plugExecutionRequest.ToolName= string.IsNullOrEmpty(plugExecutionRequest.ToolName) ? plug.ToolName: plugExecutionRequest.ToolName;
                plugExecutionRequest.ToolVersion = string.IsNullOrEmpty(plugExecutionRequest.ToolVersion) ? plug.ToolVersion : plugExecutionRequest.ToolVersion;
                plugExecutionRequest.RequestCommand = string.IsNullOrEmpty(plugExecutionRequest.RequestCommand) ? plug.ToolCommandLineShema : plugExecutionRequest.RequestCommand;
                if(plugExecutionRequest.InputVariables.Count==0&&!string.IsNullOrEmpty(plugExecutionRequest.ExecuteResultData.Ids.PDZId))
                {
                    //这里应该使用PDZ的参数
                    var PDZ = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                    var plugVariables = PDZ?.GetVariablesOfPlug(plug.DefinitionId);
                    plugVariables?.ForEach(p =>
                    {
                            plugExecutionRequest.InputVariables.Add(new()
                            {
                                Name = p.Name,
                                Value = p.Value,
                                Type = p.Type,
                                IsInput= p.IsInput,
                                IsOutput = p.IsOutput,
                            });                        
                    });
                }
                resultData = await ToolExecuteService.ExecuteToolAsync(plugExecutionRequest);
                return resultData;
            }
            else if (status == JobSubStatus.图站执行完成)
            {
                Log.Information("图站执行完成，执行数据后处理");
                try
                {
                    //后处理-----------------------------
                    //将plugExecutionRequest.ExecuteResultData的值写入相应的数据空间
                    //Log.Information($"PDZ:{plugExecutionRequest.ExecuteResultData.Ids.PDZId}");
                    if (string.IsNullOrEmpty(plugExecutionRequest?.ExecuteResultData?.Ids?.PDZId))
                    {
                        Log.Information("无PDZ信息，无需更新PDZ");
                        resultData.ExecuteStatus = JobStatus.完成;
                        resultData.ExecuteSubStatus = JobSubStatus.已完成;
                        return resultData;
                    }
                    Log.Information("开始将数据回写至PDZ");
                    var PDZ = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                    var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
                    var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
                    var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;
                    if (identityId == null)
                    {
                        PDZ?.SetVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, "ResultString", resultString);
                    }
                    else
                    {
                        PDZ.SetActionVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, identityId, "ResultString", resultString);
                    }
                    await MainApiClient.CreateOrUpdatePDZ(PDZ);
                    resultData.ExecuteStatus = JobStatus.完成;
                    resultData.ExecuteSubStatus = JobSubStatus.已完成;
                    return resultData;
                }
                catch (Exception ex)
                {
                    CLog.Error($"{plug.Name}后处理执行失败：{ex.Message}");
                    resultData.ExecuteStatus = JobStatus.完成;
                    resultData.ExecuteSubStatus = JobSubStatus.出错;
                    return resultData;
                }
            }
            return resultData;

        }

    }



}
