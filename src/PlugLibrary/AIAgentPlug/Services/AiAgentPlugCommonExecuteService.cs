using AiAgentPlug;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

public class AiAgentPlugCommonExecuteService : IPlugCommonExecute
{
    public HubConnectionManagerService HubConnectionManagerService { get; set; }
    private MainApiClient MainApiClient { get; set; } = default!;

    public AiAgentPlugCommonExecuteService(MainApiClient mainApiClient, HubConnectionManagerService hubConnectionManagerService)
    {
        //StationApiClient = stationApiClient;
        MainApiClient = mainApiClient;
        HubConnectionManagerService = hubConnectionManagerService;
    }

    public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plug = context.plugToExecute;
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        Log.Information("开始执行AI AGENT执行操作");

        return await ((IPlugCommonExecute)this).ExecuteResultReport(
                    MainApiClient,
                    plug,
                    plugExecutionRequest,
                    new Dictionary<string, string>(),
                    JobStatus.完成,
                    JobSubStatus.已完成);

    }

    
}

