using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.PlugBaseCore.Models;
using CalculatorPlug.Services;
using Serilog;

namespace CalculatorPlug.Services
{
    public class CalculatorPlugCommonExecuteService : BasePlugExecuteService
    {
        public CalculatorPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            try
            {
                Plug plug = context.plugToExecute;
                PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

                CLog.Information($"execute calculator plug: {plug?.Name}");
                var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

                // 1. 数据准备：解析变量引用
                if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
                {
                    return await ReportErrorResult(erd);
                }

                // 2. 获取表达式
                var plugDefinitionId = erd.Ids.PlugDefinitionId;
                var expression = PlugDataZone?.GetVariableValue(plugDefinitionId, nameof(InitVariableNames.Expression));
                if (string.IsNullOrWhiteSpace(expression))
                {
                    CLog.Error("计算器插件：表达式为空", PlugDataZone.PDZId);
                    erd.ExecuteStatus = JobStatus.完成;
                    erd.ExecuteSubStatus = JobSubStatus.出错;
                    erd.ExecuteResultMessage = "表达式不能为空";
                    return await ExecuteResultReport(erd);
                }

                CLog.Information($"计算器表达式: {expression}");

                // 3. 调用通用计算引擎求值
                var evalResult = CalculatorEngine.Evaluate(expression);

                if (!evalResult.Success)
                {
                    CLog.Error($"计算器求值失败: {evalResult.Error}",PlugDataZone.PDZId);
                    erd.ExecuteStatus = JobStatus.完成;
                    erd.ExecuteSubStatus = JobSubStatus.出错;
                    erd.ExecuteResultMessage = evalResult.Error ?? "计算失败";
                    return await ExecuteResultReport(erd);
                }

                // 4. 将结果写入 PDZ 变量
                var resultStr = evalResult.Value.ToString("G");
                PlugDataZone?.SetVariableValue(plugDefinitionId, nameof(InitVariableNames.Result), resultStr);
                await PDZApiClient!.CreateOrUpdatePDZ(PlugDataZone!);

                CLog.Information($"计算器结果: {expression} = {resultStr}", PlugDataZone.PDZId);

                // 5. 汇报完成
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.ExecuteResultMessage = resultStr;
                return await ExecuteResultReport(erd);
            }
            catch (Exception ex)
            {
                CLog.Error(ex.ToString()+ "计算器插件执行异常", PlugDataZone.PDZId);
                return await ReportErrorResult(new ExecuteResultData());
            }
        }
    }
}
