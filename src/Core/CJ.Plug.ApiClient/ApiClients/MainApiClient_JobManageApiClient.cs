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

public partial class MainApiClient : IJobManageApiClient
{
    public async Task<BaseJob?> CreateJobAsync(BaseJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.CreateJobAsync(request, ct);
    public async Task<ProcessJob?> CreateJobAsync(ProcessJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.CreateJobAsync(request,ct);
    public async Task<ToolJob?> CreateToolJobAsync(ToolJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.CreateToolJobAsync(request,ct);
    public async Task<bool> DeleteJobAsync(BaseJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.DeleteJobAsync(request,ct);
    public async Task<IEnumerable<BaseJob?>> GetAllJobsAsync(CancellationToken ct = default)=>await JobManageApiClient.Value.GetAllJobsAsync(ct);
    public async Task<BaseJob?> GetJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default)=>await JobManageApiClient.Value.GetJobByCorrelationIdAsync(CorrelationId, ct);
    public async Task<BaseJob?> GetJobByDefinitionIdAsync(string DefinitionId, CancellationToken ct = default)=>await JobManageApiClient.Value.GetJobByDefinitionIdAsync(DefinitionId, ct);
    public async Task<List<BaseJob>?> GetJobsByFilter(JobFilter filter, CancellationToken ct = default)=>await JobManageApiClient.Value.GetJobsByFilter(filter, ct);
    public async Task<ProcessJob?> GetProcessJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default)=>await JobManageApiClient.Value.GetProcessJobByCorrelationIdAsync(CorrelationId,ct);
    public async Task<ToolJob?> GetToolJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default) => await JobManageApiClient.Value.GetToolJobByCorrelationIdAsync(CorrelationId, ct);
    public async Task<ExecuteResultData?> GetToolJobResultByCorrelationIdAsync(string? CorrelationId, CancellationToken ct = default) => await JobManageApiClient.Value.GetToolJobResultByCorrelationIdAsync(CorrelationId, ct);
    public async Task<List<ToolJob>?> GetToolJobsByParentJobAsync(string ParentJobCorrelationId, CancellationToken ct = default)=>await JobManageApiClient.Value.GetToolJobsByParentJobAsync(ParentJobCorrelationId, ct);
    public async Task<ExecuteResultData?> SubmitNewToolExecute(string stationIp, PlugExecutionRequest request)=>await JobManageApiClient.Value.SubmitNewToolExecute(stationIp, request);
    public async Task SyncJournalData(string JobCorrelationId, CancellationToken ct = default)=>await JobManageApiClient.Value.SyncJournalData(JobCorrelationId, ct);
    public async Task<BaseJob?> UpdateJobAsync(BaseJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.UpdateJobAsync(request, ct);
    public async Task<ProcessJob?> UpdateProcessJobAsync(ProcessJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.UpdateProcessJobAsync(request, ct);
    public async Task<ToolJob?> UpdateToolJobAsync(ToolJob request, CancellationToken ct = default)=>await JobManageApiClient.Value.UpdateToolJobAsync(request, ct);
}




