
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;

public interface IPlugExecuteService
{
    Task<string?> ExecutePlug(string definitionId);
    Task<string?> ExecutePlug(string definitionId, PlugExecutionRequest? request);
    Task ReportExecuteResult(ExecuteResultData executeReport);
    Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request);
    /// <summary>
    /// MCP Tool 执行：从 Use PDZ 复制为 Job3，填充参数后执行工作流
    /// </summary>
    Task<string?> ExecuteMcpWorkflow(PlugExecutionRequest request);
    //Task<HttpResponseMessage> StationToolExecution(StationExecutionRequest stationExectionRequest);
}