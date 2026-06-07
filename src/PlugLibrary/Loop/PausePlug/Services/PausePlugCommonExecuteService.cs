using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace PausePlug.Services
{
    public class PausePlugCommonExecuteService : BasePlugExecuteService
    {
        public PausePlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
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

                Log.Information($"execute pause plug: {plug?.Name}");
                var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

                if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
                {
                    return await ReportErrorResult(erd);
                }

                // 获取暂停时间参数
                var pauseSecondsStr = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.PauseSeconds.ToString());

                if (!int.TryParse(pauseSecondsStr, out int pauseSeconds) || pauseSeconds < 0)
                {
                    CLog.Error($"PausePlug 暂停时间参数无效: {pauseSecondsStr}", PlugDataZone?.PDZId);
                    erd.ResultString = "暂停时间参数无效";
                    return await ReportErrorResult(erd);
                }

                CLog.Information($"PausePlug 开始暂停 {pauseSeconds} 秒", PlugDataZone?.PDZId);

                // 报告暂停状态 - 使用 Suspended 状态表示流程引擎层面的暂停
                erd.ExecuteStatus = JobStatus.执行中;
                erd.ExecuteSubStatus = JobSubStatus.Suspended;
                erd.ResultString = $"暂停中，等待 {pauseSeconds} 秒后继续";
                await ExecuteResultReport(erd);

                // 启动后台任务在指定时间后恢复执行
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 等待指定时间
                        await Task.Delay(pauseSeconds * 1000);

                        CLog.Information($"PausePlug 暂停完成，准备恢复执行", PlugDataZone?.PDZId);

                        // 报告完成状态 - 触发流程引擎继续执行
                        var completeErd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
                        completeErd.ExecuteStatus = JobStatus.完成;
                        completeErd.ExecuteSubStatus = JobSubStatus.已完成;
                        completeErd.Outcome = ["Done"];
                        completeErd.ResultString = $"已暂停 {pauseSeconds} 秒";
                        await ReportCompletedResult(completeErd);
                    }
                    catch (Exception ex)
                    {
                        CLog.Error($"PausePlug 后台任务异常：{ex.Message}", PlugDataZone?.PDZId);
                        var errorErd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
                        await ReportErrorResult(errorErd);
                    }
                });

                // 返回当前状态（暂停中）
                return erd;
            }
            catch (Exception ex)
            {
                CLog.Error($"PausePlugCommonExecuteService 执行异常：{ex.Message}", PlugDataZone?.PDZId);
                return await ReportErrorResult(new ExecuteResultData());
            }
        }
    }
}
