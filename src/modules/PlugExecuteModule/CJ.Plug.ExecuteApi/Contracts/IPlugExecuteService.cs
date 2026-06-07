
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
    /// MCP 统一执行端点：根据 ToolType 路由到工作流或单插头执行
    /// </summary>
    Task<string?> ExecuteMcpTool(CJ.Plug.Models.MCPTools.McpToolExecutionRequest request);

    /// <summary>
    /// 查询工作流执行状态
    /// </summary>
    Task<CJ.Plug.Models.MCPTools.ExecutionStatusDto?> GetExecutionStatus(string workflowInstanceId);
    //Task<HttpResponseMessage> StationToolExecution(StationExecutionRequest stationExectionRequest);
}