using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
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
