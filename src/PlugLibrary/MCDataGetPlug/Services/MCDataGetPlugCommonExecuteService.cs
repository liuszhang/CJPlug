
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using MCDataGetPlug;
using Serilog;
using System.Text.Json;

public class MCDataGetPlugCommonExecuteService : IPlugCommonExecute
{
    public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plugToExecute = context.plugToExecute;
        
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        Log.Information($"execute MCDataGet plug: {plugToExecute.Name}");
        return null;
        //var status = new ActivityStats();
        //status.Blocked = true;
        //Log.Information($"{plugToExecute.DefinitionId}|{JsonSerializer.Serialize(status)}");
        //Log.Information($"execute python plug: {plugToExecute.Name}");
        //var status = new ActivityStats();

        //status.Blocked = true;
        //Log.Information($"{plugToExecute.DefinitionId}|{JsonSerializer.Serialize(status)}");
        ////Console.WriteLine($"execute python plug id : {plugToExecute.DefinitionId}");
        //Thread.Sleep(2000);
        //status.Blocked = false;
        //status.Completed = 1;
        //Log.Information($"{plugToExecute.DefinitionId}|{JsonSerializer.Serialize(status)}");
    }

    
}

