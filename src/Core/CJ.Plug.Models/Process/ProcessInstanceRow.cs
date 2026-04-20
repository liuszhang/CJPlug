using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;

namespace CJ.Plug.Models.PlugProcess
{
    public record ProcessInstanceRow(
        string WorkflowInstanceId,
        string? CorrelationId,
        ProcessDefinitionSummary? WorkflowDefinition,
        Process? Process,
        int Version,
        string? Name,
        JobStatus? Status,
        JobSubStatus? SubStatus,
        int IncidentCount,
        DateTimeOffset? CreatedAt,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? FinishedAt,
        IDictionary<string, PlugStatus>? PlugStats
        //Elsa.Api.Client.Resources.WorkflowInstances.Models.WorkflowInstance WorkflowInstance
    );
}
