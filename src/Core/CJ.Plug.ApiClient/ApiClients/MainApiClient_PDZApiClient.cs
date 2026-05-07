using CJ.Plug.AuditModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugDataZoneApiClient;

public partial class MainApiClient : IPDZApiClient
{
    public async Task<DataFlowData?> CreateDataFlowData(DataFlowData dataFlowData)
    {
        var result = await PDZApiClient.Value.CreateDataFlowData(dataFlowData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, "创建数据流数据");
        return result;
    }

    public async Task<PlugDataZone?> CreateJobPDZByCopyPDZ(PlugDataZone? SourcePDZ, string? userName)
    {
        var result = await PDZApiClient.Value.CreateJobPDZByCopyPDZ(SourcePDZ, userName);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"通过复制创建作业PDZ: {userName}");
        return result;
    }

    public async Task<PlugDataZone?> CreateOrUpdatePDZ(PlugDataZone pdz)
    {
        var result = await PDZApiClient.Value.CreateOrUpdatePDZ(pdz);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"创建或更新PDZ: {pdz.PDZId}");
        return result;
    }

    public async Task<PlugData?> CreatePlugData(PlugData plugData)
    {
        var result = await PDZApiClient.Value.CreatePlugData(plugData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, "创建插件数据");
        return result;
    }

    public async Task<PlugVariableData?> CreatePlugVariableData(PlugVariableData PlugVariableData)
    {
        var result = await PDZApiClient.Value.CreatePlugVariableData(PlugVariableData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, "创建插件变量数据");
        return result;
    }

    public async Task<bool> DeleteDataFlowData(int? Id)
    {
        var result = await PDZApiClient.Value.DeleteDataFlowData(Id);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除数据流数据ID: {Id}");
        return result;
    }

    public async Task<bool> DeletePDZ(string? PDZId)
    {
        var result = await PDZApiClient.Value.DeletePDZ(PDZId);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除PDZ: {PDZId}");
        return result;
    }

    public async Task<bool> DeletePDZByDeletePlug(string? PlugDefinitionId)
    {
        var result = await PDZApiClient.Value.DeletePDZByDeletePlug(PlugDefinitionId);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"通过删除插件删除PDZ: {PlugDefinitionId}");
        return result;
    }

    public async Task<bool> DeletePlugVariableData(int? Id)
    {
        var result = await PDZApiClient.Value.DeletePlugVariableData(Id);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除插件变量数据ID: {Id}");
        return result;
    }

    public async Task<List<PlugData>> GetAllPlugDatas(CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetAllPlugDatas(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有插件数据");
        return result;
    }

    public async Task<DataFlowData?> GetDataFlowDataByDefinitionId(string definitionId, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetDataFlowDataByDefinitionId(definitionId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询数据流定义ID: {definitionId}");
        return result;
    }

    public async Task<FlowchartData?> GetFlowchartDataByDefinitionId(string definitionId, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetFlowchartDataByDefinitionId(definitionId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询流程图定义ID: {definitionId}");
        return result;
    }

    public async Task<PlugDataZone?> GetOrCreateDesignPDZ(string? UserName, string? ProcessDefinitionId)
    {
        var result = await PDZApiClient.Value.GetOrCreateDesignPDZ(UserName, ProcessDefinitionId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取或创建设计PDZ: {UserName}");
        return result;
    }

    public async Task<PlugDataZone?> GetOrCreateJobPDZ(string? JobCorrelationId)
    {
        var result = await PDZApiClient.Value.GetOrCreateJobPDZ(JobCorrelationId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取或创建作业PDZ: {JobCorrelationId}");
        return result;
    }

    public async Task<PlugDataZone?> GetOrCreatePDZFromPlug(Plug? Plug, string? UserName, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetOrCreatePDZFromPlug(Plug, UserName, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"从插件获取或创建PDZ: {Plug?.DefinitionId}");
        return result;
    }

    public async Task<PlugDataZone?> GetOrCreatePDZFromPlugDefinitionId(string PlugDefinitonId, string? UserName, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetOrCreatePDZFromPlugDefinitionId(PlugDefinitonId, UserName, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"从插件定义获取或创建PDZ: {PlugDefinitonId}");
        return result;
    }

    public async Task<PlugDataZone?> GetPDZByFilter(PDZFilter filter, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetPDZByFilter(filter, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "按筛选查询PDZ");
        return result;
    }

    public async Task<PlugDataZone?> GetPDZByIdAsync(int? Id)
    {
        var result = await PDZApiClient.Value.GetPDZByIdAsync(Id);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询PDZ ID: {Id}");
        return result;
    }

    public async Task<PlugDataZone?> GetPDZByPDZIdAsync(string? PDZId)
    {
        var result = await PDZApiClient.Value.GetPDZByPDZIdAsync(PDZId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询PDZ ID: {PDZId}");
        return result;
    }

    public async Task<PlugData?> GetPlugDataByDefinitionId(string definitionId, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetPlugDataByDefinitionId(definitionId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询插件数据定义ID: {definitionId}");
        return result;
    }

    public async Task<PlugVariableData?> GetPlugVariableDataByDefinitionId(string definitionId, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetPlugVariableDataByDefinitionId(definitionId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询插件变量数据定义ID: {definitionId}");
        return result;
    }

    public async Task<PlugVariableData?> GetPlugVariableDataById(int? Id, CancellationToken ct = default)
    {
        var result = await PDZApiClient.Value.GetPlugVariableDataById(Id, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询插件变量数据ID: {Id}");
        return result;
    }

    public async Task<DataFlowData?> UpdateDataFlowData(DataFlowData DataFlowData)
    {
        var result = await PDZApiClient.Value.UpdateDataFlowData(DataFlowData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "更新数据流数据");
        return result;
    }

    public async Task<FlowchartData?> UpdateFlowchartData(FlowchartData FlowchartData)
    {
        var result = await PDZApiClient.Value.UpdateFlowchartData(FlowchartData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "更新流程图数据");
        return result;
    }

    public async Task<PlugData?> UpdatePlugData(PlugData plugData)
    {
        var result = await PDZApiClient.Value.UpdatePlugData(plugData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "更新插件数据");
        return result;
    }

    public async Task<PlugVariableData?> UpdatePlugVariableData(PlugVariableData PlugVariableData)
    {
        var result = await PDZApiClient.Value.UpdatePlugVariableData(PlugVariableData);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, "更新插件变量数据");
        return result;
    }
}
