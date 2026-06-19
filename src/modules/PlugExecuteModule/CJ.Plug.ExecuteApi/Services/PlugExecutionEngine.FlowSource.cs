using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.SharedPages.Services;
using Serilog;
using System.Text.Json;

/// <summary>
/// PlugExecutionEngine 拆分 - 有插头ID / 流程图来源 路径（partial）。
/// 涵盖：
///   1) MCP Workflow 路径 (line 302-305)
///   2) 流程图入口路径 (ExecuteTaskPlugIds.Count == 0, 有 PlugDefinitionId)
///   3) 动作执行路径 (ExecuteTaskPlugIds.Count > 0)
/// 包含已有的 4 个守卫点（plug2==null / 流程图 handler==null / plug==null / 动作 handler==null）。
/// </summary>
internal partial class PlugExecutionEngine
{
    /// <summary>
    /// 有插头ID 路径：MCP Workflow 分支 + 流程图入口分支 + 动作执行分支的 dispatcher。
    /// </summary>
    private async Task<ExecuteResultData?> ExecuteFlowSourcePathAsync(StartExecutePlugContext ctx)
    {
        var request = ctx.Request!;

        // 有插头ID，说明是来源于引擎或者流程图，先通过ID获取类型,再通过类型获取源插头
        ctx.PlugData = await _plugDataService.GetByPlugDefinitionIdAsync(request.ExecuteResultData.Ids.PlugDefinitionId);
        var definitionId = request.ExecuteResultData.Ids.PlugDefinitionId;

        // === MCP 路径优先处理：PlugData 可能不存在（直接发布的插头没有预存的 PlugData） ===

        // MCP 调用且为工作流类型：走 Use PDZ → Job PDZ → Elsa 流程图执行
        if (request.McpToolType == "Workflow" && ctx.PlugData != null)
        {
            return await _mcpWorkflowRunner.ExecuteMcpWorkflowFromRequest(request, ctx.PlugData);
        }

        // 容错：当 McpToolType 为 "Workflow" 但 PlugData 不存在时，检查该插头是否为独立插头
        // 部分独立插头（如接口类）发布为 MCP Tool 时 ToolType 可能被误设为 "Workflow"，
        // 此时应自动重定向到 Plugin 路径执行
        if (request.McpToolType == "Workflow" && ctx.PlugData == null)
        {
            var plug = await _plugManageService.GetPlugByDefinitionId(definitionId);
            if (plug != null)
            {
                var flowchart = plug.GetFlowchartJson();
                if (flowchart == null)
                {
                    CLog.Information($"[TRACE-MCP] 检测到无流程图的独立插头 ({plug.Category}/{plug.PlugTypeKey ?? "-"})，重定向到 Plugin 路径: {definitionId}");
                    request.McpToolType = "Plugin";
                    request.ExecuteMode = ExecuteMode.Standalone;
                    return await ExecuteMcpPluginPathAsync(ctx, definitionId);
                }
            }
        }

        // 注：MCP Plugin 路径（McP==Plugin && Standalone）由 ExecuteMcpPluginPathAsync 处理，
        // 必须在 PlugData==null 防御之前拦截。该路径是单独的 partial 方法，由 caller 在
        // 主入口处根据 McpToolType 预先 dispatch 到此方法。
        // 为保持 100% 等价行为：MCP Plugin 路径检测放在此方法中，并显式调用 Mcp 分支。

        if (request.McpToolType == "Plugin" && request.ExecuteMode == ExecuteMode.Standalone)
        {
            // 路由到 MCP Plugin 路径（partial 方法在 McpPath.cs）
            // 此处是新增 dispatch 调用，原逻辑中此分支与后续 plugData==null 检查都在
            // 同一个 if-else 链中，我们将其抽出到独立 partial 方法以降低单文件复杂度。
            return await ExecuteMcpPluginPathAsync(ctx, definitionId);
        }

        // 非 MCP 路径：plugData 为 null 时尝试从 Plug 定义构建临时 PlugData 并走 Plugin 路径执行
        // 直接启动插头（如从插头管理中点击运行按钮）没有预存的 PlugData 记录，
        // 此处仿照 MCP Plugin 路径做自动 fallback，避免"未找到 PlugData"错误。
        if (ctx.PlugData == null)
        {
            CLog.Information($"[TRACE] PlugData 不存在 (non-MCP)，尝试从 Plug 定义构建临时 PlugData: {definitionId}");
            var plug = await _plugManageService.GetPlugByDefinitionId(definitionId);
            if (plug != null)
            {
                CLog.Information($"[TRACE] 找到 Plug 定义 ({plug.Category}/{plug.PlugTypeKey ?? "-"})，重定向到 Plugin 路径: {definitionId}");
                request.McpToolType = "Plugin";
                request.ExecuteMode = ExecuteMode.Standalone;
                return await ExecuteMcpPluginPathAsync(ctx, definitionId);
            }
            CLog.Error($"StartExecutePlug: 未找到 PlugData 且未找到 Plug 定义，PlugDefinitionId={definitionId}");
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData.Ids
            };
        }

        // 区分流程图入口 vs 动作执行：
        // 流程图入口：ExecuteTaskPlugIds.Count == 0（首次执行请求，从主入口 if 进来）
        // 动作执行：ExecuteTaskPlugIds.Count > 0（已 enqueue 待执行任务）
        if (request.ExecuteResultData.Ids.ExecuteTaskPlugIds.Count == 0)
        {
            return await ExecuteFlowChartEntryAsync(ctx);
        }
        else
        {
            return await ExecuteActionEntryAsync(ctx);
        }
    }

    /// <summary>
    /// 流程图入口路径：ExecuteTaskPlugIds.Count == 0, 有 PlugDefinitionId。
    /// 含守卫点 3（plug2==null）与守卫点 4（流程图 handler==null）。
    /// </summary>
    private async Task<ExecuteResultData?> ExecuteFlowChartEntryAsync(StartExecutePlugContext ctx)
    {
        var request = ctx.Request!;
        var plugData = ctx.PlugData!;

        //Log.Information($"准备启动插头{plug.Name}");
        request.ExecuteResultData = request.ExecuteResultData ?? new ExecuteResultData();
        var PDZ = await _pdzManageService.GetByPDZId(request.ExecuteResultData.Ids.PDZId);
        var PlugActionDatas = PDZ?.GetActionDatasOfPlug(request.ExecuteResultData.Ids.PlugDefinitionId) ?? new();
        if (!plugData.OnlyExecuteAction)
        {
            request.ExecuteResultData.Ids.ExecuteTaskPlugIds.Add(plugData.PlugDefinitionId);
        }
        foreach (var a in PlugActionDatas)
        {
            request.ExecuteResultData.Ids.ExecuteTaskPlugIds.Add(a.ActionPlugRootDefinitionId + "|" + a.ActionIdentityId);
        }
        StatusReporter.ReportPlugStatus(plugData.PlugDefinitionId, new PlugStatus() { Blocked = true }, PDZ?.PDZId);
        //Log.Information($"准备执行插头/动作{plug.Name}，待执行的动作列表：{JsonSerializer.Serialize(request.ExecuteResultData.Ids.ExecuteTaskPlugIds)}");

        // 查找源 Plug 定义：先按 DefinitionId 精确查找，若不存在（流程图实例场景）则通过 PlugTypeKey 回退到源模板
        var plug2 = await _plugManageService.GetPlugByDefinitionId(plugData.PlugDefinitionId);
        if (plug2 == null && !string.IsNullOrEmpty(plugData.PlugTypeKey))
        {
            plug2 = await _plugManageService.GetPlugByTypeName(plugData.PlugTypeKey);
        }

        // PlugTypeKey 为空时，尝试按名称兜底查找
        if (plug2 == null && string.IsNullOrEmpty(plugData.PlugTypeKey) && !string.IsNullOrEmpty(plugData.Name))
        {
            plug2 = await _plugManageService.GetPlugByNameAsync(plugData.Name);
        }

        // 注入 PlugTypeKey 到 request，供下游 handler 回退使用
        request.PlugTypeKey = plug2?.PlugTypeKey ?? plugData.PlugTypeKey;

        // 防御：plug2 仍为 null 时，尝试 Category 回退处理器 → 根插头回退
        if (plug2 == null)
        {
            var category = plugData.Category;

            if (!string.IsNullOrEmpty(category))
            {
                var categoryHandler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
                if (categoryHandler != null)
                {
                    CLog.Information($"[回退] PlugDefinition 未找到，使用 Category={category} 回退处理器执行 {plugData.Name}");
                    var syntheticPlug = new Plug
                    {
                        DefinitionId = plugData.PlugDefinitionId,
                        Name = plugData.Name,
                        PlugTypeKey = plugData.PlugTypeKey,
                        Category = category,
                    };
                    return await categoryHandler.PlugCommonExecute(new(syntheticPlug, request));
                }
            }

            // Category 回退失败，尝试通过 PlugTypeKey 查找根插头
            if (!string.IsNullOrEmpty(plugData.PlugTypeKey))
            {
                var rootPlug = await _plugManageService.GetPlugByTypeName(plugData.PlugTypeKey);
                if (rootPlug != null)
                {
                    var rootCategory = rootPlug.Category;
                    if (!string.IsNullOrEmpty(rootCategory))
                    {
                        var categoryHandler = _plugExecuteHandlerService.GetCategoryFallbackHandler(rootCategory);
                        if (categoryHandler != null)
                        {
                            CLog.Information($"[回退] Category 回退失败，通过根插头回退 (PlugTypeKey={plugData.PlugTypeKey}, Category={rootCategory}) 执行 {plugData.Name}");
                            return await categoryHandler.PlugCommonExecute(new(rootPlug, request));
                        }
                    }
                }
            }

            // 兜底：Category 未知且 PlugTypeKey 为空（接口类插头旧数据特征），默认使用"接口类"处理器
            if (string.IsNullOrEmpty(plugData.PlugTypeKey))
            {
                CLog.Information($"[回退] Category 未知且 PlugTypeKey 为空，默认使用 Category=接口类 回退处理器执行 {plugData.Name}");
                var categoryHandler = _plugExecuteHandlerService.GetCategoryFallbackHandler("接口类");
                if (categoryHandler != null)
                {
                    var syntheticPlug = new Plug
                    {
                        DefinitionId = plugData.PlugDefinitionId,
                        Name = plugData.Name,
                        PlugTypeKey = plugData.PlugTypeKey,
                        Category = "接口类",
                    };
                    return await categoryHandler.PlugCommonExecute(new(syntheticPlug, request));
                }
            }

            return await BuildAndReportErrorAsync(
                $"未找到 Plug 定义，PlugDefinitionId={plugData.PlugDefinitionId}, PlugTypeKey={plugData.PlugTypeKey}",
                request: request,
                definitionId: plugData.PlugDefinitionId,
                plugName: plugData.Name);
        }

        var plugTypeKey2 = plug2.PlugTypeKey ?? plugData.PlugTypeKey;
        var handler = _plugExecuteHandlerService.GetExecuteHandler(plugTypeKey2);
        if (handler == null)
        {
            var category = plug2.Category ?? plugData.Category;
            handler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
        }
        // 根插头回退：PlugTypeKey / Category 均未匹配时，通过 plugData.PlugTypeKey 重新查找根插头
        if (handler == null && !string.IsNullOrEmpty(plugData.PlugTypeKey) && plugData.PlugTypeKey != plug2.PlugTypeKey)
        {
            var rootPlug = await _plugManageService.GetPlugByTypeName(plugData.PlugTypeKey);
            if (rootPlug != null)
            {
                CLog.Information($"[回退] ExecuteHandler 和 Category 均未匹配 {plug2.Name}，通过根插头回退：PlugTypeKey={plugData.PlugTypeKey}");
                handler = _plugExecuteHandlerService.GetExecuteHandler(rootPlug.PlugTypeKey)
                    ?? _plugExecuteHandlerService.GetCategoryFallbackHandler(rootPlug.Category);
            }
        }
        if (handler == null)
        {
            return await BuildAndReportErrorAsync(
                $"未找到匹配的 ExecuteHandler (PlugTypeKey={plugTypeKey2}, Category={plug2.Category ?? plugData.Category ?? "-"})",
                request: request,
                definitionId: plug2.DefinitionId,
                plugName: plug2.Name);
        }
        return await handler.PlugCommonExecute(new(plug2, request));
    }

    /// <summary>
    /// 动作执行路径：ExecuteTaskPlugIds.Count > 0。
    /// 含守卫点 5（plug==null）与守卫点 6（动作 handler==null）。
    /// </summary>
    private async Task<ExecuteResultData?> ExecuteActionEntryAsync(StartExecutePlugContext ctx)
    {
        var request = ctx.Request!;

        //插头动作执行
        ctx.PlugData = await _plugDataService.GetByPlugDefinitionIdAsync(request?.ExecuteResultData.Ids.ExecuteTaskPlugIds[0].Split("|")[0]);
        if (ctx.PlugData == null)
        {
            CLog.Error($"2PlugData with the specified definition ID {request?.ExecuteResultData.Ids.PlugDefinitionId} not found.");
            var erd = new ExecuteResultData()
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request?.ExecuteResultData.Ids
            };
            await ReportExecuteResult(erd);
            return erd;
        }
        var plugData = ctx.PlugData;
        CLog.Information($"准备执行插头/动作{plugData.Name}");
        //StatusReporter.ReportPlugStatus(plug.DefinitionId, new PlugStatus() { Blocked = true });

        // 查找源 Plug 定义：先按 DefinitionId 精确查找，若不存在（流程图实例场景）则通过 PlugTypeKey 回退到源模板
        var plug = await _plugManageService.GetPlugByDefinitionId(plugData.PlugDefinitionId);
        if (plug == null && !string.IsNullOrEmpty(plugData.PlugTypeKey))
        {
            plug = await _plugManageService.GetPlugByTypeName(plugData.PlugTypeKey);
        }

        // PlugTypeKey 为空时，尝试按名称兜底查找
        if (plug == null && string.IsNullOrEmpty(plugData.PlugTypeKey) && !string.IsNullOrEmpty(plugData.Name))
        {
            plug = await _plugManageService.GetPlugByNameAsync(plugData.Name);
        }

        // 注入 PlugTypeKey 到 request，供下游 handler 回退使用
        request.PlugTypeKey = plug?.PlugTypeKey ?? plugData.PlugTypeKey;

        // 防御：plug 仍为 null 时，尝试 Category 回退处理器（eg. 接口类插头 PlugTypeKey 为空）
        if (plug == null)
        {
            var category = plugData.Category;
            if (!string.IsNullOrEmpty(category))
            {
                var categoryHandler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
                if (categoryHandler != null)
                {
                    CLog.Information($"[回退] PlugDefinition 未找到，使用 Category={category} 回退处理器执行 {plugData.Name}");
                    var syntheticPlug = new Plug
                    {
                        DefinitionId = plugData.PlugDefinitionId,
                        Name = plugData.Name,
                        PlugTypeKey = plugData.PlugTypeKey,
                        Category = category,
                    };
                    return await categoryHandler.PlugCommonExecute(new(syntheticPlug, request));
                }
            }

            return await BuildAndReportErrorAsync(
                $"未找到 Plug 定义，PlugDefinitionId={plugData.PlugDefinitionId}, PlugTypeKey={plugData.PlugTypeKey}",
                request: request,
                definitionId: plugData.PlugDefinitionId,
                plugName: plugData.Name);
        }

        var plugTypeKey = plug.PlugTypeKey ?? plugData.PlugTypeKey;
        var handler = _plugExecuteHandlerService.GetExecuteHandler(plugTypeKey);
        if (handler == null)
        {
            var category = plug.Category ?? plugData.Category;
            handler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
        }
        if (handler == null)
        {
            return await BuildAndReportErrorAsync(
                $"未找到匹配的 ExecuteHandler (PlugTypeKey={plugTypeKey}, Category={plug.Category ?? plugData.Category ?? "-"})",
                request: request,
                definitionId: plug.DefinitionId,
                plugName: plug.Name);
        }
        return await handler.PlugCommonExecute(new(plug, request));
        //return null;
    }
}
