
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.AspNetCore.Components;
using NXPlug_AIModel;
using Serilog;
using System.Text.Json;

public class NXAIModelPlugCommonExecuteService : IPlugCommonExecute
{
    public HubConnectionManagerService HubConnectionManagerService { get; set; }
    private MainApiClient MainApiClient { get; set; } = default!;

    public NXAIModelPlugCommonExecuteService(MainApiClient mainApiClient, HubConnectionManagerService hubConnectionManagerService)
    {
        //StationApiClient = stationApiClient;
        MainApiClient = mainApiClient;
        HubConnectionManagerService = hubConnectionManagerService;
    }

    public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plugToExecute = context.plugToExecute;
        
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

        return null;

    }

    
}

