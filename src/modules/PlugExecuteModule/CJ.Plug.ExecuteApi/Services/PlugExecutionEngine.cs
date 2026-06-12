using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Station;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.SharedPages.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

/// <summary>
/// 插头执行引擎（拆分重构 - partial class）。
/// 字段、构造函数与公共入口方法保持原签名，以保证 11 个回归测试零修改。
/// </summary>
internal partial class PlugExecutionEngine
{
    private readonly MainDbContext _dbContext;
    private readonly IPlugManageService _plugManageService;
    private readonly IEnumerable<IPlugCommonExecute> _plugCommonExecutes;
    private readonly IPlugExecuteHandlerService _plugExecuteHandlerService;
    private readonly IPDZManageService _pdzManageService;
    private readonly IPlugDataService _plugDataService;
    private readonly PlugBookmarkManager _bookmarkManager;
    private readonly McpWorkflowRunner _mcpWorkflowRunner;

    public PlugExecutionEngine(
        MainDbContext dbContext,
        IPlugManageService plugManageService,
        IEnumerable<IPlugCommonExecute> plugCommonExecutes,
        IPlugExecuteHandlerService plugExecuteHandlerService,
        IPDZManageService pdzManageService,
        IPlugDataService plugDataService,
        PlugBookmarkManager bookmarkManager,
        McpWorkflowRunner mcpWorkflowRunner)
    {
        _dbContext = dbContext;
        _plugManageService = plugManageService;
        _plugCommonExecutes = plugCommonExecutes;
        _plugExecuteHandlerService = plugExecuteHandlerService;
        _pdzManageService = pdzManageService;
        _plugDataService = plugDataService;
        _bookmarkManager = bookmarkManager;
        _mcpWorkflowRunner = mcpWorkflowRunner;
    }

    /// <summary>
    /// 一：启动插头
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request)
    {
        // 防御性初始化：防止 API 反序列化或上游未初始化 ExecuteResultData/Ids 导致 NRE
        if (request != null)
        {
            request.ExecuteResultData ??= new ExecuteResultData();
        }

        // 构造共享上下文对象，跨子方法传递必要状态
        var ctx = new StartExecutePlugContext
        {
            Request = request,
            PlugData = null,
            Handler = null,
        };

        if (request?.ExecuteResultData.Ids.ExecuteTaskPlugIds.Count == 0)
        {
            //首次执行请求提交
            var PlugTypeKey = request.PlugTypeKey;
            var PlugDefinitionId = request.ExecuteResultData.Ids.PlugDefinitionId;

            CLog.Information($"准备启动插头类型：{PlugTypeKey}");

            if (string.IsNullOrEmpty(PlugDefinitionId))
            {
                //无插头ID，说明是独立执行模式，如获取NX模型参数插头的执行,通过类型获取源插头
                return await ExecuteNoPlugIdPathAsync(ctx);
            }
            else
            {
                //有插头ID，说明是来源于引擎或者流程图，先通过ID获取类型,再通过类型获取源插头
                return await ExecuteFlowSourcePathAsync(ctx);
            }
        }
        else
        {
            //插头动作执行
            return await ExecuteFlowSourcePathAsync(ctx);
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
                await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
            else if (executeReport.ExecuteSubStatus == JobSubStatus.已完成)
            {
                // 图站执行完成回调：ExecuteStatus=执行中, ExecuteSubStatus=已完成
                // 需要通知前端状态更新并恢复 Elsa 书签继续后续流程
                if (executeReport.Ids.ExecuteTaskPlugIds.Count > 0) executeReport.Ids.ExecuteTaskPlugIds?.RemoveAt(0);
                if (executeReport.Ids.ExecuteTaskPlugIds.Count == 0)
                {
                    status.Blocked = false;
                    status.Completed = 1;
                    StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                    await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                    await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
                    return;
                }
                var request = new PlugExecutionRequest();
                request.ExecuteResultData = executeReport;
                request.ExecuteResultData.Ids = executeReport.Ids;
                await StartExecutePlug(request);
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
                await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
            else if (executeReport.ExecuteSubStatus == JobSubStatus.已完成)
            {
                if (executeReport.Ids.ExecuteTaskPlugIds.Count > 0) executeReport.Ids.ExecuteTaskPlugIds?.RemoveAt(0);
                if (executeReport.Ids.ExecuteTaskPlugIds.Count == 0)
                {
                    status.Blocked = false;
                    status.Completed = 1;
                    StatusReporter.ReportPlugStatus(plugId, status, pdzId);
                    await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                    await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
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
                await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
                await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
                return;
            }
        }
        else
        {
            status.Blocked = false;
            status.Completed = 1;
            StatusReporter.ReportPlugStatus(plugId, status, pdzId);
            await _bookmarkManager.ResumeBookmarkAsync(correlationId, plugId, pdzId, executeReport.Outcome);
            await _bookmarkManager.TryAwakenConvergencePlugs(pdzId, correlationId);
            return;
        }
    }

    /// <summary>
    /// 共享错误上报辅助方法（重构自原 StartExecutePlug 内的 local function）。
    /// 原版为闭包 `request` 的 local function；拆 partial 后改为显式接收 request。
    /// 行为/日志文本/返回字段顺序 100% 等价。
    /// </summary>
    private async Task<ExecuteResultData> BuildAndReportErrorAsync(
        string message,
        PlugExecutionRequest? request,
        string? definitionId = null,
        string? plugName = null)
    {
        CLog.Error($"StartExecutePlug: {message} (PlugDefinitionId={definitionId ?? "-"}, PlugName={plugName ?? "-"})");
        var erd = new ExecuteResultData
        {
            ExecuteStatus = JobStatus.完成,
            ExecuteSubStatus = JobSubStatus.出错,
            Ids = request?.ExecuteResultData?.Ids
        };
        await ReportExecuteResult(erd);
        return erd;
    }

    /// <summary>
    /// 拆分后跨 partial 方法共享的执行上下文。
    /// 封装 PlugData / Handler / Request 等跨分支状态，避免对字段/参数的多次重算。
    /// </summary>
    private sealed class StartExecutePlugContext
    {
        public PlugExecutionRequest? Request { get; set; }
        public PlugData? PlugData { get; set; }
        public IPlugCommonExecute? Handler { get; set; }
    }
}
