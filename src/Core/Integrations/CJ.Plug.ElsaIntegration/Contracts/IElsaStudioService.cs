using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Plug;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Workflows.Runtime;
using ExecuteWorkflowResult = Elsa.Studio.Workflows.Domain.Models.ExecuteWorkflowResult;
using Elsa.Studio.DomInterop.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;

namespace CJ.Plug.ElsaIntegration.Contracts
{
    public interface IElsaStudioService
    {
        /// <summary>
        /// 通过插头创建一个引擎工作流，用于执行和流程图展示
        /// </summary>
        /// <param name="Plug"></param>
        /// <returns></returns>
        Task<WorkflowDefinition> CreateOrUpdateWorkflowFromProcess(CJ.Plug.Models.Plug.Plug Plug);
        /// <summary>
        /// 通过定义ID删除流程
        /// </summary>
        /// <param name="WorkflowDefinitionId"></param>
        /// <returns></returns>
        Task<bool> DeleteWorkflowByDefinitionId(string WorkflowDefinitionId);
        Task<WorkflowDefinition?> FindWorkflowDefinitionByDefinitionId(string WorkflowDefinitionId);

        /// <summary>
        /// 通过插头查找流程定义
        /// </summary>
        /// <param name="Plug"></param>
        /// <returns></returns>
        Task<WorkflowDefinition?> FindWorkflowDefinitionByPlug(CJ.Plug.Models.Plug.Plug Plug);

        /// <summary>
        /// 执行流程
        /// </summary>
        /// <param name="definitionId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ExecuteWorkflowResult> ExecuteAsync(CJ.Plug.Models.Plug.Plug plug,string? userName=null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// 使用指定ID执行流程
        /// </summary>
        Task<ExecuteWorkflowResult> ExecuteWithCorrelationIdAsync(CJ.Plug.Models.Plug.Plug plug,string CorrelationId, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// 保存流程
        /// </summary>
        /// <param name="workflowDefinition"></param>
        /// <param name="publish"></param>
        /// <param name="workflowSavedCallback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default(CancellationToken));


        

        Task LoginToElsaEngine();


        Task<IEnumerable<ActivityDescriptor>> GetElsaActivityDescriptors();

        Task<WorkflowInstanceSummary?> GetWorkflowInstanceByCorrelationId(string CorrelationId);
        Task<WorkflowInstanceSummary?> GetWorkflowInstanceByInstanceId(string InstanceId);

        Task<List<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default);


        //Task ResumeFromBookmark(BookmarkFilter filter);

        Task<List<ActivityExecutionRecordSummary>> GetActivityExecutionRecords(string workflowInstanceId, string activityNodeId);

    }
}
