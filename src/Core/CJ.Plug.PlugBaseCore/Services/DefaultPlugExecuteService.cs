using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.TASApiClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog;


/// <summary>
/// 默认插头执行服务，适用于无特殊执行配置的插头
/// </summary>
public class DefaultPlugExecuteService : BasePlugExecuteService
{
    private IToolExecuteService ToolExecuteService { get; set; }
    private ITASApiClient? TASApiClient;

    public DefaultPlugExecuteService(IServiceProvider serviceProvider,
        IToolExecuteService toolExecuteService
        ) : base(serviceProvider)
    {
        ToolExecuteService= toolExecuteService;
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => string.IsNullOrEmpty(PlugTypeKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Log.Information($"执行插头类型：{context.plugExecutionRequest.PlugType}");
        Console.WriteLine($"plug type to execute：{context.plugExecutionRequest.PlugType}");
        TASApiClient= TASApiClient??_serviceProvider.GetRequiredService<ITASApiClient>();
        var plug= await TASApiClient.GetRootPlugByTypeNameAsync(context.plugExecutionRequest.PlugType);
        PlugExecutionRequest? plugExecutionRequest= context.plugExecutionRequest;

        if (plug == null || string.IsNullOrEmpty(plug.Category))
        {
            CLog.Error("获取插头或插头类别失败");
            return await ReportErrorResult(plugExecutionRequest.ExecuteResultData);
        }

        if (plug.Category.Contains("桌面类"))
        {
            Log.Information($"执行通用桌面类插头逻辑({plug.Name})");
            var ExecuteResultData = await StationCategoryExecuteMethod.Execute(plug, plugExecutionRequest,PDZApiClient,ToolExecuteService);
            return await ExecuteResultReport(ExecuteResultData);

        }
        else if (plug.Category == PlugCategorys.接口类.ToString()) { return null; }
        else if (plug.Category == PlugCategorys.脚本类.ToString()) { return null; }
        return await ReportCompletedResult(plugExecutionRequest.ExecuteResultData);

    }
}

