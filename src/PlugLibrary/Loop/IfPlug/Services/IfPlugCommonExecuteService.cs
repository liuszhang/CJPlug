using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace IfPlug.Services
{
    public class IfPlugCommonExecuteService : BasePlugExecuteService
    {
        public IfPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            Plug plug = context.plugToExecute;
            
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute while plug: {plug.Name}");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

            var pdz = await PDZApiClient.GetPDZByPDZIdAsync(erd.Ids.PDZId);
            if (pdz == null)
            {
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }
            var from = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.ConditionExpression.ToString());
            var to = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.To.ToString());
            var step = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.Step.ToString());

            for (int i = int.Parse(from); i < int.Parse(to); i += int.Parse(step))
            {
                erd.ExecuteStatus = JobStatus.执行中;
                erd.ExecuteSubStatus = JobSubStatus.循环中;
                erd.Outcome = [InitOutcomes.False.ToString()];
                pdz.SetVariableValue(plug.DefinitionId, InitVariableNames.ConditionExpression.ToString(), (i + int.Parse(step)).ToString());
                await PDZApiClient.CreateOrUpdatePDZ(pdz);
                StatusReporter.PDZUpdated(pdz.PDZId);
                return await ExecuteResultReport(erd);
            }

            erd.ExecuteStatus = JobStatus.完成;
            erd.ExecuteSubStatus = JobSubStatus.已完成;
            erd.Outcome = [InitOutcomes.False.ToString()];
            return await ExecuteResultReport(erd);
        }


    }
}

