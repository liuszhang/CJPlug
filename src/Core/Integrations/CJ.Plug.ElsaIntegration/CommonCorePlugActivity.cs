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
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using YamlDotNet.Core.Tokens;

//[Activity("Demo6666", "Demo6666", "Simple activity6666666666666 ")]
//[Activity(Namespace = "Demo", Category = "Demo", DisplayName = "测试活动", Description = "A simple activity that writes \"Hello World!\" to the console.")]
//[Activity(Namespace ="CJ",Description ="核心插头活动",Category ="商业软件")]
[Activity("CJ", "商业软件", "核心插头活动")]
//[FlowNode("Pass", "Fail")]
public class CommonCorePlugActivity : CodeActivity
{
    //[Input(IsBrowsable = false, Description = "输入插头ID")]
    //public Input<string?> PlugDefinitionId { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // 从活动上下文获取 IServiceProvider
            var serviceProvider = context.GetRequiredService<IServiceProvider>();

            bool IsCompleted = false;

            var client = new HttpClient
            {
                //BaseAddress = new Uri(GlobalData.MainApiServer)
                //修改为通过调度层分配API服务器
                BaseAddress = new Uri(GlobalData.MainDispatcherServer)
            };
            var MainApiClient = new MainApiClient(serviceProvider);

            var HubConnectionManagerService = new HubConnectionManagerService();
            await HubConnectionManagerService.ConnectAsync();
            //Log.Information("Hub链接成功");
            HubConnectionManagerService._hubConnection.Remove("CompleteActivityContext");
            HubConnectionManagerService._hubConnection.On<string>("CompleteActivityContext", async (ActivityContext) =>
            {
                ActivityContext= ActivityContext.Split(':')[1];
                if (ActivityContext== context.WorkflowExecutionContext.CorrelationId+ context.Activity.Id)
                {
                    //Log.Information($"receive ActivityContext:{ActivityContext}");
                    //await OnResumeAsync(context);
                    //await context.CompleteActivityAsync();
                    //Log.Information($"组件执行完成：{context.IsCompleted}");
                    //Log.Information($"1组件执行完成：{context.Activity.Name}");
                    IsCompleted = true;
                    
                }
                else
                {
                    //Log.Information($"receive ActivityContext:{ActivityContext},but not current ID:{context.WorkflowExecutionContext.CorrelationId + context.Activity.Id}");
                }
            });

            CLog.Information($"开始执行插头：{context.Activity.Name}", context.WorkflowExecutionContext.CorrelationId);

            //Console.WriteLine("----------------------666666666666,prepare to execute :" + JsonSerializer.Serialize(context.Activity.Id));
            //Console.WriteLine("----------------------666666666666,prepare to execute :" + JsonSerializer.Serialize(context.WorkflowExecutionContext.CorrelationId));
            var jobCorrelationId = context.WorkflowExecutionContext.CorrelationId;
            var bookmarkId = context.WorkflowExecutionContext.CorrelationId+context.Activity.Id;
            
            
            var request=new PlugExecutionRequest
            {                
                ExecuteMode = ExecuteMode.Plug,
            };
            request.ExecuteResultData.Ids = new ExecuteIdsBundle
            {
                JobCorrelationId = context.WorkflowExecutionContext.CorrelationId,
                PDZId = context.WorkflowExecutionContext.CorrelationId.EndsWith("Child")? context.WorkflowExecutionContext.CorrelationId.Replace("Child",""): context.WorkflowExecutionContext.CorrelationId,
                PlugDefinitionId = context.Activity.Id
            };



            Console.WriteLine("============CREATE BOOKMARK,WAIT FOR EXECUTING...============");

            //await MainApiClient.ExecutePlug(request);


            CreateBookmarkArgs createBookmarkArgs = new CreateBookmarkArgs();
            createBookmarkArgs.BookmarkId = bookmarkId;
            createBookmarkArgs.Callback=OnResumeAsync;
            //createBookmarkArgs.Stimulus = bookmarkId;
            //createBookmarkArgs.BookmarkName = bookmarkId;
            //createBookmarkArgs.IncludeActivityInstanceId = false;
            //createBookmarkArgs.AutoComplete = true;
            //暂时先不创建书签，目前引擎恢复书签时有BUG
            //context.CreateBookmark(createBookmarkArgs);
            //context.CreateBookmark();
            


            foreach (var b in context.Bookmarks)
            {
                //Log.Information($"书签:{b.Id}");
                //Log.Information(JsonSerializer.Serialize(b));
            }

            var erd = await MainApiClient.ExecutePlug(request);
            CLog.Information($"执行输出：{string.Join("|",erd?.Outcome)}", jobCorrelationId);
            //await MainApiClient.SyncJournalData(erd.Ids.JobCorrelationId);

            if (erd.Outcome.Length > 0)
            {
                CLog.Information("直接根据输出结束组件执行。", jobCorrelationId);
                await context.CompleteActivityWithOutcomesAsync(erd?.Outcome);
                return;
            }
            //无限循环等待执行完成
            while (!IsCompleted)
            {
                //Log.Information($"等待组件执行完成，当前书签数量：{context.Bookmarks.Count}");
                //return;
            }
            HubConnectionManagerService._hubConnection.Remove("CompleteActivityContext");
            Log.Information($"组件执行完成：{context.Activity.Name}");

            //await MainApiClient.SyncJournalData(context.WorkflowExecutionContext.CorrelationId);

            await context.CompleteActivityWithOutcomesAsync(erd?.Outcome??["Done"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine("error:" + ex.Message);
            CLog.Error("error:" + ex.Message);
            //await context.CompleteActivityWithOutcomesAsync("失败");
        }
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        Log.Information("=======88888888888=====RESUME FROM BOOKMARK============");
        foreach (var b in context.Bookmarks)
        {
            //Log.Information($"书签:{b.Id}");
        }
        try
        {
            await context.CompleteActivityAsync();
            Log.Information(context.IsCompleted.ToString());
        }
        catch(Exception ex)
        {
            CLog.Error(ex.ToString());
            //throw;
        }
        //var status = new ActivityStats();
        //status.Blocked = false;
        //status.Completed = 1;
        //Log.Information($"{context.Activity.Id}|{JsonSerializer.Serialize(status)}");
    }
}

