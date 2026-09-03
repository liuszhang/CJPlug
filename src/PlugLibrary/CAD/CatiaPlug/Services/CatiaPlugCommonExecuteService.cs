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
using CatiaPlug;
using Serilog;
using System.Text.Json;

namespace CatiaPlug.Services
{
    /// <summary>
    /// Catia 主插头通用执行服务：读取 Catia 模型参数，必要时按刷新后的参数派发改参动作。
    /// </summary>
    public class CatiaPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {
        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CatiaPlug.CommonExecuteKey);

        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
            //前处理----------------------------
            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(CatiaPlugVariables)))) { return await ReportErrorResult(erd); }

            CLog.Information($"--执行Catia插头执行逻辑--", PlugDataZone.PDZId);
            CLog.Information($"PDZID: {PlugDataZone.PDZId}", PlugDataZone.PDZId);
            CLog.Information($"PlugID: {plugExecutionRequest?.PlugDefinitionId}", PlugDataZone.PDZId);
            var PlugVariableData = PlugDataZone.PlugVariableDatas.FirstOrDefault(p => p.PlugDefinitionId == plugExecutionRequest?.PlugDefinitionId && p.Name == CatiaPlugVariables.CatiaFile.ToString());
            var FileId = PlugVariableData.Value.GetFileIdFromFileVariable();
            CLog.Information($"FileID: {FileId}", PlugDataZone.PDZId);

            // 若存在 ModelParameters 且参数有更新，则派发 CatiaSetParameters 动作
            if (!string.IsNullOrEmpty(PlugDataZone?.GetVariableValue(plugExecutionRequest?.PlugDefinitionId, CatiaPlugVariables.ModelParameters.ToString())))
            {
                var ModelParameters = JsonSerializer.Deserialize<List<ModelParameter>?>(PlugDataZone?.GetVariableValue(plugExecutionRequest.PlugDefinitionId, CatiaPlugVariables.ModelParameters.ToString()));
                var newParameters = CatiaToolIntegrationUtils.ProcessSetParameters(ModelParameters, plugExecutionRequest.PlugDefinitionId, PlugDataZone);
                if (string.IsNullOrEmpty(newParameters))
                {
                    CLog.Information("参数无需刷新");
                    return await ReportCompletedResult(erd);
                }

                var TmpJobCorrelationId = "SetParameters" + RandomLongIdentityGenerator.GenerateId();
                var inputs = new List<PlugVariableData>();
                PlugVariableData.Name = PlugGlobalEnum.CatiaSetParameters.Variables.ModelFilePath;
                PlugVariableData.Type = VariableTypeEnum.File.ToString();
                PlugVariableData.IsInput = true;
                PlugVariableData.IsOutput = true;
                inputs.Add(PlugVariableData);
                inputs.Add(new PlugVariableData { Name = PlugGlobalEnum.CatiaSetParameters.Variables.NewParameterString, Value = newParameters });
                var toolCommandRequest = new PlugExecutionRequest
                {
                    PlugTypeKey = PlugGlobalEnum.CatiaSetParameters.TypeName,
                    InputVariables = inputs,
                    ExecuteResultData = new ExecuteResultData
                    {
                        Ids = new ExecuteIdsBundle
                        {
                            ToolJobCorrelationId = TmpJobCorrelationId
                        }
                    }
                };
                //使用动作执行的模式进行改参
                await MainApiClient.ExecutePlugByType(toolCommandRequest);
            }

            return await ReportCompletedResult(erd);
        }
    }
}
