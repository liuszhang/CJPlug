using CJ.Plug.Models.DataFlow;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.SharedPages.Services;
using Serilog;

internal class PlugBookmarkManager
{
    private readonly MainApiClient _mainApiClient;

    public PlugBookmarkManager(MainApiClient mainApiClient)
    {
        _mainApiClient = mainApiClient;
    }

    /// <summary>
    /// 存储 Outcome 到 PDZ 并恢复 Elsa 书签以唤醒等待中的活动
    /// </summary>
    internal async Task ResumeBookmarkAsync(string? correlationId, string? plugId, string? pdzId, string[]? outcomes)
    {
        if (string.IsNullOrEmpty(correlationId) || string.IsNullOrEmpty(plugId)) return;

        try
        {
            // 存储 Outcome 到 PDZ，供 OnResumeAsync 读取
            if (!string.IsNullOrEmpty(pdzId))
            {
                var pdz = await _mainApiClient.GetPDZByPDZIdAsync(pdzId);
                if (pdz != null)
                {
                    var outcomeStr = outcomes?.Length > 0 ? string.Join("|", outcomes) : "Done";
                    pdz.SetActivityOutcome(plugId, outcomeStr);
                    await _mainApiClient.CreateOrUpdatePDZ(pdz);
                }
            }

            // 通过 SignalR 广播 CompleteActivityContext:
            // 1) 触发 ElsaApiServer 恢复书签（跨进程）
            // 2) 前端 UI 收到消息更新执行状态
            StatusReporter.CompleteActivityContext(correlationId, plugId);
        }
        catch (Exception ex)
        {
            Log.Error($"恢复书签失败 [{plugId}]: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查汇聚插头（如 AndPlug）的全部上游是否就绪，若就绪则：
    /// 1. 更新其 PlugStatus 为已完成
    /// 2. 存储 Outcome → PDZ
    /// 3. 恢复其 Elsa 书签
    /// </summary>
    public async Task TryAwakenConvergencePlugs(string? pdzId, string? jobCorrelationId)
    {
        if (string.IsNullOrEmpty(pdzId)) return;

        try
        {
            var pdz = await _mainApiClient.GetPDZByPDZIdAsync(pdzId);
            if (pdz == null) return;

            var readyIds = pdz.GetReadyConvergencePlugIds();
            foreach (var plugId in readyIds)
            {
                Log.Information($"汇聚插头已就绪，唤醒: {plugId}");

                // 更新汇聚插头的状态
                var completedStatus = new PlugStatus { Blocked = false, Completed = 1 };
                pdz.SetPlugStatusData(plugId, completedStatus);
                await _mainApiClient.CreateOrUpdatePDZ(pdz);

                // 存储 Outcome（默认 True，若有上游出错则为 False）
                var hasFaultedUpstream = false;
                var dataFlows = pdz.GetDataFlowData();
                if (dataFlows != null)
                {
                    foreach (var flowJson in dataFlows)
                    {
                        if (string.IsNullOrEmpty(flowJson)) continue;
                        try
                        {
                            var flow = System.Text.Json.JsonSerializer.Deserialize<CJ.Plug.Models.DataFlow.PortLinkModel>(flowJson);
                            if (flow?.TargetPort?.PlugDefinitionId == plugId
                                && !string.IsNullOrEmpty(flow.SourcePort?.PlugDefinitionId))
                            {
                                var upstreamStatus = pdz.GetPlugStatus(flow.SourcePort.PlugDefinitionId);
                                if (upstreamStatus?.Faulted == true)
                                {
                                    hasFaultedUpstream = true;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }

                var outcome = hasFaultedUpstream ? "False" : "True";
                pdz.SetActivityOutcome(plugId, outcome);
                await _mainApiClient.CreateOrUpdatePDZ(pdz);

                // 恢复书签
                await ResumeBookmarkAsync(jobCorrelationId, plugId, pdzId, [outcome]);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"汇聚检测异常: {ex.Message}");
        }
    }
}
