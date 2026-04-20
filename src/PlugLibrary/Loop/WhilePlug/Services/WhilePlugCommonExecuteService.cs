
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.ElsaIntegration.Contracts;
using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Serilog;
using System.Text.Json;
using WhilePlug;

namespace WhilePlug.Services
{
    public class WhilePlugCommonExecuteService : BasePlugExecuteService
    {
        private ElsaApiClient ElsaApiClient { get; }

        public WhilePlugCommonExecuteService(IServiceProvider serviceProvider,ElsaApiClient elsaApiClient):base(serviceProvider)
        {
            ElsaApiClient = elsaApiClient;
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            Plug plugToExecute = context.plugToExecute;
            
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
            Log.Information($"execute while plug: {plugToExecute.Name}");

            var result = plugExecutionRequest.ExecuteResultData ?? new ExecuteResultData();
            if(!await DataPrepare(plugExecutionRequest)) { return await ReportErrorResult(result); }

            try
            {
                //判断条件表达式是否满足，以决定是否执行循环
                var ConditionExpression = PlugDataZone.GetVariableValue(plugToExecute.DefinitionId, InitVariableNames.ConditionExpression.ToString());
                if (string.IsNullOrEmpty(ConditionExpression))
                {
                    Log.Information("表达式未配置。");
                    return await ReportCompletedResult(result);
                }
                var ConditionExp = JsonSerializer.Deserialize<ConditionExpression>(ConditionExpression);
                var LeftValue = PlugDataZone.GetVariableValue(ConditionExp.PlugDefinitionId, ConditionExp.VariableName);
                var RightValue = ConditionExp.ConditionValue;
                var Expression = ConditionExp.Expression;
                var CalcResult = CalcExression(LeftValue, Expression, RightValue);
                if (!CalcResult) { return await ReportCompletedResult(result); }

                var json = PlugDataZone.GetFlowchartData(plugToExecute.DefinitionId);
                json.Remove("nodeId");
                json.Remove("PlugDefinitionId");
                json.Remove("version");
                json.Remove("isContainer");
                json.Remove("metadata");

                var executeSetting = new ExecuteSetting
                {
                    CorrelationId = plugExecutionRequest.ExecuteResultData.Ids.PDZId+"Child",
                    //TriggerActivityId = plugToExecute.TriggerPlugDefinitionId,
                };
                //暂时不支持设置容器类组件的触发活动                
                result = await ElsaApiClient.ExecuteWorkflowWithExecuteSetting(json, executeSetting);
                result.Ids = plugExecutionRequest?.ExecuteResultData.Ids;
                Log.Information($"while plug 子流程执行完毕。{result.Ids.JobCorrelationId}-{result.Ids.PlugDefinitionId}");
                result.Ids.PlugDefinitionId = plugToExecute.DefinitionId;

                return await ExecuteResultReport(result);
            }
            catch(Exception ex)
            {
                CLog.Error(ex.Message);
                CLog.Error(ex.StackTrace);
                return await ReportErrorResult(result);
            }
            
        }


        private bool CalcExression(string? LeftValue, string? Expression, string? RightValue)
        {
            //if(Expression=="==") { return LeftValue==RightValue; }
            //if(Expression=="!=") { return LeftValue!=RightValue; }

            Log.Information($"Expression:{LeftValue}{Expression}{RightValue}");
            return true;
        }


    }
}

