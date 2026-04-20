using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using FastEndpoints;
using MediatR;
using Refit;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public partial class ElsaApiClient
{
    public async Task<ListResponse<PlugExecutionRecordSummary>> ListExecutionSummariesAsync(
    string workflowInstanceId,
    string activityNodeId,
    bool? completed = null, // 添加可选参数
    CancellationToken cancellationToken = default)
    {
        // 构建查询参数
        var queryParams = new Dictionary<string, string>
        {
            ["WorkflowInstanceId"] = workflowInstanceId,
            ["ActivityNodeId"] = "Workflow1:" + activityNodeId
        };

        // 添加可选参数（如果有值）
        if (completed.HasValue)
            queryParams["Completed"] = completed.Value.ToString();

        // 构建完整 URL
        var queryString = string.Join("&", queryParams.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var url = new Uri($"/elsa/api/activity-execution-summaries/list?{queryString}", UriKind.Relative);

        try
        {
            // 发送 GET 请求并解析响应
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode(); // 检查 HTTP 状态码
            // 直接反序列化为 ListResponse<ActivityExecutionRecordSummary>
            var result = await response.Content.ReadAsStringAsync();
            //Log.Information(result);
            var list= JsonSerializer.Deserialize<ListResponse<PlugExecutionRecordSummary>>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // 忽略大小写
            });
            //Log.Information($"找到记录数：{list?.Items.Count}，总数：{list?.Count}");
            return list ?? new ListResponse<PlugExecutionRecordSummary>(new List<PlugExecutionRecordSummary>(), 0);
        }
        catch (Exception e)
        {
            Console.WriteLine($"请求出错: {e.Message}");
            // 返回空响应或抛出异常，根据你的需求决定
            return new ListResponse<PlugExecutionRecordSummary>(new List<PlugExecutionRecordSummary>(), 0);
        }
    }


    public async Task<ProcessInstanceSummary?> GetWorkflowInstanceByCorrelationIdAsync(string CorrelationId)
    {
        var instances = await ListAllInstanceAsync();
        return instances.Items.FirstOrDefault(i => i.CorrelationId == CorrelationId);
    }


    public async Task<PagedListResponse<ProcessInstanceSummary>> ListAllInstanceAsync(ListWorkflowInstancesRequest? request=null, CancellationToken cancellationToken = default(CancellationToken))
    {
        var url = new Uri($"/elsa/api/workflow-instances", UriKind.Relative);
        request = request?? new ListWorkflowInstancesRequest();
        var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        //Log.Information(result);
        var pagedList = JsonSerializer.Deserialize<PagedListResponse<ProcessInstanceSummary>>(result, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // 忽略大小写
        });
        return pagedList ?? new PagedListResponse<ProcessInstanceSummary>();
    }

    public async Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string workflowInstanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default)
    {
        var url = new Uri($"/elsa/api/workflow-instances/{workflowInstanceId}/journal", UriKind.Relative);
        var request = new GetFilteredJournalRequest
        {
            Filter = filter
        };
        var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedListResponse<WorkflowExecutionLogRecord>>();
        //Log.Information(result);
        return result ?? new PagedListResponse<WorkflowExecutionLogRecord>();
    }
}

