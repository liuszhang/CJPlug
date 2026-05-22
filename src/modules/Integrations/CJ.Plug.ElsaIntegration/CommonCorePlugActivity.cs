using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using Elsa.Studio.Workflows.UI.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using YamlDotNet.Core.Tokens;

[Activity("CJ", "商业软件", "核心插头活动")]
public class CommonCorePlugActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            var serviceProvider = context.GetRequiredService<IServiceProvider>();
            var MainApiClient = new MainApiClient(serviceProvider);

            var jobCorrelationId = context.WorkflowExecutionContext.CorrelationId;
            var bookmarkId = jobCorrelationId + context.Activity.Id;

            CLog.Information($"开始执行插头：{context.Activity.Name}", jobCorrelationId);

            var request = new PlugExecutionRequest
            {
                ExecuteMode = ExecuteMode.Plug,
            };
            request.ExecuteResultData.Ids = new ExecuteIdsBundle
            {
                JobCorrelationId = jobCorrelationId,
                PDZId = jobCorrelationId.EndsWith("Child") ? jobCorrelationId.Replace("Child", "") : jobCorrelationId,
                PlugDefinitionId = context.Activity.Id
            };

            CLog.Information("============CREATE BOOKMARK,WAIT FOR EXECUTING...============");

            var erd = await MainApiClient.ExecutePlug(request);
            CLog.Information($"执行输出：{string.Join("|", erd?.Outcome)}", jobCorrelationId);

            // 判断同步/异步：ExecuteStatus == 完成 → 同步完成，否则 → 创建书签等待
            // 注意：不能基于 Outcome.Length 判断，因为默认值 ["Done"] 会导致异步插头也走同步分支
            if (erd?.ExecuteStatus == JobStatus.完成)
            {
                var outcomes = (erd.Outcome?.Length > 0) ? erd.Outcome : ["Done"];
                CLog.Information($"插头同步执行完成，Outcome: {string.Join(",", outcomes)}", jobCorrelationId);
                await context.CompleteActivityWithOutcomesAsync(outcomes);
                return;
            }

            // 异步执行：创建 Elsa 书签，释放线程，等待外部唤醒
            var bookmarkArgs = new CreateBookmarkArgs
            {
                BookmarkId = bookmarkId,
                Callback = OnResumeAsync
            };
            context.CreateBookmark(bookmarkArgs);
            // 线程在此释放，Elsa 持久化活动状态
            Log.Information($"书签已创建，线程释放: {bookmarkId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("error:" + ex.Message);
            CLog.Error("error:" + ex.Message);
        }
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        Log.Information($"书签恢复，继续执行: {context.Activity.Name}");
        try
        {
            var serviceProvider = context.GetRequiredService<IServiceProvider>();
            var MainApiClient = new MainApiClient(serviceProvider);

            // 从 PDZ 读取插头执行结果（由 ReportExecuteResult 存储）
            var jobCorrelationId = context.WorkflowExecutionContext.CorrelationId;
            var pdzId = jobCorrelationId.EndsWith("Child") ? jobCorrelationId.Replace("Child", "") : jobCorrelationId;
            var pdz = await MainApiClient.GetPDZByPDZIdAsync(pdzId);

            var outcomeStr = pdz?.GetActivityOutcome(context.Activity.Id);
            var outcomes = string.IsNullOrEmpty(outcomeStr) ? ["Done"] : outcomeStr.Split('|');

            Log.Information($"恢复完成，Outcome: {string.Join(",", outcomes)}");
            await context.CompleteActivityWithOutcomesAsync(outcomes);
        }
        catch (Exception ex)
        {
            CLog.Error($"OnResumeAsync error: {ex}");
            await context.CompleteActivityWithOutcomesAsync(["Done"]);
        }
    }
}
