using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;

namespace CJ.Plug.ElsaIntegration.Contracts
{
    public interface IElsaEngineService
    {
        Task<WorkflowInstanceSummary?> GetWorkflowInstanceByCorrelationId(string CorrelationId);

        Task ResumeFromBookmark(ResumeBookmarkRequest request,Type? ActivityType);
        Task ResumeFromBookmark(BookmarkFilter filter);

        
    }
}
