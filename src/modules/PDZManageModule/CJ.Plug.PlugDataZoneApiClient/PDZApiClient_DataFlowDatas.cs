using CJ.Plug.Models.LogModels;
using Serilog;
using System.Net.Http.Json;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public partial class PDZApiClient
    {
        
            //创建数据流
            public async Task<DataFlowData?> CreateDataFlowData(DataFlowData dataFlowData)
            {
                var response = await httpClient.PostAsJsonAsync("/api/PDZ/createDataFlow", dataFlowData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DataFlowData>();
                    return result;
                }
                Log.Information($"CreateDataFlowData failed: {response.StatusCode}");
                return null;
            }

            //删除数据流
            public async Task<bool> DeleteDataFlowData(int? Id)
            {
                var response = await httpClient.DeleteAsync($"/api/PDZ/deleteDataFlow/{Id}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                Log.Information($"DeleteDataFlowData failed: {response.StatusCode}");
                return false;
            }


            public async Task<DataFlowData?> UpdateDataFlowData(DataFlowData DataFlowData)
            {
                var response = await httpClient.PutAsJsonAsync("/api/PDZ/updateDataFlow", DataFlowData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DataFlowData>();
                    return result;
                }
                Log.Information($"UpdateDataFlowData failed: {response.StatusCode}");
                return null;
            }


            //根据DefinitionId获取DataFlowData
            public async Task<DataFlowData?> GetDataFlowDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync($"/api/PDZ/getDataFlowByDefinitionId/{definitionId}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<DataFlowData>(cancellationToken: cancellationToken);
                    }
                    CLog.Error($"GetDataFlowDataByDefinitionId failed: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    CLog.Error($"GetDataFlowDataByDefinitionId failed for DefinitionId: {definitionId}");
                    return null;
                }
            }

        
    }
}
