using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PlugExecutionEngine 拆分 - MCP Plugin 路径（partial）。
/// 涵盖 McpToolType == "Plugin" && ExecuteMode == Standalone 的全部子分支。
/// 包含 PlugData 构建、变量注入、handler 预设、Tool/Station 预解析逻辑。
/// 
/// 重构说明：已移除临时 PDZ 创建/持久化/清理逻辑。
/// 变量解析由各 handler 内部的 VariableResolver.ResolveStandalone() 统一处理，
/// Standalone 模式下不再依赖 PDZ 数据空间。
/// </summary>
internal partial class PlugExecutionEngine
{
    /// <summary>
    /// MCP Plugin 路径：Standalone 模式 + McpToolType == "Plugin"。
    /// 重构后：plug.SetVariableValue() 同步输入 → handler 内部 VariableResolver.ResolveStandalone() 解析参数 → 执行。
    /// </summary>
    /// <param name="ctx">StartExecutePlug 共享上下文</param>
    /// <param name="definitionId">已从 request.ExecuteResultData.Ids.PlugDefinitionId 解析出的 ID</param>
    private async Task<ExecuteResultData?> ExecuteMcpPluginPathAsync(StartExecutePlugContext ctx, string definitionId)
    {
        var request = ctx.Request!;
        var plugData = ctx.PlugData;

        var plug = await _plugManageService.GetPlugByDefinitionId(definitionId);
        if (plug == null)
        {
            CLog.Error($"StartExecutePlug: MCP Plugin 未找到插头 {definitionId}");
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData?.Ids,
            };
        }

        // 直接从插头管理发布的 MCP TOOL 没有 PlugData 记录，从 Plug 定义构建一个临时的
        if (plugData == null)
        {
            plugData = new PlugData
            {
                PlugDefinitionId = plug.DefinitionId,
                Name = plug.Name,
                PlugTypeKey = plug.PlugTypeKey,
                Category = plug.Category,
                OnlyExecuteAction = plug.OnlyExecuteAction,
            };
            CLog.Information($"[TRACE-MCP] PlugData 不存在，从 Plug 定义构建临时 PlugData");
        }
        // 同步到 ctx，供后续 partial 方法（如有）引用
        ctx.PlugData = plugData;

        // 直接将 MCP 输入参数设置到插头变量，供 handler 内部 VariableResolver.ResolveStandalone() 回退使用
        CLog.Information($"[TRACE-MCP] InputVariables 数量: {request.InputVariables?.Count ?? 0}");
        foreach (var v in request.InputVariables ?? new())
            CLog.Information($"[TRACE-MCP] InputVariable: Name={v.Name}, Value={v.Value}, Type={v.Type}");
        foreach (var v in request.InputVariables)
        {
            plug.SetVariableValue(v.Name, v.Value);
        }

        // 将插头定义中的默认变量值注入到 InputVariables（如果调用方未提供）。
        // 直接运行 / Standalone 模式中 InputVariables 可能为空，此时应使用插头配置的默认参数值。
        foreach (var pv in plug.PlugVariables)
        {
            if (request.InputVariables.Any(iv => iv.Name == pv.Name)) continue;
            if (string.IsNullOrEmpty(pv.Value)) continue;
            CLog.Information($"[TRACE-MCP] 使用插头默认变量: {pv.Name}={pv.Value}");
            request.InputVariables.Add(new PlugVariableData
            {
                Name = pv.Name,
                Value = pv.Value,
                Type = pv.Type
            });
        }

        CLog.Information($"[TRACE-MCP] 已设置插头变量，总变量数: {plug.PlugVariables?.Count ?? 0}, InputVariables 数: {request.InputVariables?.Count ?? 0}");

        // 将插头类型信息注入到 request，确保下游 handler 能正确使用
        request.PlugTypeKey = plug.PlugTypeKey;

        CLog.Information($"[TRACE-MCP] handler调用前 — PlugTypeKey={plug.PlugTypeKey}");
        foreach (var v in request.InputVariables ?? new())
            CLog.Information($"[TRACE-MCP] request.InputVariables: {v.Name}={v.Value}");
        var plugTypeKey = plug?.PlugTypeKey;
        var handler = _plugExecuteHandlerService.GetExecuteHandler(plugTypeKey);
        if (handler == null)
        {
            var category = plug?.Category;
            handler = _plugExecuteHandlerService.GetCategoryFallbackHandler(category);
        }
        if (handler == null)
        {
            return await BuildAndReportErrorAsync(
                $"MCP Plugin: 未找到匹配的 ExecuteHandler (PlugTypeKey={plugTypeKey}, Category={plug?.Category ?? "-"})",
                request: request,
                definitionId: definitionId,
                plugName: plug.Name);
        }
        // 预设最小 PDZ 存根，避免 handler 的 DataPrepare() 阶段因 PDZ 缺失而失败。
        // Standalone 模式下变量解析由 handler 内部的 VariableResolver.ResolveStandalone() 从 InputVariables 完成，不依赖 PDZ。
        if (handler is BasePlugExecuteService baseHandler)
        {
            baseHandler.PlugDataZone = new PlugDataZone();
        }
        // 从本地 DB 预解析 Tool 和 Station，避免 ToolExecuteService 内部通过 HTTP 调用
        // （StationAndToolApiClient 同样指向 DispatchServer，但 Tool/Station API 仅在 ApiServer 上存在）
        if (handler is StationPlugExecuteService stationHandler)
        {
            var (toolName, toolVersion) = stationHandler.GetToolInfo();
            if (!string.IsNullOrEmpty(toolName) && !string.IsNullOrEmpty(toolVersion))
            {
                var displayName = $"{toolName}({toolVersion})";
                CLog.Information($"[TRACE-MCP] 从本地 DB 解析工具: {displayName}");
                request.ResolvedTool = await _dbContext.Set<Tool>()
                    .FirstOrDefaultAsync(t => t.ToolName == toolName && t.ToolVersion == toolVersion);
                if (request.ResolvedTool != null)
                {
                    CLog.Information($"[TRACE-MCP] 工具已找到, ToolPath={request.ResolvedTool.ToolPath}");
                    // 同时解析可用的图站
                    request.ResolvedStation = await _dbContext.Set<Station>()
                        .FirstOrDefaultAsync(s => s.IsStarted == true);
                    if (request.ResolvedStation != null)
                    {
                        CLog.Information($"[TRACE-MCP] 图站已找到, StationIp={request.ResolvedStation.StationIp}");
                        request.SpecifiedStationIp = request.ResolvedStation.StationIp;
                    }
                    else
                    {
                        CLog.Warning($"[TRACE-MCP] 未找到在线图站");
                    }
                }
                else
                {
                    CLog.Error($"[TRACE-MCP] 未在本地 DB 中找到工具: {displayName}");
                }
            }
        }
        // 通用兜底（DefaultPlugExecuteService 等非 StationPlugExecuteService handler）：
        // 通过 ToolId 预解析 Tool 和 Station，替代旧版上传文件夹合成 Tool 逻辑
        else if (plug.ToolId.HasValue && request.ResolvedTool == null && request.ResolvedStation == null)
        {
            request.ResolvedTool = await _dbContext.Set<Tool>()
                .FirstOrDefaultAsync(t => t.Id == plug.ToolId.Value);
            if (request.ResolvedTool != null)
            {
                request.ToolName = request.ResolvedTool.ToolName;
                request.ToolVersion = request.ResolvedTool.ToolVersion;
                CLog.Information($"[TRACE-MCP] 通过 ToolId={plug.ToolId} 解析工具: {request.ResolvedTool.ToolName}({request.ResolvedTool.ToolVersion}), ToolPath={request.ResolvedTool.ToolPath}");

                request.ResolvedStation = await _dbContext.Set<Station>()
                    .FirstOrDefaultAsync(s => s.IsStarted == true);
                if (request.ResolvedStation != null)
                {
                    request.SpecifiedStationIp = request.ResolvedStation.StationIp;
                    CLog.Information($"[TRACE-MCP] 图站已找到, StationIp={request.ResolvedStation.StationIp}");
                }
            }
            else
            {
                CLog.Error($"[TRACE-MCP] 未在本地 DB 中找到 Tool (Id={plug.ToolId})");
            }
        }
        // 兼容旧数据：无 ToolId 的上传文件夹工具（uploaded:// 格式），保留原始合成逻辑
        else if (!plug.ToolId.HasValue
                 && plug.ToolVersionPath?.StartsWith("uploaded://") == true
                 && request.ResolvedTool == null)
        {
            var pathPart = plug.ToolVersionPath["uploaded://".Length..];
            var partSlash = pathPart.Split('/');
            string uploadedUserOrSystem, uploadedFolderName;
            if (partSlash.Length >= 2)
            {
                uploadedUserOrSystem = partSlash[0];
                uploadedFolderName = partSlash[1];
            }
            else
            {
                uploadedUserOrSystem = string.Equals(plug.CreateType, PlugCreateTypeEnum.SystemInitPlug.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? "0System" : (plug.Creater ?? "unknown");
                uploadedFolderName = plug.ToolName ?? partSlash[0];
            }
            var entryFile = plug.GetPlugSetting("UploadedEntryFile");
            if (string.IsNullOrEmpty(entryFile))
            {
                var entryMatch = System.Text.RegularExpressions.Regex.Match(
                    plug.ToolCommandLineShema ?? "", @"\[ToolPath\]\\([^\s\]]+)");
                entryFile = entryMatch.Success ? entryMatch.Groups[1].Value : "";
            }
            var uploadedToolBasePath = $"Tools/{uploadedUserOrSystem}/{uploadedFolderName}";
            var uploadedToolPath = string.IsNullOrEmpty(entryFile)
                ? uploadedToolBasePath
                : $"{uploadedToolBasePath}/{entryFile}";

            request.ResolvedTool = new Tool
            {
                ToolName = uploadedFolderName,
                ToolVersion = "1.0",
                ToolPath = uploadedToolPath,
                ToolBasePath = uploadedToolBasePath,
                CommandParameter = plug.ToolCommandLineShema,
                SkipDownloadToStation = false,
                IsBrowsable = false,
            };
            request.ToolName = uploadedFolderName;
            request.ToolVersion = "1.0";
            CLog.Information($"[TRACE-MCP-LEGACY] 旧格式上传工具合成 Tool: {uploadedFolderName}, BasePath={uploadedToolBasePath}");

            request.ResolvedStation = await _dbContext.Set<Station>()
                .FirstOrDefaultAsync(s => s.IsStarted == true);
            if (request.ResolvedStation != null)
            {
                request.SpecifiedStationIp = request.ResolvedStation.StationIp;
                CLog.Information($"[TRACE-MCP] 图站已找到, StationIp={request.ResolvedStation.StationIp}");
            }
        }
        return await handler?.PlugCommonExecute(new(plug, request));
    }
}
