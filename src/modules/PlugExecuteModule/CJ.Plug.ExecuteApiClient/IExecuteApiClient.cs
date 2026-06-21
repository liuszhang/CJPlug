using CJ.Plug.Models.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IExecuteApiClient
{
    Task<ExecuteResultData?> ExecuteOnStation(PlugExecutionRequest stationExectionRequest);
    Task<ExecuteResultData?> ExecutePlug(PlugExecutionRequest plugExecutionRequest, CancellationToken cancellationToken = default);
    Task<ExecuteResultData?> ExecutePlugByDefifnitionId(string definitionId, string? CorrelationId = null, CancellationToken cancellationToken = default);
    Task<ExecuteResultData?> ExecutePlugByType(PlugExecutionRequest PlugExecutionRequest, CancellationToken cancellationToken = default);
    Task ExecutePlugWithRequest(string DefinitionId, PlugExecutionRequest plugExecutionRequest, CancellationToken cancellationToken = default);
    Task<string?> ExecuteToolCommand(PlugExecutionRequest request);
    Task<(string?, string?)> SubmitExecute(PlugData PlugData, string UserName, PlugDataZone PlugDataZone, PDZTypeEnum PDZType);

    Task ExecuteResultReport(ExecuteResultData executeReport);

    /// <summary>
    /// 启动独立的 WPF 桌面执行程序 (CJ.Plug.ExecuteApp)
    /// </summary>
    Task LaunchStandaloneAppAsync(string definitionId, CancellationToken cancellationToken = default);
}

