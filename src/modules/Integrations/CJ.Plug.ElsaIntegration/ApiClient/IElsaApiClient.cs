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
        Task DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
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
