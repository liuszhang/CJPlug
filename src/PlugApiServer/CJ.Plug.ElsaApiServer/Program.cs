using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Login;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJ.Plug.TASApiClient;
using Elsa.Agents;
using Elsa.Extensions;
using Elsa.Studio.Agents.UI.Pages;
using Elsa.Studio.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using Serilog;
using System.Text;
using System.Text.Json;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.EnableMultiplexing", false);

// 设置 .NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(new SignalRLogSink("Elsa"))
    .CreateLogger();

builder.Services.ConfigModuleApiServices();

builder.Services.AddSignalR();

//添加Elsa相关服务
builder.AddElsaServicesForApi();

builder.Services.AddSingleton<MainApiClient>();
//builder.Services.AddHttpClient<MainApiClient>(client =>
//{
//    client.BaseAddress = new(GlobalData.MainDispatcherServer);
//    client.Timeout = TimeSpan.FromSeconds(60);
//});



var HubConnectionManagerService =new HubConnectionManagerService();
builder.Services.AddSingleton<HubConnectionManagerService>(HubConnectionManagerService);

HubConnectionManagerService._hubConnection.On<string>("ResumeElsaProcess", async (bookmarkInfo) =>
{
    Console.WriteLine($"ReceiveLog:{bookmarkInfo}");
    
    try
    {
        var Info = bookmarkInfo.Split(':')[1];
        var instanceId = Info.Split("-")[0];
        var DefinitionId = Info.Split("-")[1];
        var CorrelationId = Info.Split("-")[2];

        var bookmarkId = CorrelationId + DefinitionId;
        Log.Information($"instanceId:{instanceId}");
        Log.Information($"DefinitionId:{DefinitionId}");
        Log.Information($"CorrelationId:{CorrelationId}");

        //方式一：通过workflowclient恢复
        var _workflowRuntime = builder.Services.BuildServiceProvider().GetRequiredService<IWorkflowRuntime>();
        //var result = await _workflowRuntime.ResumeWorkflowAsync(instanceId);
        var client = await _workflowRuntime.CreateClientAsync(instanceId);
        //var state = await client.ExportStateAsync();
        //Log.Information("state:" + JsonSerializer.Serialize(state.Status.ToString()));
        //Log.Information("subState:" + JsonSerializer.Serialize(state.SubStatus.ToString()));
        var request = new RunWorkflowInstanceRequest();
        request.BookmarkId = bookmarkId;
        //request.TriggerActivityId = bookmarkId;
        //request.ActivityHandle = ActivityHandle.FromActivityId(DefinitionId);
        //request.Properties = new Dictionary<string, object>();
        //request.Input = new Dictionary<string, object>();
        //var requ = RunWorkflowInstanceRequest.Empty;
        ////requ.BookmarkId = bookmarkId;
        Log.Information($"准备恢复流程执行:{client.WorkflowInstanceId}({await client.InstanceExistsAsync()})");
        await client.RunInstanceAsync(request);
        return; // 如果恢复成功，直接返回

        //方式二：通过bookmarkQueue恢复
        //if (bookmarkQueue == null)
        //{
        //    CLog.Error("bookmarkQueue is null");
        //    return;
        //}
        var bookmarkQueue= builder.Services.BuildServiceProvider().GetRequiredService<IBookmarkQueue>();
        var bookmarkQueueItem = new NewBookmarkQueueItem() {CorrelationId= instanceId };
        //await Task.Delay(1000);
        Log.Information($"bookmarkQueue准备恢复流程执行");
        //var hasher = builder.Services.BuildServiceProvider().GetRequiredService<IStimulusHasher>();
        //var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<CommonCorePlugActivity>();
        //Log.Information($"activityTypeName:{activityTypeName}");
        //var stimulus = bookmarkId;
        //var bookmarkQueueItem = new NewBookmarkQueueItem()
        //{
        //    //WorkflowInstanceId = instanceId,
        //    //BookmarkId = bookmarkId,
        //    //Options = new ResumeBookmarkOptions { },
        //    StimulusHash = hasher.Hash(activityTypeName, bookmarkId, null),
        //    //ActivityInstanceId = "",
        //    ActivityTypeName = activityTypeName
        //};
        //Log.Information(bookmarkQueueItem.StimulusHash);
        //await Task.Delay(10000);
        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem);

        //方式三：通过bookmarkResumer恢复
        //var resumer = builder.Services.BuildServiceProvider().GetRequiredService<IBookmarkResumer>();
        //var filter = new BookmarkFilter();
        ////filter.BookmarkId = bookmarkId;
        ////filter.WorkflowInstanceId = instanceId;
        //filter.CorrelationId= instanceId;

        ////var request = new ResumeBookmarkRequest();
        ////request.WorkflowInstanceId = instanceId;
        ////request.BookmarkId = bookmarkId;
        ////request.ActivityHandle = null;
        ////request.Properties = null;
        ////request.Input = null;

        //Log.Information($"resumer准备恢复流程执行");
        //var result = await resumer.ResumeAsync(filter,new ResumeBookmarkOptions { Input=null,Properties=null});


        Log.Information($"恢复流程执行:{instanceId}");
    }
    catch (Exception ex)
    {
        CLog.Error($"继续执行流程失败：{ex.Message}");
        CLog.Error($"继续执行流程失败：{ex.StackTrace}");
        //CLog.Error($"继续执行流程失败：{ex.InnerException}");
    }
});

HubConnectionManagerService._hubConnection.On<string>("CompleteActivityContext", async (activityContext) =>
{
    Console.WriteLine($"Receive CompleteActivityContext:{activityContext ?? "(null)"}");
    try
    {
        if (string.IsNullOrEmpty(activityContext))
        {
            Log.Warning("CompleteActivityContext 收到空消息");
            return;
        }

        // 消息格式: correlationId|plugId
        var parts = activityContext.Split('|');
        if (parts.Length < 2)
        {
            Log.Warning($"CompleteActivityContext 消息格式错误: {activityContext}");
            return;
        }
        var correlationId = parts[0];
        var plugId = parts[1];
        var bookmarkId = correlationId + plugId;
        Log.Information($"准备恢复书签 [{bookmarkId}]，CorrelationId={correlationId}");

        // 1. 通过 IWorkflowInstanceStore 查询工作流实例
        var sp = builder.Services.BuildServiceProvider();
        Log.Information($"DI resolved, searching instance by CorrelationId={correlationId}");
        var instanceStore = sp.GetRequiredService<Elsa.Workflows.Management.IWorkflowInstanceStore>();
        var filter = new Elsa.Workflows.Management.Filters.WorkflowInstanceFilter { CorrelationId = correlationId };
        var instances = await instanceStore.FindManyAsync(filter);
        var instance = instances?.FirstOrDefault();
        Log.Information($"FindMany returned: {(instance == null ? "null" : $"{instance.Id} (total:{instances?.Count() ?? 0})")}");

        if (instance == null)
        {
            Log.Warning($"未找到 CorrelationId={correlationId} 的工作流实例");
            return;
        }

        // 2. 通过 IBookmarkResumer 直接恢复书签（避免 RunInstanceAsync 触发工作流重新反序列化）
        var resumer = sp.GetRequiredService<IWorkflowResumer>();
        var resumeFilter = new BookmarkFilter
        {
            BookmarkId = bookmarkId,
            WorkflowInstanceId = instance.Id,
            CorrelationId = correlationId
        };
        var options = new ResumeBookmarkOptions { Input = null, Properties = null };
        Log.Information($"通过 BookmarkResumer 恢复书签: InstanceId={instance.Id}, BookmarkId={bookmarkId}");
        var result = await resumer.ResumeAsync(resumeFilter, options);
        Log.Information($"书签恢复{(result.ToString() == "True" ? "成功" : "失败(Matched=false)")}: {bookmarkId}；result={result}");
    }
    catch (Exception ex)
    {
        Log.Error(ex, $"CompleteActivityContext 处理失败: {ex.Message}");
        Log.Error($"CompleteActivityContext StackTrace: {ex.StackTrace}");
        if (ex.InnerException != null)
            Log.Error($"CompleteActivityContext Inner: {ex.InnerException.Message}");
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure web application's middleware pipeline.
app.UseCors();
app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();

//添加Elsa相关API
app.UseElsaEndpoints();

//启用 NSwag 和 Swagger UI
app.UseOpenApi();
app.UseSwaggerUi();

//app.UseHttpsRedirection();

app.Run();
