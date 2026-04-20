
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.Extensions.DependencyInjection;
using NXPlug;
using Serilog;
using System.Text.Json;

public class NXPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
{
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
        //前处理----------------------------
        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }


        CLog.Information($"--执行NX插头执行逻辑--", PlugDataZone.PDZId);
        CLog.Information($"PDZID: {PlugDataZone.PDZId}", PlugDataZone.PDZId);
        CLog.Information($"PlugID: {plugExecutionRequest?.PlugDefinitionId}", PlugDataZone.PDZId);
        var PlugVariableData = PlugDataZone.PlugVariableDatas.FirstOrDefault(p => p.PlugDefinitionId == plugExecutionRequest?.PlugDefinitionId && p.Name == InitVariableNames.NXFile.ToString());
        var FileId = PlugVariableData.Value.GetFileIdFromFileVariable();
        CLog.Information($"FileID: {FileId}", PlugDataZone.PDZId);
        //var stream = await MainApiClient.DownloadFileByFileId(FileId);
        if (!string.IsNullOrEmpty(PlugDataZone?.GetVariableValue(plugExecutionRequest?.PlugDefinitionId, InitVariableNames.ModelParameters.ToString())))
        {
            var ModelParameters = JsonSerializer.Deserialize<List<ModelParameter>?>(PlugDataZone?.GetVariableValue(plugExecutionRequest.PlugDefinitionId, InitVariableNames.ModelParameters.ToString()));
            var newParameters = ToolIntegrationUtils.ProcessSetParameters(ModelParameters, plugExecutionRequest.PlugDefinitionId, PlugDataZone);
            if (string.IsNullOrEmpty(newParameters))
            {
                CLog.Information("参数无需刷新");
                return await ReportCompletedResult(erd);
            }

            var TmpJobCorrelationId = "SetParameters" + RandomLongIdentityGenerator.GenerateId();
            var inputs = new List<PlugVariableData>();
            PlugVariableData.Name = PlugGlobalEnum.NXSetParameters.Variables.ModelFilePath;
            PlugVariableData.Type = VariableTypeEnum.File.ToString();
            PlugVariableData.IsInput = true;
            PlugVariableData.IsOutput = true;
            inputs.Add(PlugVariableData);
            inputs.Add(new PlugVariableData { Name = PlugGlobalEnum.NXSetParameters.Variables.NewParameterString, Value = newParameters });
            var toolCommandRequest = new PlugExecutionRequest
            {
                PlugType = PlugGlobalEnum.NXSetParameters.TypeName,
                //SpecifiedStationIp = stationIp.StationIp,
                //ExecuteMode = ExecuteMode.Standalone,
                InputVariables = inputs,
                ExecuteResultData = new ExecuteResultData
                {
                    Ids = new ExecuteIdsBundle
                    {
                        ToolJobCorrelationId = TmpJobCorrelationId
                    }
                }
            };
            //使用动作执行的模式进行获取参数
            await MainApiClient.ExecutePlugByType(toolCommandRequest);
        }


        return await ReportCompletedResult(erd);
    }

    
}

