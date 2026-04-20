//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.ApiClient.Contracts;
using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.Models.Relation;
using Microsoft.Extensions.DependencyInjection;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.TASApiClient;
using CJ.Plug.StationAndToolApiClient;
using CJ.Plug.ProcessManageApiClient;

public partial class MainApiClient : IProcessManageApiClient
{
    public async Task<Process> CreateNewWorkflow(Process newWorkflow, CancellationToken cancellationToken = default)=>await ProcessManageApiClient.Value.CreateNewWorkflow(newWorkflow, cancellationToken);
    public async Task<Process[]> GetWorkflowsAsync(int maxItems = 20, CancellationToken cancellationToken = default)=>await ProcessManageApiClient.Value.GetWorkflowsAsync(maxItems, cancellationToken);
    public async Task<bool> UpdateProcessAsync(int? workflowId, Process workflow, CancellationToken cancellationToken = default)=>await ProcessManageApiClient.Value.UpdateProcessAsync(workflowId, workflow, cancellationToken);
}




