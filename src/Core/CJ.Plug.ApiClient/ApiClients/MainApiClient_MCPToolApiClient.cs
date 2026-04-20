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
using CJ.Plug.UserManageApiClient;
using CJ.Plug.MCPToolApiClient;
using CJ.Plug.Models.MCPTools;

public partial class MainApiClient : IMCPToolApiClient
{
    public Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)=>MCPToolApiClient.Value.CreateMCPToolAsync(request, cancellationToken);

    public Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default)=>MCPToolApiClient.Value.DeleteMCPToolAsync(toolId, cancellationToken);
    public Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default)=>MCPToolApiClient.Value.GetAllMCPToolsAsync(cancellationToken);

    public Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)=>MCPToolApiClient.Value.UpdateMCPToolAsync(request, cancellationToken);
}




