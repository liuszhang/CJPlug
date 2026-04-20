using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Serilog;

public class WorkflowFinishedHandler() : INotificationHandler<WorkflowFinished>
{
    public async Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
    {
        Log.Information("Workflow finished: {WorkflowId}", notification.Workflow.Id);
        Console.WriteLine("Workflow finished: {WorkflowId}", notification.Workflow.Id);
    }
}