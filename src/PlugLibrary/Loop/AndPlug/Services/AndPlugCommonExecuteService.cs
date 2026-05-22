using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace AndPlug.Services
{
    public class AndPlugCommonExecuteService : BasePlugExecuteService
    {
        public AndPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            Plug plug = context.plugToExecute;
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"AndPlug 进入等待: {plug.Name}");

            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

            // 获取 PDZ 检查上游连接
            var pdz = await PDZApiClient.GetPDZByPDZIdAsync(erd.Ids.PDZId);
            if (pdz == null)
            {
                Log.Error("AndPlug: 未找到 PDZ");
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.False.ToString()];
                return await ExecuteResultReport(erd);
            }

            var upstreamCount = CountUpstreamPlugs(pdz, plug.DefinitionId);
            Log.Information($"AndPlug: 发现 {upstreamCount} 个上游插头, 挂起等待书签恢复");

            if (upstreamCount == 0)
            {
                Log.Information("AndPlug: 无上游插头，直接通过");
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.已完成;
                erd.Outcome = [InitOutcomes.True.ToString()];
                return await ExecuteResultReport(erd);
            }

            // 返回空 Outcome → CommonCorePlugActivity 创建 Elsa 书签，释放线程
            // 上游全部完成后 TryAwakenConvergencePlugs 会恢复书签
            return new ExecuteResultData
            {
                ExecuteStatus = JobStatus.执行中,
                ExecuteSubStatus = JobSubStatus.Suspended,
                Ids = erd.Ids,
                Outcome = [] // 空 → 触发书签路径
            };
        }

        private static int CountUpstreamPlugs(PlugDataZone pdz, string thisPlugDefinitionId)
        {
            var dataFlows = pdz.GetDataFlowData();
            if (dataFlows == null) return 0;

            var upstreamIds = new HashSet<string>();
            foreach (var flowJson in dataFlows)
            {
                if (string.IsNullOrEmpty(flowJson)) continue;
                try
                {
                    var flow = System.Text.Json.JsonSerializer.Deserialize<CJ.Plug.Models.DataFlow.PortLinkModel>(flowJson);
                    if (flow?.TargetPort?.PlugDefinitionId == thisPlugDefinitionId
                        && !string.IsNullOrEmpty(flow.SourcePort?.PlugDefinitionId))
                    {
                        upstreamIds.Add(flow.SourcePort.PlugDefinitionId);
                    }
                }
                catch { }
            }
            return upstreamIds.Count;
        }
    }
}
