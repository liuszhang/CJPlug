using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;
using System.Text.Json;

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

            Log.Information($"execute if plug: {plug.Name}");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();


            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }


            // 获取 PDZ 中该插头的条件表达式参数值
            var pdz = await PDZApiClient.GetPDZByPDZIdAsync(erd.Ids.PDZId);
            if (pdz == null)
            {
                Log.Error("IFPlug: 未找到数据空间");
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }

            var conditionValue = pdz.GetVariableValue(plug.DefinitionId, InitVariableNames.ConditionExpression.ToString());
            if (string.IsNullOrEmpty(conditionValue))
            {
                Log.Information("IFPlug: 条件表达式为空，结果为 False");
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }

            try
            {
                var condition = JsonSerializer.Deserialize<ConditionExpression>(conditionValue);
                if (condition == null || string.IsNullOrEmpty(condition.VariableName))
                {
                    Log.Information("IFPlug: 条件表达式无效，结果为 False");
                    erd.ExecuteStatus = JobStatus.完成;
                    erd.ExecuteSubStatus = JobSubStatus.已完成;
                    erd.Outcome = [InitOutcomes.False.ToString()];
                    return await ExecuteResultReport(erd);
                }

                // 获取条件表达式中引用的变量实际值
                // 如果条件值来源于另一个变量，则获取该变量的值作为条件值
                string? actualConditionValue;
                if (condition.IsValueFromVariable && !string.IsNullOrEmpty(condition.SourceVariableName))
                {
                    actualConditionValue = pdz.GetVariableValue(
                        condition.PlugDefinitionId,
                        condition.SourceVariableName);
                }
                else
                {
                    actualConditionValue = condition.ConditionValue;
                }

                // 获取被比较的变量的实际值
                var actualVariableValue = pdz.GetVariableValue(
                    condition.PlugDefinitionId,
                    condition.VariableName);

                Log.Information($"IFPlug: 变量[{condition.VariableName}] = '{actualVariableValue}', " +
                    $"条件值 = '{actualConditionValue}', 表达式 = '{condition.Expression}'");

                // 执行条件判断
                bool result = EvaluateCondition(actualVariableValue, actualConditionValue, condition.Expression);

                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.Outcome = [result ? InitOutcomes.True.ToString() : InitOutcomes.False.ToString()];

                Log.Information($"IFPlug: 条件判断结果 = {result}");
                return await ExecuteResultReport(erd);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "IFPlug: 条件表达式计算失败");
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }
        }

        /// <summary>
        /// 根据表达式比较两个值
        /// 支持的表达式: ==, !=, >, <, >=, <=
        /// </summary>
        private static bool EvaluateCondition(string? variableValue, string? conditionValue, string? expression)
        {
            // 空值处理：null 和 空字符串 视为相等
            if (variableValue == null && conditionValue == null)
                return expression == "==" || expression == ">=" || expression == "<=";

            if (variableValue == null || conditionValue == null)
                return expression == "!=";

            var expr = expression ?? "==";

            // 尝试数值比较
            if (double.TryParse(variableValue, out var numVar) && double.TryParse(conditionValue, out var numCond))
            {
                return expr switch
                {
                    "==" => Math.Abs(numVar - numCond) < 0.0001,
                    "!=" => Math.Abs(numVar - numCond) >= 0.0001,
                    ">" => numVar > numCond,
                    "<" => numVar < numCond,
                    ">=" => numVar >= numCond,
                    "<=" => numVar <= numCond,
                    _ => string.Equals(variableValue, conditionValue, StringComparison.OrdinalIgnoreCase)
                };
            }

            // 尝试 bool 比较
            if (bool.TryParse(variableValue, out var boolVar) && bool.TryParse(conditionValue, out var boolCond))
            {
                return expr switch
                {
                    "==" => boolVar == boolCond,
                    "!=" => boolVar != boolCond,
                    _ => boolVar == boolCond
                };
            }

            // 默认字符串比较
            return expr switch
            {
                "==" => string.Equals(variableValue, conditionValue, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(variableValue, conditionValue, StringComparison.OrdinalIgnoreCase),
                _ => string.Equals(variableValue, conditionValue, StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}

