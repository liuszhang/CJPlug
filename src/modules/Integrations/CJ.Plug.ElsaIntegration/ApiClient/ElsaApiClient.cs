using CJ.Plug.ElsaIntegration.ApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugDataZoneApiClient;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Workflows.Management.Services;
using FastEndpoints;
using MediatR;
using Refit;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public partial class ElsaApiClient : IElsaApiClient
{
    private HttpClient httpClient = new();
    private readonly HttpClient DispatcherClient;
    private MainApiClient MainApiClient;

    public ElsaApiClient(HttpClient dispatcherClient,IServiceProvider serviceProvider)
    {
        DispatcherClient = dispatcherClient;
        httpClient.BaseAddress = new Uri(DispatcherClient.GetStringAsync("api/dispatch/GetElsaEngineServer").Result);
        //Console.WriteLine("[ElsaApiClient]elsa engine server to use is:" + httpClient.BaseAddress?.ToString());
        var apiKey = DispatcherClient.GetStringAsync("api/dispatch/GetElsaEngineApiKey").Result;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

        MainApiClient = new MainApiClient(serviceProvider);
    }
    


    /// <summary>
    /// Reports a task as completed.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="result">The result of the task.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task ReportTaskCompletedAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var url = new Uri($"tasks/{taskId}/complete", UriKind.Relative);
        var request = new { Result = result };
        await httpClient.PostAsJsonAsync(url, request, cancellationToken);
    }
       

    private async Task<HttpResponseMessage> SaveWorkflowAsync(WorkflowDefinition workflowDefinition, bool publish, CancellationToken cancellationToken = default)
    {
        var url = new Uri($"/elsa/api/workflow-definitions", UriKind.Relative);

        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Id = workflowDefinition.Id,
                Description = workflowDefinition.Description,
                Name = workflowDefinition.Name,
                ToolVersion = workflowDefinition.ToolVersion,
                Inputs = workflowDefinition.Inputs,
                Options = workflowDefinition.Options,
                Outcomes = workflowDefinition.Outcomes,
                Outputs = workflowDefinition.Outputs,
                Variables = workflowDefinition.Variables.Select(x => new VariableDefinition
                {
                    Id = x.Id,
                    Name = x.Name,
                    TypeName = x.TypeName,
                    Value = x.Value?.ToString(),
                    IsArray = x.IsArray,
                    StorageDriverTypeName = x.StorageDriverTypeName
                }).ToList(),
                Version = workflowDefinition.Version,
                CreatedAt = workflowDefinition.CreatedAt,
                CustomProperties = workflowDefinition.CustomProperties,
                DefinitionId = workflowDefinition.DefinitionId,
                IsLatest = workflowDefinition.IsLatest,
                IsPublished = workflowDefinition.IsPublished,
                Root = workflowDefinition.Root
            },
            Publish = publish,
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Content = new StringContent(e.Message)
            };
        }
    }

    public async Task<HttpResponseMessage> SaveWorkflowFromPlugAsync(Plug Plug, bool publish, CancellationToken cancellationToken = default)
    {
        var workflowDefinition = new WorkflowDefinition()
        {
            DefinitionId = Plug.DefinitionId,
            Name = Plug.Name,
            Version = 1,
            IsLatest = true,             
            Root = Plug.ToActivityJson()
        };
        //workflowDefinition.Options.UsableAsActivity = Plug.ShowInPlugLibrary;
        //workflowDefinition.Options.ActivityCategory = Plug.GroupName;
        //workflowDefinition.Options.AutoUpdateConsumingWorkflows = false;

        await DeleteByDefinitionIdAsync(workflowDefinition.DefinitionId);

        var response = await SaveWorkflowAsync(workflowDefinition,publish);
        return response;        
    }


    private async Task<HttpResponseMessage> SaveWorkflowFromJsonAsync(JsonObject WorkflowJson, bool publish, CancellationToken cancellationToken = default)
    {
        var workflowDefinition = new WorkflowDefinition()
        {
            Id = WorkflowJson.GetId(),
            DefinitionId = WorkflowJson.GetId(),
            Name = WorkflowJson.GetName(),
            Version = 1,
            IsLatest = true,
            Root = WorkflowJson
        };
        //workflowDefinition.Options.UsableAsActivity = Plug.ShowInPlugLibrary;
        //workflowDefinition.Options.ActivityCategory = Plug.GroupName;
        //workflowDefinition.Options.AutoUpdateConsumingWorkflows = false;

        await DeleteByDefinitionIdAsync(workflowDefinition.DefinitionId);

        var response = await SaveWorkflowAsync(workflowDefinition, publish);
        return response;
    }



    public async Task<WorkflowDefinition?> GetByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        var url = new Uri($"/elsa/api/workflow-definitions/by-definition-id/{definitionId}?versionOptions={versionOptions}", UriKind.Relative);

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var workflowDefinition = await response.Content.ReadFromJsonAsync<WorkflowDefinition>(cancellationToken: cancellationToken);
                return workflowDefinition;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }



    public async Task DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default(CancellationToken))
    {
        var url = new Uri($"/elsa/api/workflow-definitions/{definitionId}", UriKind.Relative);
        try
        {
            var response = await httpClient.DeleteAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Workflow with ID {definitionId} deleted successfully.");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// 调用Elsa流程引擎的API接口进行流程执行
    /// </summary>
    /// <param name="definitionId"></param>
    /// <param name="ExecuteSetting"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteSetting ExecuteSetting, CancellationToken cancellationToken = default(CancellationToken))
    {
        var url = new Uri($"/elsa/api/workflow-definitions/{definitionId}/execute", UriKind.Relative);
        var request = new ExecuteWorkflowDefinitionRequest
        {
            //使用自定义的ID进行流程实例化跟踪，后续如果引擎支持自定义ID则可以去掉
            CorrelationId = ExecuteSetting.CorrelationId,
            VersionOptions = VersionOptions.Latest,
            TriggerActivityId = ExecuteSetting.TriggerActivityId,
        };
        //Log.Information($"触发活动ID：{request.TriggerActivityId}");
        // 构建 POST 请求
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"  // 媒体类型
        );
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = jsonContent
        };
        //var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        // 发送请求并仅等待头部返回
        HttpResponseMessage response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead
        );
        var workflowInstanceId = response.Headers.TryGetValues("x-elsa-workflow-instance-id", out var workflowInstanceIdValues) ? workflowInstanceIdValues.FirstOrDefault() : null;
        var cannotStart = string.Equals(response.Headers.TryGetValues("x-elsa-workflow-cannot-start", out var cannotStartValues) ? cannotStartValues.FirstOrDefault() : null, "true", StringComparison.OrdinalIgnoreCase);
        //Log.Information($"引擎响应workflowInstanceId：{workflowInstanceId}");
        //Log.Information($"引擎状态cannotStart：{cannotStart}");
        
        var result=new ExecuteWorkflowResult(workflowInstanceId,cannotStart);
        return result;
    }

    [Obsolete]
    public async Task<ExecuteWorkflowResult> ExecutePlugWithCorrelationIdAsync(Plug Plug, ExecuteSetting ExecuteSetting, CancellationToken cancellationToken = default(CancellationToken))
    {
        Log.Information(Plug.ToActivityJson().ToString());

        await SaveWorkflowFromPlugAsync(Plug, true, cancellationToken);

        //创建一条Job记录，用于后续作业追踪
        var job = new ProcessJob
        {
            //EngineInstanceId = executeResult.WorkflowInstanceId,
            JobCorrelationId = ExecuteSetting.CorrelationId,
            ProcessDefinitionId = Plug.DefinitionId,
            CreatedAt = DateTimeOffset.UtcNow.ToLocalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToLocalTime()
        };
        job = await MainApiClient.CreateJobAsync(job);
        //用CorrelationId创建一个PDZ，用于后续的执行数据承载
        await MainApiClient.GetOrCreateJobPDZ(ExecuteSetting.CorrelationId);

        var result = await ExecuteAsync(Plug.DefinitionId, ExecuteSetting, cancellationToken);

        //更新实例ID至Job记录
        job.EngineInstanceId = result.WorkflowInstanceId;
        await MainApiClient.UpdateJobAsync(job);

        return result;
    }

    /// <summary>
    /// prepare workflow data and ready to execute workflow
    /// </summary>
    /// <param name="WorkflowJson"></param>
    /// <param name="ExecuteSetting"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ExecuteWorkflowResult> PrepareAndExecuteWorkflow(JsonObject WorkflowJson, ExecuteSetting ExecuteSetting, CancellationToken cancellationToken = default(CancellationToken))
    {
        //Log.Information(WorkflowJson.ToString());

        await SaveWorkflowFromJsonAsync(WorkflowJson, true, cancellationToken);

        //创建一条Job记录，用于后续作业追踪
        //var job = new ProcessJob
        //{
        //    //EngineInstanceId = executeResult.WorkflowInstanceId,
        //    JobCorrelationId = ExecuteSetting.CorrelationId,
        //    ProcessDefinitionId = WorkflowJson.GetId(),
        //    CreatedAt = DateTimeOffset.UtcNow.ToLocalTime(),
        //    UpdatedAt = DateTimeOffset.UtcNow.ToLocalTime()
        //};
        //job = await MainApiClient.CreateJobAsync(job);
        ////用CorrelationId创建一个PDZ，用于后续的执行数据承载
        //await MainApiClient.GetOrCreateJobPDZ(ExecuteSetting.CorrelationId);
              
        var result = await ExecuteAsync(WorkflowJson.GetId(), ExecuteSetting, cancellationToken);

        //更新实例ID至Job记录
        var job=await MainApiClient.GetProcessJobByCorrelationIdAsync(ExecuteSetting.CorrelationId);
        CLog.Information($"执行结束，更新引擎作业ID（{result.WorkflowInstanceId}）至流程作业ID（{job.JobCorrelationId}）中", job.JobCorrelationId);
        job.EngineInstanceId = result.WorkflowInstanceId;        
        var journalData = (await GetJournalAsync(result.WorkflowInstanceId)).Items.ToList();
        job.JournalData = JsonSerializer.Serialize(journalData);
        job.JobStatus=JobStatus.完成.ToString();
        job.JobSubStatus = result.CannotStart ? JobSubStatus.出错.ToString() : JobSubStatus.已完成.ToString();
        await MainApiClient.UpdateProcessJobAsync(job);
        //await MainApiClient.SyncJournalData(job.JobCorrelationId);


        return result;
    }


    /// <summary>
    /// 20250601 计划统一使用json加执行设定的方式进行引擎工作流执行
    /// </summary>
    /// <param name="WorkflowJson"></param>
    /// <param name="ExecuteSetting"></param>
    /// <returns></returns>
    public async Task<ExecuteResultData> ExecuteWorkflowWithExecuteSetting(JsonObject WorkflowJson,ExecuteSetting ExecuteSetting)
    {
        var engineResult = await PrepareAndExecuteWorkflow(WorkflowJson, ExecuteSetting);

        return new ExecuteResultData
        {
            ExecuteResultMessage = engineResult.WorkflowInstanceId,
            ExecuteStatus = JobStatus.完成,
            ExecuteSubStatus = engineResult.CannotStart ? JobSubStatus.出错: JobSubStatus.已完成,
        };
    }

}

