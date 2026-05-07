using CJ.Plug.AuditModels;
using CJ.Plug.Models.Station;
using CJ.Plug.StationAndToolApiClient;

public partial class MainApiClient : IStationAndToolApiClient
{
    public async Task<Station?> CheckOrCreateStation(string stationIp, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.CheckOrCreateStation(stationIp, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"检查或创建工作站: {stationIp}");
        return result;
    }

    public async Task CreateOrSetToolConfig(StationConfigTable stationToolConfig)
    {
        await StationAndToolApiClient.Value.CreateOrSetToolConfig(stationToolConfig);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "创建或设置工具配置");
    }

    public async Task<Station?> CreateStationAsync(Station newStation, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.CreateStationAsync(newStation, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建工作站ID: {newStation.Id}");
        return result;
    }

    public async Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.CreateStationToolConfigAsync(newStationToolConfig, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, "创建工作站工具配置");
        return result;
    }

    public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.CreateToolAsync(newTool, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建工具ID: {newTool.Id}");
        return result;
    }

    public async Task<bool> DeleteStationAsync(int StationId, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.DeleteStationAsync(StationId, ct);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除工作站ID: {StationId}");
        return result;
    }

    public async Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.DeleteStationToolConfigAsync(stationToolConfigId, ct);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除工作站工具配置ID: {stationToolConfigId}");
        return result;
    }

    public async Task<bool> DeleteToolAsync(int? ToolId, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.DeleteToolAsync(ToolId, ct);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除工具ID: {ToolId}");
        return result;
    }

    public async Task<List<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetAllStationConfigsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有工作站配置");
        return result;
    }

    public async Task<List<Station>?> GetAllStationsAsync(CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetAllStationsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有工作站");
        return result;
    }

    public async Task<List<Tool>?> GetAllToolsAsync(CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetAllToolsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有工具");
        return result;
    }

    public async Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetByIdAsync(id, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工作站配置ID: {id}");
        return result;
    }

    public async Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetByStationIpAsync(stationIp, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工作站配置IP: {stationIp}");
        return result;
    }

    public async Task<Station?> GetStationByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetStationByIdAsync(id, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工作站ID: {id}");
        return result;
    }

    public async Task<Station?> GetStationByIpAsync(string stationIp, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetStationByIpAsync($"{stationIp}", ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工作站IP: {stationIp}");
        return result;
    }

    public async Task<string?> GetStationToUse(CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetStationToUse(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "获取可用工作站");
        return result;
    }

    public async Task<Station?> GetStationToUseByTool(string toolName, string? version = null, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetStationToUseByTool(toolName, version, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"按工具获取工作站: {toolName}");
        return result;
    }

    public async Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetToolByDisplayNameAsync(toolDisplayName, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具显示名: {toolDisplayName}");
        return result;
    }

    public async Task<Tool?> GetToolByIdAsync(int? id, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.GetToolByIdAsync(id, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具ID: {id}");
        return result;
    }

    public async Task<string?> GetToolPathByFilter(ToolConfigFilter ToolConfigFilter)
    {
        var result = await StationAndToolApiClient.Value.GetToolPathByFilter(ToolConfigFilter);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "按筛选获取工具路径");
        return result;
    }

    public async Task<string?> GetToolPathOnIp(string ip, string toolName, string? version = null)
    {
        var result = await StationAndToolApiClient.Value.GetToolPathOnIp(ip, toolName, version);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取IP工具路径: {ip}.{toolName}");
        return result;
    }

    public async Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.UpdateStationAsync(updatedStation, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新工作站ID: {updatedStation.Id}");
        return result;
    }

    public async Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.UpdateStationToolConfigAsync(updatedStationToolConfig, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "更新工作站工具配置");
        return result;
    }

    public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken ct = default)
    {
        var result = await StationAndToolApiClient.Value.UpdateToolAsync(updatedTool, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新工具ID: {updatedTool.Id}");
        return result;
    }
}
