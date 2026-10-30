using CJ.Plug.Models.Job;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace CJ.Plug.ElsaIntegration.ApiClient
{
    public interface IElsaApiClient
    {
        /// <summary>
        /// 删除指定 <paramref name="definitionId"/> 的全部版本。
        /// 返回 <c>true</c> 表示「删除成功」或「本来就不存在」（两者对调用方等价：定义已不在库里）；
        /// 返回 <c>false</c> 表示删除未生效（鉴权/方法不允许/引擎异常），调用方应记告警但<b>不阻断保存</b>
        /// —— 保存是按 <c>DefinitionId + Version</c> 的 upsert，删除只是「清掉历史版本」的可选前置。
        /// </summary>
        Task<bool> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<ExecuteWorkflowResult> ExecutePlugWithCorrelationIdAsync(Models.Plug.Plug Plug, ExecuteSetting ExecuteSetting, CancellationToken cancellationToken = default);
        Task<ExecuteResultData> ExecuteWorkflowWithExecuteSetting(JsonObject WorkflowJson, ExecuteSetting ExecuteSetting);
        Task<WorkflowDefinition?> GetByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default);
        Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string workflowInstanceId, JournalFilter? filter = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
        Task<ProcessInstanceSummary?> GetWorkflowInstanceByCorrelationIdAsync(string CorrelationId);
        Task<ICollection<ActivityDescriptor>> ListAllActivityDescriptorsAsync([Query] ListActivityDescriptorsRequest? request = null, CancellationToken cancellationToken = default);
        Task<PagedListResponse<ProcessInstanceSummary>> ListAllInstanceAsync(ListWorkflowInstancesRequest? request = null, CancellationToken cancellationToken = default);
        Task<ListResponse<PlugExecutionRecordSummary>> ListExecutionSummariesAsync(string workflowInstanceId, string activityNodeId, bool? completed = null, CancellationToken cancellationToken = default);
        Task<LoginResponse> LoginToElsaAsync([Body] LoginRequest? request = null, CancellationToken cancellationToken = default);
        Task ReportTaskCompletedAsync(string taskId, object? result = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> SaveWorkflowFromPlugAsync(Models.Plug.Plug Plug, bool publish, CancellationToken cancellationToken = default);
    }
}
