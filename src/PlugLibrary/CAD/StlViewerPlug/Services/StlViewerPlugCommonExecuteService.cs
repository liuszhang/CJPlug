using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace StlViewerPlug.Services;

/// <summary>
/// STL 查看器执行服务 — 直接返回完成（文件上传由前端处理）
/// </summary>
public class StlViewerPlugCommonExecuteService(IServiceProvider serviceProvider)
    : BasePlugExecuteService(serviceProvider)
{
    public override bool IsThisPlugTypeKey(string? PlugTypeKey)
        => PlugTypeKey == PlugKeySetting.CommonExecuteKey;

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        var erd = context.plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
        if (!await DataPrepare(context.plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
            return await ReportErrorResult(erd);

        CLog.Information("--执行 STL 查看器插头--", PlugDataZone.PDZId);

        // STL 查看器不需要后端处理，文件已由前端上传到变量中
        return await ReportCompletedResult(erd);
    }
}
