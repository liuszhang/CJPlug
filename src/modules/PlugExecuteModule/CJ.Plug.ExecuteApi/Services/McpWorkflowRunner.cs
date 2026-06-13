using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.SharedPages.Services;

internal class McpWorkflowRunner
{
    private readonly IPDZManageService _pdzManageService;
    private readonly IPlugManageService _plugManageService;
    private readonly MainApiClient _mainApiClient;
    private readonly ElsaApiClient _elsaApiClient;

    public McpWorkflowRunner(
        IPDZManageService pdzManageService,
        IPlugManageService plugManageService,
        MainApiClient mainApiClient,
        ElsaApiClient elsaApiClient)
    {
        _pdzManageService = pdzManageService;
        _plugManageService = plugManageService;
        _mainApiClient = mainApiClient;
        _elsaApiClient = elsaApiClient;
    }

    /// <summary>
    /// MCP 工作流执行子路径：从 Use PDZ 创建 Job PDZ，填充参数后通过 Elsa 引擎执行流程图
    /// 由 StartExecutePlug 在检测到 McpToolType=="Workflow" 时调用
    /// </summary>
    public async Task<ExecuteResultData?> ExecuteMcpWorkflowFromRequest(PlugExecutionRequest request, PlugData plugData)
    {
        var definitionId = request.PlugDefinitionId;

        // 1. 查找 Use PDZ（参数模板）
        var usePDZId = "MCP_Use0_" + definitionId;
        var usePDZ = await _pdzManageService.GetByPDZId(usePDZId);
        if (usePDZ == null)
        {
            CLog.Error("ExecuteMcpWorkflowFromRequest: 未找到 Use PDZ，请先发布为 MCP Tool");
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData?.Ids ?? new ExecuteIdsBundle { PlugDefinitionId = definitionId },
            };
        }

        // 2. 获取流程图
        var plug = await _plugManageService.GetPlugByDefinitionId(definitionId);
        var flowchart = plug?.ToActivityJson();
        if (flowchart == null || flowchart.Count == 0)
        {
            CLog.Error("ExecuteMcpWorkflowFromRequest: 未找到流程图数据");
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData?.Ids ?? new ExecuteIdsBundle { PlugDefinitionId = definitionId },
            };
        }

        // 3. 从 Use PDZ 复制为 Job PDZ
        var currentUser = "MCP_Caller";
        (var jobPDZId, var jobDefinitionId) = await _mainApiClient.SubmitExecute(
            plugData, currentUser, usePDZ, PDZTypeEnum.Job3);

        if (string.IsNullOrEmpty(jobPDZId))
        {
            CLog.Error("ExecuteMcpWorkflowFromRequest: 创建 Job PDZ 失败");
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                Ids = request.ExecuteResultData?.Ids ?? new ExecuteIdsBundle { PlugDefinitionId = definitionId },
            };
        }

        // 4. 填充输入参数到 Job PDZ
        var jobPDZ = await _mainApiClient.GetPDZByPDZIdAsync(jobPDZId);
        if (jobPDZ != null && request.InputVariables?.Count > 0)
        {
            foreach (var v in request.InputVariables)
            {
                jobPDZ.SetVariableValue(definitionId, v.Name, v.Value, v.Type);
            }
            await _mainApiClient.CreateOrUpdatePDZ(jobPDZ);
        }

        // 4.5 P5: 将 File 类型输入变量预下载到图站，
        // 确保 Elsa 工作流中的工具执行步骤能找到文件本地副本。
        // 注：如果 Elsa 工作流的工具步骤自身也走 ToolExecuteService，会在执行时再次下载；
        // 此处提前下载可加速执行，并为不经过 ToolExecuteService 的 Activity 提供文件可用性保障。
        var hasFileVariables = request.InputVariables?.Any(
            v => v.Type == VariableTypeEnum.File.ToString() && v.IsInput == true) == true;
        if (hasFileVariables && jobPDZ != null)
        {
            try
            {
                Station? station = null;
                try
                {
                    station = await _mainApiClient.GetStationToUseByTool(
                        "GenericWorkflow", "1.0", null);
                }
                catch
                {
                    // GetStationToUseByTool 内部会调用 GetToolByDisplayNameAsync，
                    // 若工具表中无 GenericWorkflow 记录则可能失败，忽略此错误继续执行
                }

                if (station != null && !string.IsNullOrEmpty(station.StationIp))
                {
                    if (!station.StationIp.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        station.StationIp = "http://" + station.StationIp;

                    var stationClient = new StationApiClient(
                        new HttpClient { BaseAddress = new Uri(station.StationIp) });
                    var anyDownloaded = false;

                    foreach (var v in request.InputVariables!
                        .Where(v => v.Type == VariableTypeEnum.File.ToString() && v.IsInput == true))
                    {
                        if (string.IsNullOrEmpty(v.Value))
                            continue;

                        var localPath = await stationClient.DownloadFileByFileIdAsync(v);
                        if (!string.IsNullOrEmpty(localPath))
                        {
                            jobPDZ.SetVariableValue(definitionId, v.Name, localPath, v.Type);
                            CLog.Information(
                                $"[MCP-Workflow] 文件预下载成功: {v.Name} → {localPath} (图站: {station.StationIp})");
                            anyDownloaded = true;
                        }
                        else
                        {
                            CLog.Warning(
                                $"[MCP-Workflow] 文件预下载失败: {v.Name}, value={v.Value}, 图站={station.StationIp}. 工作流执行可能因找不到文件而失败。");
                        }
                    }

                    if (anyDownloaded)
                        await _mainApiClient.CreateOrUpdatePDZ(jobPDZ);
                }
                else
                {
                    CLog.Warning(
                        "[MCP-Workflow] 未找到可用图站，跳过文件预下载。工作流中的工具步骤将自行处理文件下载。");
                }
            }
            catch (Exception ex)
            {
                // 预下载失败不应阻止工作流提交，因为工具步骤可能自带下载逻辑
                CLog.Warning($"[MCP-Workflow] 文件预下载异常（工作流将继续执行）: {ex.Message}");
            }
        }

        // 5. 构造执行请求，走 Elsa 引擎执行流程图
        var executeSetting = new ExecuteSetting
        {
            CorrelationId = jobPDZId,
            TriggerActivityId = usePDZ.TriggerPlugDefinitionId,
        };

        var executeResult = await _elsaApiClient.ExecuteWorkflowWithExecuteSetting(flowchart, executeSetting);
        var workflowInstanceId = executeResult?.ExecuteResultMessage;

        return new ExecuteResultData
        {
            ExecuteStatus = JobStatus.执行中,
            ExecuteSubStatus = JobSubStatus.提交,
            ResultString = $"工作流执行成功，实例 ID: {workflowInstanceId}",
            Ids = new ExecuteIdsBundle
            {
                PlugDefinitionId = definitionId,
                PDZId = jobPDZId,
                JobCorrelationId = jobPDZId,
            },
        };
    }
}
