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
    /// <para>P5.9 / D-P5-7：<paramref name="accessToken"/> 由调用方（Blazor 电路）从 <c>.ThreeRole.Token</c>
    /// cookie 读出后传入；服务端据此签发 60 秒一次性换票码，让子进程的 WebView2 拿到登录态。
    /// 为空时服务端返回 401（不再允许匿名拉起进程）。</para>
    /// </summary>
    Task LaunchStandaloneAppAsync(string definitionId, string? accessToken = null, CancellationToken cancellationToken = default);
}

