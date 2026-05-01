using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Login;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJ.Plug.TASApiClient;
using Elsa.Agents;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
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
