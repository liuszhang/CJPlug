using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public interface IPDZApiClient
    {
        Task<DataFlowData?> CreateDataFlowData(DataFlowData dataFlowData);
        Task<PlugDataZone?> CreateJobPDZByCopyPDZ(PlugDataZone? SourcePDZ, string? userName);
        Task<PlugDataZone?> CreateOrUpdatePDZ(PlugDataZone pdz);
        Task<PlugData?> CreatePlugData(PlugData plugData);
        Task<PlugVariableData?> CreatePlugVariableData(PlugVariableData PlugVariableData);
        Task<bool> DeleteDataFlowData(int? Id);
        Task<bool> DeletePDZ(string? PDZId);
        Task<bool> DeletePDZByDeletePlug(string? PlugDefinitionId);
        Task<bool> DeletePlugVariableData(int? Id);
        Task<List<PlugData>> GetAllPlugDatas(CancellationToken cancellationToken = default);
        Task<DataFlowData?> GetDataFlowDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default);
        Task<FlowchartData?> GetFlowchartDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default);
        Task<PlugDataZone?> GetOrCreateDesignPDZ(string? UserName, string? ProcessDefinitionId);
        Task<PlugDataZone?> GetOrCreateJobPDZ(string? JobCorrelationId);
        Task<PlugDataZone?> GetOrCreatePDZFromPlug(Models.Plug.Plug? Plug, string? UserName, CancellationToken cancellationToken = default);
        Task<PlugDataZone?> GetOrCreatePDZFromPlugDefinitionId(string PlugDefinitionId, string? UserName, CancellationToken cancellationToken = default);
        Task<PlugDataZone?> GetPDZByFilter(PDZFilter filter, CancellationToken cancellationToken = default);
        Task<PlugDataZone?> GetPDZByIdAsync(int? Id);
        Task<PlugDataZone?> GetPDZByPDZIdAsync(string? PDZId);
        Task<PlugData?> GetPlugDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default);
        Task<PlugVariableData?> GetPlugVariableDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default);
        Task<PlugVariableData?> GetPlugVariableDataById(int? Id, CancellationToken cancellationToken = default);
        Task<DataFlowData?> UpdateDataFlowData(DataFlowData DataFlowData);
        Task<FlowchartData?> UpdateFlowchartData(FlowchartData FlowchartData);
        Task<PlugData?> UpdatePlugData(PlugData plugData);
        Task<PlugVariableData?> UpdatePlugVariableData(PlugVariableData PlugVariableData);
    }
}
