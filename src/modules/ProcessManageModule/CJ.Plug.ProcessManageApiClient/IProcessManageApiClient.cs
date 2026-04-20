using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Shared;
using System.Net.Http.Json;

namespace CJ.Plug.ProcessManageApiClient
{
    public interface IProcessManageApiClient
    {
        Task<Process> CreateNewWorkflow(Process newWorkflow, CancellationToken cancellationToken = default);
        Task<Process[]> GetWorkflowsAsync(int maxItems = 20, CancellationToken cancellationToken = default);
        Task<bool> UpdateProcessAsync(int? workflowId, Process workflow, CancellationToken cancellationToken = default);
    }
}
