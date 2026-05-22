using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.SharedPages.Services;
using CJ.Plug.ElsaIntegration.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Serilog;
using System.Text.Json;
using System.Timers;

public class PlugExecuteService : IPlugExecuteService
{
    private readonly MainDbContext _dbContext;

    private IPlugManageService PlugManageService { get; set; }
    private IPlugCommonExecute PlugCommonExecute { get; set; }
    private IEnumerable<IPlugCommonExecute> PlugCommonExecutes {  get; set; }
    private IPlugExecuteHandlerService PlugExecuteHandlerService { get; set; }
    private IPDZManageService PDZManageService { get; set; }
    private IPlugDataService PlugDataService { get; set; }
    private MainApiClient MainApiClient { get; set; }
    private ElsaApiClient ElsaApiClient { get; set; }
    private IElsaEngineService ElsaEngineService { get; set; }

    public PlugExecuteService(
        MainDbContext dbContext,
        IPlugManageService plugManageService, 
        IPlugCommonExecute plugCommonExecute,
        IEnumerable<IPlugCommonExecute> plugCommonExecutes,
        IPlugExecuteHandlerService plugExecuteHandlerService,
        IPDZManageService pDZManageService,
        IPlugDataService plugDataService,
        MainApiClient mainApiClient,
        ElsaApiClient elsaApiClient,
        IElsaEngineService elsaEngineService
        )
    {
        _dbContext = dbContext;
        PlugManageService = plugManageService;
        PlugCommonExecute = plugCommonExecute;
        PlugExecuteHandlerService = plugExecuteHandlerService;
        PlugCommonExecutes = plugCommonExecutes;
        PDZManageService = pDZManageService;
        PlugDataService = plugDataService;
        MainApiClient = mainApiClient;
        ElsaApiClient = elsaApiClient;
        ElsaEngineService = elsaEngineService;
    }


    /// <summary>
    /// 一：启动插头
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request)
    {
        PlugData? plugData;
        IPlugCommonExecute? handler;
        if(request?.ExecuteResultData.Ids.ExecuteTaskPlugIds.Count==0)
        {
            //首次执行请求提交
            var PlugType = request.PlugType;
            var PlugTypeKey = request.PlugTypeKey;
            var PlugDefinitionId=request.ExecuteResultData.Ids.PlugDefinitionId;

            CLog.Information($"准备启动插头类型：{PlugTypeKey}");

            if (string.IsNullOrEmpty(PlugDefinitionId))
            {
                //无插头ID，说明是独立执行模式，如获取NX模型参数插头的执行,通过类型获取源插头
                var plug = await PlugManageService.GetPlugByTypeName(PlugType);
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
                    handler = PlugExecuteHandlerService.GetExecuteHandler(PlugTypeKey);
                    var result = await handler?.PlugCommonExecute(new(plug, request));
                    return result;
                }
                else
                {
                    handler = PlugExecuteHandlerService.GetExecuteHandler(PlugTypeKey);
                    return await handler?.PlugCommonExecute(new(plug, request));
                }
            }
            else
            {
                //有插头ID，说明是来源于引擎或者流程图，先通过ID获取类型,再通过类型获取源插头
                plugData = await PlugDataService.GetByPlugDefinitionIdAsync(request.ExecuteResultData.Ids.PlugDefinitionId);
                //Log.Information($"准备启动插头{plug.Name}");
                request.ExecuteResultData = request.ExecuteResultData ?? new ExecuteResultData();
                var PDZ = await PDZManageService.GetByPDZId(request.ExecuteResultData.Ids.PDZId);
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

                handler = PlugExecuteHandlerService.GetExecuteHandler(plugData.PlugTypeKey);
                return await handler?.PlugCommonExecute(new(null, request));

            }
        }
        else
        {
            //插头动作执行
            plugData = await PlugDataService.GetByPlugDefinitionIdAsync(request?.ExecuteResultData.Ids.ExecuteTaskPlugIds[0].Split("|")[0]);
            if (plugData == null)
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
            CLog.Information($"准备执行插头/动作{plugData.Name}");
            //StatusReporter.ReportPlugStatus(plug.DefinitionId, new PlugStatus() { Blocked = true });
            handler = PlugExecuteHandlerService.GetExecuteHandler(plugData.PlugTypeKey);
            return await handler?.PlugCommonExecute(new(null, request));
            //return null;
        }        
    }

    /// <summary>
    /// 二：汇报执行结果、通知流程引擎继续执行后续流程、分发下一步动作
    /// </summary>
    /// <param name="executeReport"></param>
    /// <returns></returns>
    public async Task ReportExecuteResult(ExecuteResultData executeReport)
    {
        var status = new PlugStatus();
        var plugId = executeReport.Ids.PlugDefinitionId;
        var correlationId = executeReport.Ids.JobCorrelationId;
        var pdzId = executeReport.Ids.PDZId;

        CLog.Information($"接收到的执行结果：{executeReport.ExecuteStatus}({executeReport.ExecuteSubStatus})", pdzId);

        if (executeReport.ExecuteStatus == JobStatus.执行中)
        {
            if (executeReport.ExecuteSubStatus == JobSubStatus.图站执行完成)
            {
                if (string.IsNullOrEmpty(executeReport.Ids?.ProcessJobEngineId))
                {
                    if (!string.IsNullOrEmpty(correlationId))
                    {
                        var job = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.JobCorrelationId == correlationId);
                        if (job != null)
                            executeReport.Ids.ProcessJobEngineId = job?.EngineInstanceId;
                    }
                }

                var request = new PlugExecutionRequest();
                request.ExecuteResultData = executeReport;
                await StartExecutePlug(request);
            }
            else if (executeReport.ExecuteSubStatus == JobSubStatus.出错)
            {
                status.Blocked = false;
                status.Faulted = true;
                StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                await ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
            else
            {
                Log.Information("该状态无需处理，原地等待。");
            }
        }
        else if (executeReport.ExecuteStatus == JobStatus.完成)
        {
            if (executeReport.ExecuteSubStatus == JobSubStatus.出错)
            {
                status.Blocked = false;
                status.Faulted = true;
                StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                await ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
            else if (executeReport.ExecuteSubStatus == JobSubStatus.已完成)
            {
                if(executeReport.Ids.ExecuteTaskPlugIds.Count>0) executeReport.Ids.ExecuteTaskPlugIds?.RemoveAt(0);
                if(executeReport.Ids.ExecuteTaskPlugIds.Count == 0)
                {
                    status.Blocked = false;
                    status.Completed = 1;
                    StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                    await ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                    await TryAwakenConvergencePlugs(pdzId, correlationId);
                    return;
                }
                var request = new PlugExecutionRequest();
                request.ExecuteResultData = executeReport;
                request.ExecuteResultData.Ids = executeReport.Ids;
                await StartExecutePlug(request);                
            }
            else
            {
                status.Blocked = false;
                status.Completed = 1;
                StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                await ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
        }
        else
        {
            status.Blocked = false;
            status.Completed = 1;
            StatusReporter.ReportPlugStatus(plugId, status, pdzId);
            await ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
            await TryAwakenConvergencePlugs(pdzId, correlationId);
            return;
        }
    }

    /// <summary>
    /// 存储 Outcome 到 PDZ 并恢复 Elsa 书签以唤醒等待中的活动
    /// </summary>
    private async Task ResumeBookmarkAsync(string? correlationId, string? plugId, string? pdzId, string[]? outcomes)
    {
        if (string.IsNullOrEmpty(correlationId) || string.IsNullOrEmpty(plugId)) return;

        try
        {
            // 存储 Outcome 到 PDZ，供 OnResumeAsync 读取
            if (!string.IsNullOrEmpty(pdzId))
            {
                var pdz = await MainApiClient.GetPDZByPDZIdAsync(pdzId);
                if (pdz != null)
                {
                    var outcomeStr = outcomes?.Length > 0 ? string.Join("|", outcomes) : "Done";
                    pdz.SetActivityOutcome(plugId, outcomeStr);
                    await MainApiClient.CreateOrUpdatePDZ(pdz);
                }
            }

            // 通过 SignalR 广播 CompleteActivityContext:
            // 1) 触发 ElsaApiServer 恢复书签（跨进程）
            // 2) 前端 UI 收到消息更新执行状态
            StatusReporter.CompleteActivityContext(correlationId, plugId);
        }
        catch (Exception ex)
        {
            Log.Error($"恢复书签失败 [{plugId}]: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查汇聚插头（如 AndPlug）的全部上游是否就绪，若就绪则：
    /// 1. 更新其 PlugStatus 为已完成
    /// 2. 存储 Outcome → PDZ
    /// 3. 恢复其 Elsa 书签
    /// </summary>
    private async Task TryAwakenConvergencePlugs(string? pdzId, string? jobCorrelationId)
    {
        if (string.IsNullOrEmpty(pdzId)) return;

        try
        {
            var pdz = await MainApiClient.GetPDZByPDZIdAsync(pdzId);
            if (pdz == null) return;

            var readyIds = pdz.GetReadyConvergencePlugIds();
            foreach (var plugId in readyIds)
            {
                Log.Information($"汇聚插头已就绪，唤醒: {plugId}");

                // 更新汇聚插头的状态
                var completedStatus = new PlugStatus { Blocked = false, Completed = 1 };
                pdz.SetPlugStatusData(plugId, completedStatus);
                await MainApiClient.CreateOrUpdatePDZ(pdz);

                // 存储 Outcome（默认 True，若有上游出错则为 False）
                var hasFaultedUpstream = false;
                var dataFlows = pdz.GetDataFlowData();
                if (dataFlows != null)
                {
                    foreach (var flowJson in dataFlows)
                    {
                        if (string.IsNullOrEmpty(flowJson)) continue;
                        try
                        {
                            var flow = System.Text.Json.JsonSerializer.Deserialize<CJ.Plug.Models.DataFlow.PortLinkModel>(flowJson);
                            if (flow?.TargetPort?.PlugDefinitionId == plugId
                                && !string.IsNullOrEmpty(flow.SourcePort?.PlugDefinitionId))
                            {
                                var upstreamStatus = pdz.GetPlugStatus(flow.SourcePort.PlugDefinitionId);
                                if (upstreamStatus?.Faulted == true)
                                {
                                    hasFaultedUpstream = true;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }

                var outcome = hasFaultedUpstream ? "False" : "True";
                pdz.SetActivityOutcome(plugId, outcome);
                await MainApiClient.CreateOrUpdatePDZ(pdz);

                // 恢复书签
                await ResumeBookmarkAsync(jobCorrelationId, plugId, pdzId, [outcome]);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"汇聚检测异常: {ex.Message}");
        }
    }


    






    //-----------------------------------------------------------------------------------------//


    [Obsolete("此方法已弃用，请使用 ExecutePlug(PlugExecutionRequest? request) 代替。")]
    public async Task<string?> ExecutePlug(string definitionId)
    {
        Console.WriteLine(">>>>>>>>>>>>>ready to execute plug.");

        var currentUser = "admin";

        //获取PDZ
        var PlugDataZone =await MainApiClient.GetOrCreatePDZFromPlugDefinitionId(definitionId, currentUser);
        var PlugData = await MainApiClient.GetPlugDataByDefinitionId(definitionId);
        var Flowchart = PlugDataZone?.GetFlowchartData(definitionId);

        Console.WriteLine($"准备执行插头{PlugData?.Name}，Flowchart：{Flowchart}。");

        var TriggerActivityId = PlugDataZone?.TriggerPlugDefinitionId;
        (var PDZId, var JobDefinitionId) = await MainApiClient.SubmitExecute(PlugData, currentUser, PlugDataZone, PDZTypeEnum.Job2);
        
        //PlugDataZone = await MainApiClient.GetPDZByPDZIdAsync(PDZId);
        //Log.Information($"开始监听PDZ{PDZId}-{JobDefinitionId}的执行状态。");
        //await HubActivityStatus(PDZId);

        var executeSetting = new ExecuteSetting
        {
            CorrelationId = PDZId,
            TriggerActivityId = TriggerActivityId,
        };
        var executeResult = await ElsaApiClient.ExecuteWorkflowWithExecuteSetting(Flowchart, executeSetting);

        var WorkflowInstanceId = executeResult.ExecuteResultMessage;

        return $"工具ID为{definitionId}的工具适配器执行成功，执行结果实例为：{WorkflowInstanceId}";

        var executeRequest = new PlugExecutionRequest();
        executeRequest.ExecuteResultData = new ExecuteResultData()
        {
            Ids = new ExecuteIdsBundle()
            {
                PlugDefinitionId = definitionId
            }
        };
        await StartExecutePlug(executeRequest);

        //测试通过CMD命令启动notepad
        //var startInfo = new System.Diagnostics.ProcessStartInfo
        //{
        //    FileName = "notepad.exe",
        //    Arguments = "",
        //    RedirectStandardOutput = false,
        //    UseShellExecute = true,
        //    CreateNoWindow = false
        //};
        //var process = System.Diagnostics.Process.Start(startInfo);
        //process.WaitForExit();


        Console.WriteLine("<<<<<<<<<<<<<<<end execute plug.");

        return $"工具ID为{definitionId}的工具适配器执行成功，执行结果为：666";


        //var hrm = new HttpResponseMessage();
        var plug = await PlugManageService.GetPlugByDefinitionId(definitionId);
        if (plug == null)
        {
            //hrm.StatusCode = System.Net.HttpStatusCode.NotFound;
            //hrm.Content = new StringContent("Plug with the specified definition ID not found.", Encoding.UTF8, "text/plain");
            return "Plug with the specified definition ID not found.";
        }
        Console.WriteLine(">>>>>>>>>>>>>ready to execute plug.");
        string resultString = "";
        var status = new PlugStatus();
        status.Blocked = true;
        StatusReporter.ReportPlugStatus(plug.DefinitionId, status);
        if (!plug.OnlyExecuteAction)
        {
            foreach (var pce in PlugCommonExecutes)
            {
                //1执行插头通用方法
                resultString += await pce.PlugCommonExecute(new(plug));
            }
        }
        var PlugActions = await PlugManageService.GetPlugActionsByPlugIdAsync(plug.Id);
        //2按照顺序执行插头自定义动作
        foreach (var a in PlugActions)
        {
            foreach (var pce in PlugCommonExecutes)
            {
                //动作也是插头，也是执行插头通用方法
                resultString += await pce.PlugCommonExecute(new(a));
            }
        }

        status.Blocked = false;
        status.Completed = 1;
        StatusReporter.ReportPlugStatus(plug.DefinitionId, status);

        Console.WriteLine("<<<<<<<<<<<<<<<end execute plug.");
        return resultString;
    }

    [Obsolete("此方法已弃用，请使用 ExecutePlug(PlugExecutionRequest? request) 代替。")]
    public async Task<string?> ExecutePlug(string definitionId, PlugExecutionRequest? request)
    {
        var plug = await PlugManageService.GetPlugByDefinitionId(definitionId);
        if (plug == null)
        {
            CLog.Error($"Plug with the specified definition ID {definitionId} not found.");
            return null;
        }
        Console.WriteLine(">>>>>>>>>>>>>ready to execute plug.");
        Console.WriteLine(JsonSerializer.Serialize(request));

        //在执行之前通过ExecuteMode的不同，先处理好参数，再交给插头做业务逻辑处理
        //var EvaledCommandLine = "";
        if (request.ExecuteMode == ExecuteMode.Action)
        {
            //从父插头获取参数值
            var ParentPlug = await PlugManageService.GetParentPlugById(plug.Id);
            foreach (var v in ParentPlug.PlugVariables)
            {
                if (v.IsValueFromOtherVariable)
                {
                    //Log.Information($"eval plug variable: {v.SourceValue}");                    
                    var (plugId, variableName) = ParameterGenerator.ExtractNumberAndText(v.SourceValue);
                    //v.Value=await mainApiClient.GetPlugVariableValueAsync(plugId, variableName);
                    var dataSourcePlug = await PlugManageService.GetPlugByDefinitionId(plugId);
                    if (dataSourcePlug is not null)
                    {
                        var variable = dataSourcePlug.PlugVariables.Find(v => v.Name == variableName);
                        if (variable is not null)
                        {
                            v.Value = variable.Value;
                        }
                    }
                    Log.Information($"evaled plug variable: {v.Value}");
                }
            }
            //直接将父插头的参数赋值给插头，用于简化参数处理
            plug.PlugVariables = ParentPlug.PlugVariables;
            //EvaledCommandLine = await ParameterGenerator.EvalCommandLine(plug);
        }
        else if (request.ExecuteMode == ExecuteMode.Standalone)
        {
            Console.WriteLine("standalone execute");
            //独立执行的模式，直接从request中获取参数值
            foreach (var p in request.InputVariables)
            {
                plug.SetVariableValue(p.Name, p.Value);
                Console.WriteLine($"name {p.Name} set to {p.Value}");
            }
            //EvaledCommandLine = await ParameterGenerator.EvalCommandLine(plug);
        }
        else if (request.ExecuteMode == ExecuteMode.Plug)
        {
            //插头执行的模式，直接从自己的参数中获取值
            foreach (var v in plug.PlugVariables)
            {
                if (v.IsValueFromOtherVariable)
                {
                    //Log.Information($"eval plug variable: {v.SourceValue}");                    
                    var (plugId, variableName) = ParameterGenerator.ExtractNumberAndText(v.SourceValue);
                    //v.Value=await mainApiClient.GetPlugVariableValueAsync(plugId, variableName);
                    var dataSourcePlug = await PlugManageService.GetPlugByDefinitionId(plugId);
                    if (dataSourcePlug is not null)
                    {
                        var variable = dataSourcePlug.PlugVariables.Find(v => v.Name == variableName);
                        if (variable is not null)
                        {
                            v.Value = variable.Value;
                        }
                    }
                    Log.Information($"evaled plug variable: {v.Value}");
                }
            }
            //用处理好的参数值来处理执行命令，如[ModelFilePath]替换为实际的值
            //交给插头自己处理
            //EvaledCommandLine = await ParameterGenerator.EvalCommandLine(plug);
        }
        //plug.SetPlugSetting(PlugSettingKey.CommandLineShema.ToString(),EvaledCommandLine);
        //Log.Information($"EvaledCommandLine: {EvaledCommandLine}");
        //Console.WriteLine($"ready to execute plug id : {plug.DefinitionId}");
        string resultString = "";
        var status = new PlugStatus();
        status.Blocked = true;
        Log.Information($"{plug.DefinitionId}|{JsonSerializer.Serialize(status)}");
        foreach (var pce in PlugCommonExecutes)
        {
            //1执行插头通用方法
            resultString += await pce.PlugCommonExecute(new(plug, request));
        }
        var PlugActions = await PlugManageService.GetPlugActionsByPlugIdAsync(plug.Id);
        //2按照顺序执行插头自定义动作
        foreach (var a in PlugActions)
        {
            foreach (var pce in PlugCommonExecutes)
            {
                resultString += await pce.PlugCommonExecute(new(a, request));
            }
        }

        //status.Blocked = false;
        //status.Completed = 1;
        //Log.Information($"{plug.DefinitionId}|{JsonSerializer.Serialize(status)}");
        Console.WriteLine("<<<<<<<<<<<<<<<end execute plug.");
        return resultString;
    }

    /// <summary>
    /// MCP Tool 执行：从 Use PDZ 复制为 Job3 PDZ，填充参数后执行工作流
    /// </summary>
    public async Task<string?> ExecuteMcpWorkflow(PlugExecutionRequest request)
    {
        var definitionId = request.PlugDefinitionId;
        if (string.IsNullOrEmpty(definitionId))
        {
            CLog.Error("ExecuteMcpWorkflow: PlugDefinitionId 为空");
            return null;
        }

        // 1. 查找 Use PDZ（参数模板）
        var usePDZId = "MCP_Use_" + definitionId;
        var usePDZ = await PDZManageService.GetByPDZId(usePDZId);
        if (usePDZ == null)
        {
            CLog.Error("ExecuteMcpWorkflow: 未找到 Use PDZ，请先发布为 MCP Tool");
            return null;
        }

        // 2. 获取 Plug 数据和流程图
        var plugData = await PlugDataService.GetByPlugDefinitionIdAsync(definitionId);
        var flowchart = usePDZ.GetFlowchartData(definitionId);
        if (plugData == null || flowchart == null)
        {
            CLog.Error("ExecuteMcpWorkflow: 未找到 Plug");
            return null;
        }

        // 3. 从 Use PDZ 复制为 Job3 PDZ
        var currentUser = "MCP_Caller";
        (var jobPDZId, var jobDefinitionId) = await MainApiClient.SubmitExecute(
            plugData, currentUser, usePDZ, PDZTypeEnum.Job3);

        if (string.IsNullOrEmpty(jobPDZId))
        {
            CLog.Error("ExecuteMcpWorkflow: 创建 Job3 PDZ 失败");
            return null;
        }

        // 4. 填充输入参数到 Job3 PDZ
        var jobPDZ = await MainApiClient.GetPDZByPDZIdAsync(jobPDZId);
        if (jobPDZ != null && request.InputVariables?.Count > 0)
        {
            foreach (var v in request.InputVariables)
            {
                jobPDZ.SetVariableValue(definitionId, v.Name, v.Value, v.Type);
            }
            await MainApiClient.CreateOrUpdatePDZ(jobPDZ);
        }

        // 5. 构造执行请求，走 Elsa 引擎
        var executeSetting = new ExecuteSetting
        {
            CorrelationId = jobPDZId,
            TriggerActivityId = usePDZ.TriggerPlugDefinitionId,
        };

        var executeResult = await ElsaApiClient.ExecuteWorkflowWithExecuteSetting(flowchart, executeSetting);
        var workflowInstanceId = executeResult?.ExecuteResultMessage;

        return "MCP 工作流执行成功，实例 ID: " + workflowInstanceId;
    }

}

