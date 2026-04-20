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
using CJ.Plug.Models.Station;

public partial class MainApiClient : IStationAndToolApiClient
{
    public async Task<Station?> CheckOrCreateStation(string stationIp, CancellationToken ct = default)=>await StationAndToolApiClient.Value.CheckOrCreateStation(stationIp, ct);
    public async Task CreateOrSetToolConfig(StationConfigTable stationToolConfig)=>await StationAndToolApiClient.Value.CreateOrSetToolConfig(stationToolConfig);
    public async Task<Station?> CreateStationAsync(Station newStation, CancellationToken ct = default) => await StationAndToolApiClient.Value.CreateStationAsync(newStation, ct);
    public async Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken ct = default) => await StationAndToolApiClient.Value.CreateStationToolConfigAsync(newStationToolConfig, ct);
    public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken ct = default) => await StationAndToolApiClient.Value.CreateToolAsync(newTool, ct);
    public async Task<bool> DeleteStationAsync(int StationId, CancellationToken ct = default) => await StationAndToolApiClient.Value.DeleteStationAsync(StationId, ct);
    public async Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken ct = default) => await StationAndToolApiClient.Value.DeleteStationToolConfigAsync(stationToolConfigId, ct);
    public async Task<bool> DeleteToolAsync(int? ToolId, CancellationToken ct = default) => await StationAndToolApiClient.Value.DeleteToolAsync(ToolId, ct);
    public async Task<List<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken ct = default) => await StationAndToolApiClient.Value.GetAllStationConfigsAsync(ct);
    public async Task<List<Station>?> GetAllStationsAsync(CancellationToken ct = default) => await StationAndToolApiClient.Value.GetAllStationsAsync(ct);
    public async Task<List<Tool>?> GetAllToolsAsync(CancellationToken ct = default) => await StationAndToolApiClient.Value.GetAllToolsAsync(ct);
    public async Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetByIdAsync(id, ct);
    public async Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetByStationIpAsync(stationIp, ct);
    public async Task<Station?> GetStationByIdAsync(int id, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetStationByIdAsync(id, ct);
    public async Task<Station?> GetStationByIpAsync(string stationIp, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetStationByIpAsync($"{stationIp}", ct);
    public async Task<string?> GetStationToUse(CancellationToken ct = default) => await StationAndToolApiClient.Value.GetStationToUse(ct);
    public async Task<Station?> GetStationToUseByTool(string toolName, string? version = null, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetStationToUseByTool(toolName, version, ct);
    public async Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetToolByDisplayNameAsync(toolDisplayName, ct);
    public async Task<Tool?> GetToolByIdAsync(int? id, CancellationToken ct = default) => await StationAndToolApiClient.Value.GetToolByIdAsync(id, ct);
    public async Task<string?> GetToolPathByFilter(ToolConfigFilter ToolConfigFilter) => await StationAndToolApiClient.Value.GetToolPathByFilter(ToolConfigFilter);
    public async Task<string?> GetToolPathOnIp(string ip, string toolName, string? version = null) => await StationAndToolApiClient.Value.GetToolPathOnIp(ip, toolName, version);
    public async Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken ct = default) => await StationAndToolApiClient.Value.UpdateStationAsync(updatedStation, ct);
    public async Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken ct = default) => await StationAndToolApiClient.Value.UpdateStationToolConfigAsync(updatedStationToolConfig, ct);
    public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken ct = default) => await StationAndToolApiClient.Value.UpdateToolAsync(updatedTool, ct);
}




