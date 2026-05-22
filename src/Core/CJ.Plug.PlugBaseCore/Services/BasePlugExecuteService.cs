using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// 插头执行基础服务类，实现一些默认方法，如汇报出错、汇报完成、数据准备等
/// </summary>
public abstract class BasePlugExecuteService : IPlugCommonExecute
{
    public readonly IServiceProvider _serviceProvider;
    public MainApiClient? MainApiClient { get; set; }
    public IPDZApiClient? PDZApiClient { get; set; }

    public PlugDataZone? PlugDataZone { get; set; }

   
    public BasePlugExecuteService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        MainApiClient = _serviceProvider.GetService<MainApiClient>();
        PDZApiClient=_serviceProvider.GetService<IPDZApiClient>();
    }
    //public BasePlugExecuteService(MainApiClient mainApiClient)
    //{
    //    MainApiClient = mainApiClient;
    //}

    public abstract bool IsThisPlugTypeKey(string? PlugTypeKey);

    public abstract Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context);

    //汇报执行状态方法
    public async Task<ExecuteResultData> ExecuteResultReport(ExecuteResultData executeResultData)
    {
        await MainApiClient.ExecuteResultReport(executeResultData);
        return executeResultData;
    }

    //执行出错方便方法
    public async Task<ExecuteResultData> ReportErrorResult(ExecuteResultData executeResultData)
    {
        //重置ExecuteValue为[NeedGen]

        executeResultData.ExecuteStatus=JobStatus.完成;
        executeResultData.ExecuteSubStatus=JobSubStatus.出错;
        await ExecuteResultReport(executeResultData);
        return executeResultData;
    }

    //执行完成方便方法
    public async Task<ExecuteResultData> ReportCompletedResult(ExecuteResultData executeResultData)
    {
        //重置ExecuteValue为[NeedGen]

        executeResultData.ExecuteStatus = JobStatus.完成;
        executeResultData.ExecuteSubStatus = JobSubStatus.已完成;
        await ExecuteResultReport(executeResultData);
        return executeResultData;
    }

    /// <summary>
    /// 通用数据准备方法
    /// </summary>
    /// <param name="plugExecutionRequest">执行请求</param>
    /// <returns>数据准备成功与否</returns>
    public async Task<bool> DataPrepare(PlugExecutionRequest? plugExecutionRequest, string[]? VariableNames=null)
    {        
        try
        {
            var result = plugExecutionRequest.ExecuteResultData ?? new ExecuteResultData();
            PDZApiClient= PDZApiClient??_serviceProvider.GetRequiredService<IPDZApiClient>();
            PlugDataZone = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
            CLog.Information($"PDZID:{plugExecutionRequest.ExecuteResultData.Ids.PDZId}", plugExecutionRequest.ExecuteResultData.Ids.PDZId);
            if (PlugDataZone == null)
            {
                CLog.Error("未找到数据空间，执行中止，请检查数据。");
                return false;
            }
            if (VariableNames != null)
            {
                var plugDefinitionId = plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId;
                foreach (var v in VariableNames)
                {
                    //将参数的值处理为真实的值，主要是为了处理有引用关系的参数的值
                    var variableValue = PlugDataZone.GetVariableValue(plugDefinitionId, v);
                    PlugDataZone.SetVariableValue(plugDefinitionId, v, variableValue);
                }
                await PDZApiClient.CreateOrUpdatePDZ(PlugDataZone);
            }
            return true;
        }
        catch(Exception ex)
        {
            CLog.Error("数据准备异常，执行中止，请检查数据。");
            CLog.Error(ex.StackTrace);
            CLog.Error(ex.Message);
            return false;
        }
    }
}

