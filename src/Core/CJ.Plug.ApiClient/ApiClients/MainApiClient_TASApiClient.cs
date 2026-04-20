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
using CJ.Plug.Models.PlugAction;

public partial class MainApiClient : ITASApiClient
{
    public async Task AddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)=>await TASApiClient.Value.AddPlugActionToPlug(plug, plugAction, relationshipType);
    public async Task<PlugAction?> CreateAndAddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)=>await TASApiClient.Value.CreateAndAddPlugActionToPlug(plug,plugAction,relationshipType);
    public async Task<Plug?> CreateNewPlug(Plug newItem, CancellationToken cancellationToken = default)=>await TASApiClient.Value.CreateNewPlug(newItem, cancellationToken);
    public async Task<PlugAction?> CreatePlugAction(PlugAction newItem, CancellationToken cancellationToken = default)=>await TASApiClient.Value.CreatePlugAction(newItem, cancellationToken);
    public async Task<bool> DeletePlug(int? itemId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.DeletePlug(itemId, cancellationToken);
    public async Task<bool> DeletePlugAction(PlugAction? item, CancellationToken cancellationToken = default)=>await TASApiClient.Value.DeletePlugAction(item, cancellationToken);
    public async Task<bool> DeletePlugByDefinitionId(string? definitionId, CancellationToken cancellationToken = default)=> await TASApiClient.Value.DeletePlugByDefinitionId(definitionId, cancellationToken);
    public async Task DeletePlugToPlugActionRealtionship(PlugToPlugAction plugToPlugAction, CancellationToken cancellationToken = default)=>await TASApiClient.Value.DeletePlugToPlugActionRealtionship(plugToPlugAction, cancellationToken);
    public async Task<List<Plug>?> GetCihldPlugsByDefinitionId(string DefinitionId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetCihldPlugsByDefinitionId(DefinitionId, cancellationToken);
    public async Task<Plug?> GetParentPlugById(Plug ChildPlug) => await TASApiClient.Value.GetParentPlugById(ChildPlug);
    public async Task<PlugAction?> GetPlugActionById(int? plugActionId)=>await TASApiClient.Value.GetPlugActionById(plugActionId);
    public async Task<List<Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? plugDefinitionId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugActionsByPlugDefinitionIdAsync(plugDefinitionId, cancellationToken);
    public async Task<List<PlugAction>?> GetPlugActionsByPlugId2(int? plugId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugActionsByPlugId2(plugId, cancellationToken);
    public async Task<List<PlugAction>?> GetPlugActionsByPlugIdAsync(int? plugId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugActionsByPlugIdAsync(plugId, cancellationToken);
    public async Task<List<PlugAction>?> GetPlugActionsToExecuteByPlugId(int? plugId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugActionsToExecuteByPlugId(plugId, cancellationToken);
    public async Task<Plug?> GetPlugByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugByDefinitionIdAsync(definitionId, cancellationToken);
    public async Task<Plug?> GetPlugById(int? Id, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugById(Id, cancellationToken);
    public async Task<List<Plug>> GetPlugs(CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetPlugs(cancellationToken);
    public async Task<string?> GetPlugVariableValueAsync(int plugId, string variableName) => await TASApiClient.Value.GetPlugVariableValueAsync(plugId, variableName);
    public async Task<string?> GetPlugVariableValueAsync(string plugDefinitionId, string variableName)=>await TASApiClient.Value.GetPlugVariableValueAsync(plugDefinitionId, variableName);
    public async Task<Plug?> GetRootPlugByTypeNameAsync(string typeName, CancellationToken cancellationToken = default)=>await TASApiClient.Value.GetRootPlugByTypeNameAsync(typeName, cancellationToken);
    public async Task SetExecutePlugActionsToPlug(Plug plug, List<PlugAction>? plugActions)=>await TASApiClient.Value.SetExecutePlugActionsToPlug(plug, plugActions);
    public async Task<PlugAction?> UpdatePlugActionAsync(PlugAction newItem, CancellationToken cancellationToken = default)=>await TASApiClient.Value.UpdatePlugActionAsync(newItem,cancellationToken);
    public async Task<Plug?> UpdatePlugAsync(int? itemId, Plug item, CancellationToken cancellationToken = default)=>await TASApiClient.Value.UpdatePlugAsync(itemId, item,cancellationToken);
}




