using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;

namespace CJ.Plug.Models.Shared
{
    /// <summary>
    /// 统一变量解析器 —— 所有执行路径的参数获取入口。
    /// 不抛异常，所有缺失值返回 null，由调用方决定是否处理。
    /// </summary>
    public static class VariableResolver
    {
        /// <summary>
        /// Plug/Action 模式：PDZ 运行时值 → Plug 定义层 Value（元数据回退）
        /// </summary>
        /// <param name="variableName">变量名</param>
        /// <param name="pdz">PlugDataZone 实例</param>
        /// <param name="plugDefinitionId">当前插头的定义 ID</param>
        /// <param name="plugVariables">插头定义的变量列表（PDZ 无值时回退到此）</param>
        /// <returns>解析后的值，全链路缺失则返回 null</returns>
        public static string? ResolveFromPDZ(
            string variableName,
            PlugDataZone pdz,
            string plugDefinitionId,
            List<PlugVariable>? plugVariables = null)
        {
            // Step 1: PDZ 运行时值
            var pdzValue = pdz.GetVariableValue(plugDefinitionId, variableName);
            if (pdzValue != null)
                return pdzValue;

            // Step 2: Plug 定义层 Value（从插头定义回退）
            if (plugVariables != null)
            {
                var plugVar = plugVariables.FirstOrDefault(v => v.Name == variableName);
                if (plugVar?.Value != null)
                {
                    CLog.Warning($"变量 {variableName} 在 PDZ 中无运行时值，回退到 Plug 定义层 Value: {plugVar.Value}");
                    return plugVar.Value;
                }
            }

            // Step 3: 全链路未找到
            CLog.Warning($"变量 {variableName} 在所有数据源中均未找到值");
            return null;
        }

        /// <summary>
        /// Standalone 模式：InputVariables → PlugVariables.Value
        /// </summary>
        /// <param name="variableName">变量名</param>
        /// <param name="inputVariables">调用方传入的参数字典（可能为 null）</param>
        /// <param name="plugVariables">插头定义的变量列表（可能为 null）</param>
        /// <returns>解析后的值，全链路缺失则返回 null</returns>
        public static string? ResolveStandalone(
            string variableName,
            List<PlugVariableData>? inputVariables,
            List<PlugVariable>? plugVariables)
        {
            // Step 1: InputVariables（调用方传入）
            if (inputVariables != null)
            {
                var inputVar = inputVariables.FirstOrDefault(v => v.Name == variableName);
                if (inputVar?.Value != null)
                    return inputVar.Value;
            }

            // Step 2: PlugVariables.Value
            if (plugVariables != null)
            {
                var plugVar = plugVariables.FirstOrDefault(v => v.Name == variableName);
                if (plugVar?.Value != null)
                    return plugVar.Value;
            }

            // Step 3: 全链路未找到
            CLog.Warning($"变量 {variableName} 在所有 Standalone 数据源中均未找到值");
            return null;
        }
    }
}
