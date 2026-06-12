using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

/// <summary>
/// PlugExecutionEngine 拆分 - 无插头ID 路径（partial）。
/// 涵盖独立执行模式（Standalone / 非 Standalone）的两条分支。
/// 
/// 说明：Standalone 分支已正确委托给 handler.PlugCommonExecute()。
/// handler 内部负责变量解析（如 CMDPlugCommonExecuteService.SubmitStandalone 使用
/// VariableResolver.ResolveStandalone() 统一从 InputVariables 解析参数）。
/// 本文件无需额外改动。
/// </summary>
internal partial class PlugExecutionEngine
{
    /// <summary>
    /// 无插头ID 路径：通过 PlugTypeKey 获取源插头，按 ExecuteMode 走独立执行模式。
    /// </summary>
    private async Task<ExecuteResultData?> ExecuteNoPlugIdPathAsync(StartExecutePlugContext ctx)
    {
        var request = ctx.Request!;
        var PlugTypeKey = request.PlugTypeKey;

        var plug = await _plugManageService.GetPlugByTypeName(PlugTypeKey);
        if (plug == null)
        {
            CLog.Error($"PlugData with the specified type {PlugTypeKey} not found.");
            var erd = new ExecuteResultData()
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData.Ids
            };
            await ReportExecuteResult(erd);
            return erd;
        }
        //如果是独立执行模式，则直接等待执行完成并获取插头的执行结果数据
        //现在处理方法还未做区分，后续梳理需求后再更新
        if (request.ExecuteMode == ExecuteMode.Standalone)
        {
            CLog.Information("---插头独立执行模式---");
            // Standalone 路径：委托 handler 执行，handler 内部使用 VariableResolver.ResolveStandalone() 解析参数。
            // 例如 CMDPlugCommonExecuteService.SubmitStandalone 从 InputVariables 读取 command/arguments 等参数。
            var plugTypeKey = PlugTypeKey;
            var handler = _plugExecuteHandlerService.GetExecuteHandler(plugTypeKey);
            if (handler == null)
            {
                var category = plug?.Category;
                handler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
            }
            if (handler == null)
            {
                return await BuildAndReportErrorAsync(
                    $"未找到匹配的 ExecuteHandler (PlugTypeKey={plugTypeKey}, Category={plug?.Category ?? "-"})",
                    request: request,
                    definitionId: plug?.DefinitionId,
                    plugName: plug?.Name);
            }
            return await handler.PlugCommonExecute(new(plug, request));
        }
        else
        {
            var plugTypeKey = PlugTypeKey;
            var handler = _plugExecuteHandlerService.GetExecuteHandler(plugTypeKey);
            if (handler == null)
            {
                var category = plug?.Category;
                handler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
            }
            if (handler == null)
            {
                return await BuildAndReportErrorAsync(
                    $"未找到匹配的 ExecuteHandler (PlugTypeKey={plugTypeKey}, Category={plug?.Category ?? "-"})",
                    request: request,
                    definitionId: plug?.DefinitionId,
                    plugName: plug?.Name);
            }
            return await handler.PlugCommonExecute(new(plug, request));
        }
    }
}
