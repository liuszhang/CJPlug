using CJ.Plug.Models.Job;

public partial class MainApiClient : IExecuteApiClient
{
    public async Task<ExecuteResultData?> ExecuteOnStation(PlugExecutionRequest stationExectionRequest)=>await ExecuteApiClient.Value.ExecuteOnStation(stationExectionRequest);
    public async Task<ExecuteResultData?> ExecutePlug(PlugExecutionRequest plugExecutionRequest, CancellationToken ct = default)=>await ExecuteApiClient.Value.ExecutePlug(plugExecutionRequest, ct);
    public async Task<ExecuteResultData?> ExecutePlugByDefifnitionId(string definitionId, string? CorrelationId = null, CancellationToken ct = default)=>await ExecuteApiClient.Value.ExecutePlugByDefifnitionId(definitionId, CorrelationId, ct);
    public async Task<ExecuteResultData?> ExecutePlugByType(PlugExecutionRequest PlugExecutionRequest, CancellationToken ct = default)=>await ExecuteApiClient.Value.ExecutePlugByType(PlugExecutionRequest, ct);
    public async Task ExecutePlugWithRequest(string DefinitionId, PlugExecutionRequest plugExecutionRequest, CancellationToken ct = default)=>await ExecuteApiClient.Value.ExecutePlugWithRequest(DefinitionId, plugExecutionRequest, ct);

    public async Task ExecuteResultReport(ExecuteResultData executeReport) => await ExecuteApiClient.Value.ExecuteResultReport(executeReport);
    public async Task<string?> ExecuteToolCommand(PlugExecutionRequest request)=>await ExecuteApiClient.Value.ExecuteToolCommand(request);
    public async Task<(string?, string?)> SubmitExecute(PlugData PlugData, string UserName, PlugDataZone PlugDataZone, PDZTypeEnum PDZType)=>await ExecuteApiClient.Value.SubmitExecute(PlugData, UserName, PlugDataZone,PDZType);
}




