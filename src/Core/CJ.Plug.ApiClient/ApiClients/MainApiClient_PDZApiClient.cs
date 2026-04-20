using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugDataZoneApiClient;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;

public partial class MainApiClient : IPDZApiClient
{
    public async Task<DataFlowData?> CreateDataFlowData(DataFlowData dataFlowData)=>await PDZApiClient.Value.CreateDataFlowData(dataFlowData);
    public async Task<PlugDataZone?> CreateJobPDZByCopyPDZ(PlugDataZone? SourcePDZ, string? userName)=>await PDZApiClient.Value.CreateJobPDZByCopyPDZ(SourcePDZ, userName);
    public async Task<PlugDataZone?> CreateOrUpdatePDZ(PlugDataZone pdz)=>await PDZApiClient.Value.CreateOrUpdatePDZ(pdz);
    public async Task<PlugData?> CreatePlugData(PlugData plugData)=>await PDZApiClient.Value.CreatePlugData(plugData);
    public async Task<PlugVariableData?> CreatePlugVariableData(PlugVariableData PlugVariableData)=>await PDZApiClient.Value.CreatePlugVariableData(PlugVariableData);
    public async Task<bool> DeleteDataFlowData(int? Id)=>await PDZApiClient.Value.DeleteDataFlowData(Id);
    public async Task<bool> DeletePDZ(string? PDZId)=>await PDZApiClient.Value.DeletePDZ(PDZId);
    public async Task<bool> DeletePDZByDeletePlug(string? PlugDefinitionId)=>await PDZApiClient.Value.DeletePDZByDeletePlug(PlugDefinitionId);
    public async Task<bool> DeletePlugVariableData(int? Id)=>await PDZApiClient.Value.DeletePlugVariableData(Id);
    public async Task<List<PlugData>> GetAllPlugDatas(CancellationToken ct = default)=>await PDZApiClient.Value.GetAllPlugDatas(ct);
    public async Task<DataFlowData?> GetDataFlowDataByDefinitionId(string definitionId, CancellationToken ct = default)=>await PDZApiClient.Value.GetDataFlowDataByDefinitionId(definitionId, ct);
    public async Task<FlowchartData?> GetFlowchartDataByDefinitionId(string definitionId, CancellationToken ct = default)=>await PDZApiClient.Value.GetFlowchartDataByDefinitionId(definitionId,ct);
    public async Task<PlugDataZone?> GetOrCreateDesignPDZ(string? UserName, string? ProcessDefinitionId)=>await PDZApiClient.Value.GetOrCreateDesignPDZ(UserName, ProcessDefinitionId);
    public async Task<PlugDataZone?> GetOrCreateJobPDZ(string? JobCorrelationId)=>await PDZApiClient.Value.GetOrCreateJobPDZ(JobCorrelationId);
    public async Task<PlugDataZone?> GetOrCreatePDZFromPlug(Plug? Plug, string? UserName, CancellationToken ct = default)=>await PDZApiClient.Value.GetOrCreatePDZFromPlug(Plug, UserName, ct);
    public async Task<PlugDataZone?> GetOrCreatePDZFromPlugDefinitionId(string PlugDefinitonId, string? UserName, CancellationToken ct = default)=>await PDZApiClient.Value.GetOrCreatePDZFromPlugDefinitionId(PlugDefinitonId, UserName, ct);
    public async Task<PlugDataZone?> GetPDZByFilter(PDZFilter filter, CancellationToken ct = default)=>await PDZApiClient.Value.GetPDZByFilter(filter, ct);
    public async Task<PlugDataZone?> GetPDZByIdAsync(int? Id)=>await PDZApiClient.Value.GetPDZByIdAsync(Id);
    public async Task<PlugDataZone?> GetPDZByPDZIdAsync(string? PDZId)=>await PDZApiClient.Value.GetPDZByPDZIdAsync(PDZId);
    public async Task<PlugData?> GetPlugDataByDefinitionId(string definitionId, CancellationToken ct = default)=>await PDZApiClient.Value.GetPlugDataByDefinitionId(definitionId, ct);
    public async Task<PlugVariableData?> GetPlugVariableDataByDefinitionId(string definitionId, CancellationToken ct = default)=>await PDZApiClient.Value.GetPlugVariableDataByDefinitionId(definitionId,ct);
    public async Task<PlugVariableData?> GetPlugVariableDataById(int? Id, CancellationToken ct = default)=>await PDZApiClient.Value.GetPlugVariableDataById(Id, ct);
    public async Task<DataFlowData?> UpdateDataFlowData(DataFlowData DataFlowData)=>await PDZApiClient.Value.UpdateDataFlowData(DataFlowData);
    public async Task<FlowchartData?> UpdateFlowchartData(FlowchartData FlowchartData)=>await PDZApiClient.Value.UpdateFlowchartData(FlowchartData);
    public async Task<PlugData?> UpdatePlugData(PlugData plugData)=>await PDZApiClient.Value.UpdatePlugData(plugData);
    public async Task<PlugVariableData?> UpdatePlugVariableData(PlugVariableData PlugVariableData)=>await PDZApiClient.Value.UpdatePlugVariableData(PlugVariableData);
}

