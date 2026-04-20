using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;
using RESTPlug;
using RESTPlug.Utils;
using System.Text.Json;

public class RESTPlugCommonExecuteService : BasePlugExecuteService
{
    public RESTPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
       
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData;

        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

        CLog.Information($"execute rest plug", PlugDataZone.PDZId);

        var result =await new SendHttpRequestPlugCommonExecuteService(_serviceProvider).TrySendAsync(PlugDataZone, plugExecutionRequest.PlugDefinitionId);
        //var result =await sendHttpRequestPlug.TrySendAsync(PlugDataZone, plugExecutionRequest.PlugDefinitionId);
        //CLog.Information($"result.ResultString:{result?.ResultString}",PlugDataZone.PDZId);
        erd.ResultString= result.ResultString;

        //var OutputMapping = PlugDataZone.GetVariableValue(plugExecutionRequest.PlugDefinitionId,InitVariableNames.OutputMappings.ToString());
        var OutputMapping = PlugDataZone.PlugVariableDatas.FirstOrDefault(p =>
        p.PlugDefinitionId == plugExecutionRequest.PlugDefinitionId && p.Name == InitVariableNames.OutputMappings.ToString());
        if (string.IsNullOrEmpty(OutputMapping.Value))
        {
            return await ReportCompletedResult(erd);
        }
        var Outputs = JsonSerializer.Deserialize<List<DefaultOutputMapping>>(OutputMapping.Value);
        foreach (var output in Outputs)
        {
            output.Value = DataParser.GetParsedResult(erd?.ResultString,output.ReadSchemaValue);
            //CLog.Information($"{output.OutputName}:{output.Value.ToString()}",PlugDataZone.PDZId);
            PlugDataZone.SetVariableValue(plugExecutionRequest.PlugDefinitionId, output.OutputName, output.Value);
        }
        await MainApiClient.CreateOrUpdatePDZ(PlugDataZone);
        //CLog.Information($"updated PDZ in main api after set variable values", PlugDataZone.PDZId);

        //OutputMapping.Value=JsonSerializer.Serialize(Outputs);
        //await MainApiClient.UpdatePlugVariableData(OutputMapping);

        //CLog.Information(erd.Ids.PlugDefinitionId,PlugDataZone.PDZId);

        return await ReportCompletedResult(erd);
    }
}

