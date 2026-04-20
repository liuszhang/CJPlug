using CJ.Plug.Models.LogModels;
using Serilog;
using System.Net.Http.Json;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public partial class PDZApiClient
    {
        

            //统一使用该方法进行PDZ的创建和更新操作,直接更新PDZ的方法比较重，要慎用，优先直接更新PDZ中的子数据
            public async Task<FlowchartData?> UpdateFlowchartData(FlowchartData FlowchartData)
            {
                var response = await httpClient.PutAsJsonAsync("/api/PDZ/updateFlowchart", FlowchartData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FlowchartData>();
                    return result;
                }
                Log.Information($"UpdateFlowchartData failed: {response.StatusCode}");
                return null;
            }


            //根据DefinitionId获取FlowchartData
            public async Task<FlowchartData?> GetFlowchartDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync($"/api/PDZ/getFlowchartByDefinitionId/{definitionId}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<FlowchartData>(cancellationToken: cancellationToken);
                    }
                    CLog.Error($"GetFlowchartDataByDefinitionId failed: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    CLog.Error($"GetFlowchartDataByDefinitionId failed for DefinitionId: {definitionId}");
                    return null;
                }
            }

        
    }
}
