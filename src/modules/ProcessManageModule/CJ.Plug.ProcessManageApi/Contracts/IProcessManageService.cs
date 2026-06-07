using CJ.Plug.Models.Contracts;

using CJ.Plug.Models.PlugProcess;
using System.Text.Json.Nodes;

public interface IProcessManageService : IBaseRepositoryService<Process, int>
{
    Task<Process> CreateWorkflowAsync(Process request, CancellationToken cancellationToken = default);
    //Task<bool> DeleteWorkflowAsync(int id);
    Task<IEnumerable<Process>> GetAllWorkflowsAsync(string? userName = null, CancellationToken cancellationToken = default);
    Task<Process?> GetWorkflowById(int id);
    Task<JsonObject> GetWorkflowJsonAsync(int? Id, CancellationToken cancellationToken = default);
}