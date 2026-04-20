using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services to the container.
services.AddElsa();

// Build the service container.
var serviceProvider = services.BuildServiceProvider();

// Define a simple workflow that writes a message to the console.
//var workflow = new WriteLine("Hello world!");
var workflow = new Sequence
{
    Activities =
    {
        new WriteLine("Hello World!"),
        new WriteLine("Goodbye cruel world...")
    }
};

// Resolve a workflow runner to execute the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Execute the workflow.
await workflowRunner.RunAsync(workflow);

//var workflowRuntime = serviceProvider.GetRequiredService<IWorkflowRuntime>();
//var result = await workflowRuntime.StartWorkflowAsync("39d6dad381a7b185");