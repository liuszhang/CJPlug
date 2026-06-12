using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CJ.Plug.PlugBaseCore.Services;

/// <summary>
/// 桌面类插头（需要分发到图站执行的插头）的通用执行基类。
/// 封装了提交 → 图站执行完成 两阶段状态机的通用逻辑：
/// 1. <see cref="JobSubStatus.提交"/> → 调用子类的 <see cref="SubmitAsync"/> 方法
/// 2. <see cref="JobSubStatus.图站执行完成"/> → 自动将 ResultString 写回 PDZ 并报告完成
///
/// 子类只需实现 <see cref="SubmitAsync"/> 方法，专注于插头特有的提交逻辑。
/// 如果需要在图站执行完成后做额外的后处理（如文件回写），可重写 <see cref="OnStationCompletedAsync"/>。
/// </summary>
public abstract class StationPlugExecuteService : BasePlugExecuteService
{
    /// <summary>
    /// 获取该插头绑定的工具名称，如 "CMD"、"Python"、"NX" 等
    /// </summary>
    protected abstract string ToolName { get; }

    /// <summary>
    /// 获取该插头绑定的工具版本，如 "1.0"、"3.12" 等
    /// </summary>
    protected abstract string ToolVersion { get; }

    /// <summary>
    /// 公开工具信息供 <see cref="PlugExecutionEngine"/> 从本地 DB 预解析 Tool 和 Station。
    /// </summary>
    public override (string? toolName, string? toolVersion) GetToolInfo() => (ToolName, ToolVersion);

    /// <summary>
    /// 获取用于从 PDZ 读取变量值的变量名数组，传递给 <see cref="BasePlugExecuteService.DataPrepare"/>。
    /// 通常为插头 InitVariableNames 枚举的所有值。
    /// 如果子类不需要 DataPrepare，可返回 null 或空数组。
    /// </summary>
    protected abstract string[]? DataPrepareVariableNames { get; }

    /// <summary>
    /// 获取 ResultString 写回 PDZ 时使用的变量名。
    /// 默认返回 "ResultString"，子类可重写以使用自定义名称（如 InitVariableNames.ResultString.ToString()）。
    /// </summary>
    protected virtual string ResultStringVariableName => "ResultString";

    // ---- 可选的依赖服务 ----

    /// <summary>
    /// 工具执行服务。子类可通过此属性调用 <see cref="IToolExecuteService.ExecuteToolAsync"/>。
    /// 在构造函数中从 DI 容器自动解析。
    /// </summary>
    protected IToolExecuteService? ToolExecuteService { get; }

    /// <summary>
    /// 构造函数。自动从 DI 容器解析 <see cref="IToolExecuteService"/>（可选）。
    /// </summary>
    protected StationPlugExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        ToolExecuteService = _serviceProvider.GetService<IToolExecuteService>();
    }

    // ========================= 模板方法：两阶段调度 =========================

    /// <summary>
    /// 插头通用执行入口。根据 <see cref="JobSubStatus"/> 分发到提交阶段或后处理阶段。
    /// </summary>
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        var plug = context.plugToExecute;
        var plugExecutionRequest = context.plugExecutionRequest;

        var resultData = plugExecutionRequest!.ExecuteResultData!;
        var status = resultData.ExecuteSubStatus;

        // ---- 阶段判断前的数据准备 ----
        if (DataPrepareVariableNames is { Length: > 0 })
        {
            if (!await DataPrepare(plugExecutionRequest, DataPrepareVariableNames))
                return await ReportErrorResult(resultData);
        }

        CLog.Information($"开始执行 {plug?.Name ?? ToolName} 插头，状态为 {status}", resultData.Ids?.PDZId);

        // ---- 阶段 1：提交至图站 ----
        if (status == JobSubStatus.提交 || string.IsNullOrEmpty(status?.ToString()))
        {
            var executionRequest = BuildExecutionRequest(plugExecutionRequest);
            resultData = await SubmitAsync(executionRequest);
            return await ExecuteResultReport(resultData!);
        }

        // ---- 阶段 2：图站执行完成，后处理 ----
        if (status == JobSubStatus.图站执行完成)
        {
            Log.Information("图站执行完成，执行数据后处理");
            try
            {
                await WriteResultStringToPDZ(plugExecutionRequest);

                // 允许子类执行额外的后处理（如文件回写）
                await OnStationCompletedAsync(plugExecutionRequest);

                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.已完成;
                return await ExecuteResultReport(resultData);
            }
            catch (Exception ex)
            {
                CLog.Error($"{plug?.Name ?? ToolName} 后处理执行失败：{ex.Message}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return await ExecuteResultReport(resultData);
            }
        }

        return await ExecuteResultReport(resultData);
    }

    // ========================= 子类必须实现 =========================

    /// <summary>
    /// 提交插头执行到图站。子类负责：
    /// 1. 从 PDZ 读取所需变量，构建 <see cref="PlugVariableData"/> 列表
    /// 2. 将变量添加到 <paramref name="executionRequest"/>.InputVariables
    /// 3. 调用 <see cref="ToolExecuteService"/>.<see cref="IToolExecuteService.ExecuteToolAsync"/> 提交
    /// 4. 返回执行结果
    /// </summary>
    /// <param name="executionRequest">
    /// 预构建的执行请求，已设置 ToolName、ToolVersion、ExecuteResultData.Ids。
    /// 子类需填充 InputVariables 后提交。
    /// </param>
    protected abstract Task<ExecuteResultData?> SubmitAsync(PlugExecutionRequest executionRequest);

    // ========================= 子类可选重写 =========================

    /// <summary>
    /// 图站执行完成后的额外后处理钩子。
    /// 在 ResultString 已写回 PDZ 之后、状态标记为"已完成"之前调用。
    /// 子类可在此处执行文件回写、额外变量更新等操作。
    /// 默认实现为空（无操作）。
    /// </summary>
    protected virtual Task OnStationCompletedAsync(PlugExecutionRequest plugExecutionRequest)
    {
        return Task.CompletedTask;
    }

    // ========================= 通用工具方法 =========================

    /// <summary>
    /// 构建预置的执行请求，设置 ToolName、ToolVersion 和 Ids。
    /// </summary>
    protected PlugExecutionRequest BuildExecutionRequest(PlugExecutionRequest source)
    {
        return new PlugExecutionRequest
        {
            ToolName = ToolName,
            ToolVersion = ToolVersion,
            ExecuteMode = source.ExecuteMode,
            // 复制 InputVariables，确保 MCP Standalone 等路径中 command 参数等输入数据不丢失
            InputVariables = source.InputVariables ?? new(),
            // 复制其他在提交阶段可能需要的字段
            RequestCommand = source.RequestCommand,
            SpecifiedStationIp = source.SpecifiedStationIp,
            StationApiPort = source.StationApiPort,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = source.ExecuteResultData?.Ids,
                ExecuteStatus = JobStatus.执行中,
                ExecuteSubStatus = JobSubStatus.提交
            }
        };
    }

    /// <summary>
    /// 从 <see cref="ExecuteTaskPlugIds"/> 解析 identityId。
    /// 格式 "plugDefId|actionIdentityId" → 返回 actionIdentityId；
    /// 不含 "|" → 返回 null（普通插头执行）。
    /// </summary>
    protected static string? ParseIdentityId(PlugExecutionRequest? request)
    {
        var executeId = request?.ExecuteResultData?.Ids?.ExecuteTaskPlugIds?.FirstOrDefault();
        if (string.IsNullOrEmpty(executeId)) return null;
        return executeId.Contains('|') ? executeId.Split('|')[1] : null;
    }

    /// <summary>
    /// 将 ResultString 写回 PDZ。自动处理普通变量和动作变量两种路径。
    /// </summary>
    protected virtual async Task WriteResultStringToPDZ(PlugExecutionRequest plugExecutionRequest)
    {
        PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();

        var pdz = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData!.Ids!.PDZId);
        if (pdz == null)
        {
            CLog.Error($"未找到数据空间：{plugExecutionRequest.ExecuteResultData.Ids.PDZId}");
            return;
        }

        var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
        var plugDefinitionId = plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId;
        var identityId = ParseIdentityId(plugExecutionRequest);
        var variableName = ResultStringVariableName;

        if (identityId == null)
        {
            pdz.SetVariableValue(plugDefinitionId, variableName, resultString);
        }
        else
        {
            pdz.SetActionVariableValue(plugDefinitionId, identityId, variableName, resultString);
        }

        await PDZApiClient.CreateOrUpdatePDZ(pdz);
        StatusReporter.PDZUpdated(pdz.PDZId);
    }

    /// <summary>
    /// 从 PDZ 获取变量值，自动处理普通变量和动作变量两种路径。
    /// </summary>
    /// <param name="pdz">数据空间</param>
    /// <param name="plugDefinitionId">插头定义 ID</param>
    /// <param name="variableName">变量名</param>
    /// <param name="identityId">动作 identityId，null 表示普通插头执行</param>
    protected static string? GetPDZVariableValue(
        PlugDataZone pdz, string plugDefinitionId, string variableName, string? identityId = null)
    {
        if (identityId == null)
        {
            return VariableResolver.ResolveFromPDZ(variableName, pdz, plugDefinitionId);
        }
        else
        {
            var actionValue = pdz.ActionVariableDatas?
                .Where(p => p.ActionIdentityId == identityId)
                .FirstOrDefault(p => p.Name == variableName)?.Value;
            if (actionValue != null)
                return actionValue;
            // Action 路径也回退到 PDZ 解析链（含 Plug 层 Value 回退）
            return VariableResolver.ResolveFromPDZ(variableName, pdz, plugDefinitionId);
        }
    }
}
