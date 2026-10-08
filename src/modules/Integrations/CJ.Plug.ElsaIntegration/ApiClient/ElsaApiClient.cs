using CJ.Plug.ElsaIntegration;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var engineBaseUrl = DispatcherClient.GetStringAsync("api/dispatch/GetElsaEngineServer").Result;
        //Console.WriteLine("[ElsaApiClient]elsa engine server to use is:" + engineBaseUrl);

        // ⚠ 2026-10-08：身份通道由 ApiKey 改为管理员 JWT。
        //   Elsa 3.9 起 AdminApiKeyProvider 的语义是「未显式配置 = 全拒」（UseAdminApiKey() 无参重载的文档原话
        //   "The provider denies all keys unless configured."），而这里原先只带
        //   Authorization: ApiKey <GlobalData.ElsaEngineApiKey>（硬编码全零 GUID）⇒ 引擎每个管理端点都回 401，
        //   且保存/执行两处不检查状态码 ⇒ 静默失败（实例 ID 为 null → journal 空段 URL 404 → 编辑器整页 ErrorBoundary）。
        //   实机实测：ApiKey 全零 → 401；同一管理员账号的 JWT → 200。JWT 凭据与引擎侧
        //   identity.UseAdminUserProvider(...) 同源（ElsaAdminCredentialResolver），独立进程算出的口令一致。
        var configuration = serviceProvider.GetService<IConfiguration>();
        httpClient = new HttpClient(new ElsaJwtAuthHandler(
            engineBaseUrl,
            ElsaAdminCredentialResolver.ResolveUserName(configuration),
            ElsaAdminCredentialResolver.ResolvePassword(configuration)));
        httpClient.BaseAddress = new Uri(engineBaseUrl);

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

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        }
        catch (Exception e)
        {
            // 传输层失败（引擎未启动/连接被拒）：明确抛出，绝不返回假的 500 让上层把"没保存"当成"已保存"继续执行。
            CLog.Error($"保存流程定义失败（请求未送达）：POST {url} → {e.Message}");
            throw new InvalidOperationException($"保存流程定义失败（请求未送达 {url}）：{e.Message}", e);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await DescribeFailureAsync(response, cancellationToken);
            response.Dispose();
            CLog.Error($"保存流程定义失败：POST {url} → {detail}");
            throw new InvalidOperationException($"保存流程定义失败：POST {url} → {detail}");
        }

        return response;
    }

    /// <summary>
    /// 把一次失败响应压成单行诊断文本（状态码 + 原因短语 + 截断的响应体）。
    /// 原先保存/执行路径完全不看状态码，401/400 被静默吞掉，最终只能看到下游
    /// 「journal 空段 URL 404」这种与真因无关的症状。
    /// </summary>
    private static async Task<string> DescribeFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var summary = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
                summary += $"；响应体：{Truncate(body.Trim(), 500)}";
        }
        catch
        {
            // 读不出响应体不影响诊断文本。
        }

        return summary;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

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

        // 执行被拒（401 未授权 / 404 定义不存在 / 400 请求模型不匹配）时必须响亮失败：
        // 原先不看状态码 ⇒ 实例 ID 为 null 却继续往下走，最终以「journal 空段 URL 404」的形式
        // 冒到页面 ErrorBoundary，真因被掩盖。
        if (!response.IsSuccessStatusCode)
        {
            var detail = await DescribeFailureAsync(response, cancellationToken);
            response.Dispose();
            var message = $"触发流程执行失败（DefinitionId={definitionId}）：POST {url} → {detail}";
            CLog.Error(message);
            throw new InvalidOperationException(message);
        }

        var workflowInstanceId = response.Headers.TryGetValues("x-elsa-workflow-instance-id", out var workflowInstanceIdValues) ? workflowInstanceIdValues.FirstOrDefault() : null;
        var cannotStart = string.Equals(response.Headers.TryGetValues("x-elsa-workflow-cannot-start", out var cannotStartValues) ? cannotStartValues.FirstOrDefault() : null, "true", StringComparison.OrdinalIgnoreCase);
        //Log.Information($"引擎响应workflowInstanceId：{workflowInstanceId}");
        //Log.Information($"引擎状态cannotStart：{cannotStart}");

        if (string.IsNullOrWhiteSpace(workflowInstanceId) && !cannotStart)
        {
            var message = $"触发流程执行失败（DefinitionId={definitionId}）：引擎返回 {(int)response.StatusCode} 但未带 x-elsa-workflow-instance-id 头，" +
                          "无法追踪工作流实例（请检查引擎侧流程定义是否保存成功、身份通道是否可用）。";
            CLog.Error(message);
            throw new InvalidOperationException(message);
        }

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
        if (job == null)
        {
            // 作业没落库（ApiServer 侧异常）时给出明确报错，而不是在下面访问 job.JobCorrelationId 抛 NullReferenceException。
            CLog.Error($"执行结束，但未按关联 ID 找到流程作业（CorrelationId={ExecuteSetting.CorrelationId}），引擎实例={result.WorkflowInstanceId}，作业数据未回写");
            return result;
        }

        CLog.Information($"执行结束，更新引擎作业ID（{result.WorkflowInstanceId}）至流程作业ID（{job.JobCorrelationId}）中", job.JobCorrelationId);
        job.EngineInstanceId = result.WorkflowInstanceId;

        // ⚠ 实例 ID 为空（cannot-start 场景）时绝不能去拼 journal URL：会拼出 /workflow-instances//journal，
        //   路由匹配不上 → 404 → HttpRequestException，真因被一个假 404 掩盖（2026-10-08 实机踩到）。
        var journalData = string.IsNullOrWhiteSpace(result.WorkflowInstanceId)
            ? new List<Elsa.Api.Client.Resources.WorkflowInstances.Models.WorkflowExecutionLogRecord>()
            : (await GetJournalAsync(result.WorkflowInstanceId)).Items.ToList();
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

