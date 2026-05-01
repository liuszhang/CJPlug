using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace AndPlug.Services
{
    public class AndPlugCommonExecuteService : BasePlugExecuteService
    {
        public AndPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            Plug plug = context.plugToExecute;

            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute and plug: {plug.Name}");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

            var pdz = await PDZApiClient.GetPDZByPDZIdAsync(erd.Ids.PDZId);
            if (pdz == null)
            {
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }

            var conditionA = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.ConditionExpressionA.ToString());
            var conditionB = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.ConditionExpressionB.ToString());

            if (bool.TryParse(conditionA, out var resultA) && bool.TryParse(conditionB, out var resultB) && resultA && resultB)
            {
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.Outcome = [InitOutcomes.True.ToString()];
            }
            else
            {
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.Outcome = [InitOutcomes.False.ToString()];
            }

            return await ExecuteResultReport(erd);
        }


    }
}
