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
/// 默认插头执行服务，适用于无特殊执行配置的插头（桌面类回退处理器）。
/// </summary>
public class DefaultPlugExecuteService : BasePlugExecuteService, IPlugCategoryFallbackHandler
{
    private IToolExecuteService ToolExecuteService { get; set; }
    private ITASApiClient? TASApiClient;

    public string Category => "桌面类";

    public DefaultPlugExecuteService(IServiceProvider serviceProvider,
        IToolExecuteService toolExecuteService
        ) : base(serviceProvider)
    {
        ToolExecuteService= toolExecuteService;
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => false;

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Log.Information($"执行插头类型：{context.plugExecutionRequest.PlugTypeKey}");
        Console.WriteLine($"plug type to execute：{context.plugExecutionRequest.PlugTypeKey}");
        TASApiClient= TASApiClient??_serviceProvider.GetRequiredService<ITASApiClient>();
        var plug = context.plugToExecute ?? await TASApiClient.GetRootPlugByTypeNameAsync(context.plugExecutionRequest.PlugTypeKey);
        PlugExecutionRequest? plugExecutionRequest= context.plugExecutionRequest;

        if (plug == null || string.IsNullOrEmpty(plug.Category))
        {
            CLog.Error($"获取插头或插头类别失败。PlugTypeKey={context.plugExecutionRequest.PlugTypeKey}");
            return await ReportErrorResult(plugExecutionRequest.ExecuteResultData);
        }

        Log.Information($"执行通用桌面类插头逻辑({plug.Name})");
        var ExecuteResultData = await StationCategoryExecuteMethod.Execute(plug, plugExecutionRequest, PDZApiClient, ToolExecuteService);
        return await ExecuteResultReport(ExecuteResultData);
    }
}
