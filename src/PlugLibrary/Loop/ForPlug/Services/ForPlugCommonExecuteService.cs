using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace ForPlug.Services
{
    public class ForPlugCommonExecuteService : BasePlugExecuteService
    {
        public ForPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            try
            {

                Plug plug = context.plugToExecute;

                string? PlugDefinitionId = context.plugExecutionRequest?.ExecuteResultData?.Ids?.PlugDefinitionId;
                PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

                Log.Information($"execute for plug: {plug?.Name}");
                var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

                if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

                //将插头的参数处理为实际值
                var from = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.From.ToString());
                var to = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.To.ToString());
                var step = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.Step.ToString());
                CLog.Information($"ForPlug执行参数：From={from}, To={to}, Step={step}", PlugDataZone.PDZId);


                if (int.Parse(from) > int.Parse(to))
                {
                    erd.ExecuteStatus = JobStatus.完成;
                    erd.ExecuteSubStatus = JobSubStatus.已完成;
                    erd.Outcome = [InitOutcomes.结束.ToString()];
                    return await ExecuteResultReport(erd);
                }
                else
                {
                    erd.ExecuteStatus = JobStatus.执行中;
                    erd.ExecuteSubStatus = JobSubStatus.循环中;
                    erd.Outcome = [InitOutcomes.循环.ToString()];

                    var plugVariableData = PlugDataZone.SetVariableValue(PlugDefinitionId, InitVariableNames.From.ToString(), (int.Parse(from) + int.Parse(step)).ToString());
                    //仅更新变量数据，降低数据库压力
                    await MainApiClient.UpdatePlugVariableData(plugVariableData);
                    return await ExecuteResultReport(erd);
                }

            }
            catch (Exception ex)
            {
                CLog.Error($"ForPlugCommonExecuteService 执行异常：{ex.Message}",PlugDataZone?.PDZId);
                return await ReportErrorResult(new ExecuteResultData());
            }
        }


    }
}

