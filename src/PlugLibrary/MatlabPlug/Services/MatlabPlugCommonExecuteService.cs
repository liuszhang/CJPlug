
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using MatlabPlug;
using Serilog;
using System.Text.Json;

public class MatlabPlugCommonExecuteService(MainApiClient MainApiClient) : IPlugCommonExecute
{
    public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plugToExecute = context.plugToExecute;
        
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        Log.Information($"execute matlab plug: {plugToExecute.Name}");
        return await ((IPlugCommonExecute)this).ExecuteResultReport(
                    MainApiClient,
                    plugToExecute,
                    plugExecutionRequest,
                    new Dictionary<string, string>(),
                    JobStatus.完成,
                    JobSubStatus.已完成);
    }

    
}

