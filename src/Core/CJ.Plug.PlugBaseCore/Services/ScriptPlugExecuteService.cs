using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace CJ.Plug.PlugBaseCore.Services;

/// <summary>
/// 脚本类插头（PythonPlug、JavaPlug 等）的公共执行基类。
/// 脚本在服务端本地执行（进程调用），不经过图站分发。
/// 与 <see cref="StationPlugExecuteService"/> / <see cref="ApiPlugExecuteService"/> 同级。
///
/// 模板方法执行流程：
/// 1. DataPrepare — 数据准备（从 PDZ 读取所需变量）
/// 2. GetScriptCode — 从 PDZ 或 InputVariables 读取脚本代码
/// 3. PreprocessScriptAsync — 预处理钩子（参数通配符替换等，默认 no-op）
/// 4. ExecuteScriptAsync — 本地执行脚本（子类实现，如启 python/java 进程）
/// 5. WriteResultToPDZ — 将执行结果写回 PDZ
/// 6. ReportCompletedResult — 报告完成
///
/// 子类只需：
/// 1. 实现 <see cref="ScriptVariableName"/> — 脚本变量名（"PythonScript"/"JavaCode"）
/// 2. 实现 <see cref="DataPrepareVariableNames"/> — 需从 PDZ 读取的变量名
/// 3. 实现 <see cref="ExecuteScriptAsync"/> — 本地执行脚本代码
/// 4. 可选重写 <see cref="PreprocessScriptAsync"/> — 参数通配符替换（PythonPlug）
/// 5. 可选重写 <see cref="IsThisPlugTypeKey"/> — 默认实现返回 true（匹配所有）
/// </summary>
public abstract class ScriptPlugExecuteService : BasePlugExecuteService
{
    // ====== New Abstract Members ======

    /// <summary>
    /// PDZ 中存储脚本代码的变量名，如 "PythonScript"、"JavaCode"
    /// </summary>
    protected abstract string ScriptVariableName { get; }

    /// <summary>
    /// 获取用于从 PDZ 读取变量值的变量名数组。
    /// 通常为插头 InitVariableNames 枚举的所有值。
    /// </summary>
    protected abstract string[]? DataPrepareVariableNames { get; }

    /// <summary>
    /// 在服务端本地执行脚本代码。
    /// 子类负责：启动本地进程（如 python、javac+java）、捕获输出、返回结果。
    /// </summary>
    /// <param name="scriptCode">预处理后的脚本代码</param>
    /// <param name="request">执行请求（含 PDZ 信息）</param>
    /// <returns>执行结果数据。成功时 ResultString 为进程输出，失败时为错误信息。</returns>
    protected abstract Task<ExecuteResultData?> ExecuteScriptAsync(
        string scriptCode, PlugExecutionRequest request);

    // ====== Constructor ======

    protected ScriptPlugExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    // ====== BasePlugExecuteService: IsThisPlugTypeKey ======

    /// <summary>
    /// 默认返回 true（匹配所有插头类型键）。
    /// 子类可重写以精确匹配。
    /// </summary>
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => true;

    // ====== Template Method: PlugCommonExecute ======

    /// <inheritdoc/>
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        var plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        // ---- Step 1: DataPrepare ----
        if (DataPrepareVariableNames is { Length: > 0 })
        {
            if (!await DataPrepare(plugExecutionRequest, DataPrepareVariableNames))
                return await ReportErrorResult(erd);
        }

        CLog.Information($"开始执行 {GetType().Name} 脚本插头", PlugDataZone?.PDZId);

        // ---- Step 2: GetScriptCode ----
        var scriptCode = await GetScriptCodeAsync(plugExecutionRequest!);
        if (scriptCode == null)
        {
            CLog.Error("获取脚本代码失败");
            erd.ResultString = "获取脚本代码失败";
            return await ReportErrorResult(erd);
        }

        if (string.IsNullOrEmpty(scriptCode))
        {
            CLog.Warning($"{ScriptVariableName} 内容为空，跳过执行");
            erd.ResultString = "";
            return await ReportCompletedResult(erd);
        }

        // ---- Step 3: PreprocessScriptAsync ----
        scriptCode = await PreprocessScriptAsync(scriptCode, plugExecutionRequest);

        CLog.Information($"脚本预处理完成，长度: {scriptCode.Length}");

        // ---- Step 4: ExecuteScriptAsync ----
        var result = await ExecuteScriptAsync(scriptCode, plugExecutionRequest);

        // ---- Step 5: WriteResultToPDZ ----
        await WriteResultToPDZAsync(plugExecutionRequest, result);

        // ---- Step 6: ReportCompletedResult ----
        return await ReportCompletedResult(result ?? erd);
    }

    // ====== Virtual: GetScriptCodeAsync ======

    /// <summary>
    /// 读取脚本代码。
    /// 非 Standalone：从 PDZ 加载数据空间，读取指定变量。
    /// Standalone：从 InputVariables 中读取。
    /// </summary>
    protected virtual async Task<string?> GetScriptCodeAsync(PlugExecutionRequest request)
    {
        if (request.ExecuteMode != ExecuteMode.Standalone)
        {
            PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();
            PlugDataZone = await PDZApiClient.GetPDZByPDZIdAsync(
                request.ExecuteResultData!.Ids!.PDZId);
            if (PlugDataZone == null)
            {
                CLog.Error($"未找到数据空间：{request.ExecuteResultData.Ids!.PDZId}");
                return null;
            }

            return GetPDZVariableValue(PlugDataZone,
                request.ExecuteResultData.Ids.PlugDefinitionId, ScriptVariableName);
        }
        else
        {
            return request.InputVariables?
                .FirstOrDefault(v => v.Name == ScriptVariableName)?.Value;
        }
    }

    // ====== Virtual: PreprocessScriptAsync ======

    /// <summary>
    /// 脚本预处理钩子。在 ExecuteScriptAsync 之前调用。
    /// 默认实现直接返回原始脚本（no-op）。
    /// PythonPlug 可重写此方法实现参数通配符 ${参数名} 替换。
    /// </summary>
    protected virtual Task<string> PreprocessScriptAsync(
        string scriptCode, PlugExecutionRequest request)
    {
        return Task.FromResult(scriptCode);
    }

    // ====== Virtual: WriteResultToPDZAsync ======

    /// <summary>
    /// 将执行结果写回 PDZ。
    /// 默认实现将 ResultString 写入 PDZ 变量。
    /// </summary>
    protected virtual async Task WriteResultToPDZAsync(
        PlugExecutionRequest request, ExecuteResultData? result)
    {
        if (result == null || PlugDataZone == null)
            return;

        var resultString = result.ResultString;
        var plugDefinitionId = request.ExecuteResultData?.Ids?.PlugDefinitionId;
        if (string.IsNullOrEmpty(plugDefinitionId))
            return;

        PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();

        PlugDataZone.SetVariableValue(plugDefinitionId, ResultVariableName, resultString);
        await PDZApiClient.CreateOrUpdatePDZ(PlugDataZone);

        CLog.Information($"执行结果已写入 PDZ 变量 {ResultVariableName}");
    }

    /// <summary>
    /// 结果写回 PDZ 时使用的变量名。默认 "ResultString"。
    /// 子类可重写以使用自定义名称（如 InitVariableNames.ResultString.ToString()）。
    /// </summary>
    protected virtual string ResultVariableName => "ResultString";

    // ====== Static Utility: SubstituteParameters ======

    /// <summary>
    /// 将脚本中的参数通配符 ${参数名} 替换为实际参数值。
    /// </summary>
    public static string SubstituteParameters(string script, Func<string, string?> variableLookup)
    {
        if (string.IsNullOrEmpty(script) || variableLookup == null)
            return script;

        var pattern = @"\$\{([^}]+)\}";
        return Regex.Replace(script, pattern, match =>
        {
            var paramName = match.Groups[1].Value;
            var value = variableLookup(paramName);
            if (!string.IsNullOrEmpty(value))
            {
                CLog.Information($"参数替换: ${{{paramName}}} → {value}");
                return value;
            }
            CLog.Warning($"未找到参数 {paramName} 的值，保留原始通配符");
            return match.Value;
        });
    }

    /// <summary>
    /// 将脚本中的参数通配符 ${参数名} 替换为实际参数值（基于变量列表）。
    /// </summary>
    public static string SubstituteParameters(
        string script, IEnumerable<PlugVariableData>? variables)
    {
        if (variables == null)
            return SubstituteParameters(script, (Func<string, string?>)null!);
        return SubstituteParameters(script, paramName =>
        {
            var variable = variables.FirstOrDefault(v =>
                string.Equals(v.Name, paramName, StringComparison.OrdinalIgnoreCase));
            if (variable != null && !string.IsNullOrEmpty(variable.Value))
                return variable.Value;
            if (variable != null && !string.IsNullOrEmpty(variable.DefaultValue))
                return variable.DefaultValue;
            return null;
        });
    }

    // ====== Helper: GetPDZVariableValue ======

    /// <summary>
    /// 从 PDZ 获取变量值。简单封装，子类可重写以支持 action 变量路径。
    /// </summary>
    protected static string? GetPDZVariableValue(
        PlugDataZone pdz, string plugDefinitionId, string variableName)
    {
        try
        {
            return pdz?.GetVariableValue(plugDefinitionId, variableName);
        }
        catch (Exception ex)
        {
            CLog.Error($"获取变量 {variableName} 失败: {ex.Message}");
            return null;
        }
    }
}
