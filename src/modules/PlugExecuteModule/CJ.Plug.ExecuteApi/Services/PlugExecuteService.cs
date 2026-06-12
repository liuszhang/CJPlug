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
using Serilog;
using System.Text.Json;

public class PlugExecuteService : IPlugExecuteService
{
    private readonly MainDbContext _dbContext;

    private IPlugManageService PlugManageService { get; set; }
    private IPlugCommonExecute PlugCommonExecute { get; set; }
    private IEnumerable<IPlugCommonExecute> PlugCommonExecutes { get; set; }
    private IPlugExecuteHandlerService PlugExecuteHandlerService { get; set; }
    private IPDZManageService PDZManageService { get; set; }
    private IPlugDataService PlugDataService { get; set; }
    private MainApiClient MainApiClient { get; set; }
    private ElsaApiClient ElsaApiClient { get; set; }
    private IElsaEngineService ElsaEngineService { get; set; }

    private readonly PlugBookmarkManager _bookmarkManager;
    private readonly McpWorkflowRunner _mcpWorkflowRunner;
    private readonly PlugExecutionEngine _engine;

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

        _bookmarkManager = new PlugBookmarkManager(MainApiClient);
        _mcpWorkflowRunner = new McpWorkflowRunner(PDZManageService, PlugManageService, MainApiClient, ElsaApiClient);
        _engine = new PlugExecutionEngine(
            _dbContext,
            PlugManageService,
            PlugCommonExecutes,
            PlugExecuteHandlerService,
            PDZManageService,
            PlugDataService,
            _bookmarkManager,
            _mcpWorkflowRunner);
    }


    /// <summary>
    /// 一：启动插头
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request)
    {
        return await _engine.StartExecutePlug(request);
    }

    /// <summary>
    /// 二：汇报执行结果、通知流程引擎继续执行后续流程、分发下一步动作
    /// </summary>
    /// <param name="executeReport"></param>
    /// <returns></returns>
    public async Task ReportExecuteResult(ExecuteResultData executeReport)
    {
        await _engine.ReportExecuteResult(executeReport);
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
    /// MCP 统一执行端点：构造 PlugExecutionRequest 后走 StartExecutePlug 统一路径
    /// </summary>
    public async Task<string?> ExecuteMcpTool(CJ.Plug.Models.MCPTools.McpToolExecutionRequest request)
    {
        if (string.IsNullOrEmpty(request.PlugDefinitionId))
        {
            CLog.Error("ExecuteMcpTool: PlugDefinitionId 为空");
            return "错误: PlugDefinitionId 为空";
        }

        var plugRequest = new PlugExecutionRequest
        {
            PlugDefinitionId = request.PlugDefinitionId,
            InputVariables = request.InputVariables ?? new(),
            ExecuteMode = request.ToolType == "Workflow" ? ExecuteMode.Plug : ExecuteMode.Standalone,
            McpToolType = request.ToolType,
        };

        var result = await StartExecutePlug(plugRequest);

        // 提取执行结果
        if (result == null)
            return "执行完成（无返回结果）";

        if (!string.IsNullOrEmpty(result.ResultString))
            return result.ResultString;

        if (!string.IsNullOrEmpty(result.ExecuteResultMessage))
            return result.ExecuteResultMessage;

        return $"执行状态: {result.ExecuteStatus}({result.ExecuteSubStatus})";
    }

    /// <summary>
    /// 查询 Elsa 工作流实例的执行状态
    /// </summary>
    public async Task<CJ.Plug.Models.MCPTools.ExecutionStatusDto?> GetExecutionStatus(string workflowInstanceId)
    {
        if (string.IsNullOrEmpty(workflowInstanceId))
            return null;

        try
        {
            // 通过 correlationId 查询工作流实例
            var instance = await ElsaApiClient.GetWorkflowInstanceByCorrelationIdAsync(workflowInstanceId);

            if (instance == null)
                return new CJ.Plug.Models.MCPTools.ExecutionStatusDto
                {
                    WorkflowInstanceId = workflowInstanceId,
                    Status = "NotFound",
                    ResultMessage = "未找到工作流实例",
                };

            return new CJ.Plug.Models.MCPTools.ExecutionStatusDto
            {
                WorkflowInstanceId = instance.Id,
                Status = instance.Status?.ToString(),
                CreatedAt = instance.CreatedAt,
                FinishedAt = instance.FinishedAt,
            };
        }
        catch (Exception ex)
        {
            CLog.Error($"GetExecutionStatus 失败: {ex.Message}");
            return new CJ.Plug.Models.MCPTools.ExecutionStatusDto
            {
                WorkflowInstanceId = workflowInstanceId,
                Status = "Error",
                ResultMessage = ex.Message,
            };
        }
    }

}
