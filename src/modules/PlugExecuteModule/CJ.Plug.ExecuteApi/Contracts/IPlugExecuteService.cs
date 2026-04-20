
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;

public interface IPlugExecuteService
{
    Task<string?> ExecutePlug(string definitionId);
    Task<string?> ExecutePlug(string definitionId, PlugExecutionRequest? request);
    Task ReportExecuteResult(ExecuteResultData executeReport);
    Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request);
    //Task<HttpResponseMessage> StationToolExecution(StationExecutionRequest stationExectionRequest);
}