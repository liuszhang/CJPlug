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

public partial class MainApiClient:IRelationApiClient

{

    public async Task<CommonRelation?> CreateOrUpdateRelationAsync(CommonRelation request, CancellationToken cancellationToken = default)=> await RelationApiClient.Value.CreateOrUpdateRelationAsync(request, cancellationToken);
    public async Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)=>await RelationApiClient.Value.DeleteRealationAsync(request, cancellationToken);
    public async Task<List<CommonRelation?>> GetAllRelations(CancellationToken cancellationToken = default)=>await RelationApiClient.Value.GetAllRelations(cancellationToken);
    public async Task<List<CommonRelation>?> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default)=>await RelationApiClient.Value.GetRealationByFilterAsync(filter, cancellationToken);
    public async Task<List<CommonRelation?>> GetRelationsByCategoryAsync(string Category, CancellationToken cancellationToken = default)=>await RelationApiClient.Value.GetRelationsByCategoryAsync(Category, cancellationToken);




}




