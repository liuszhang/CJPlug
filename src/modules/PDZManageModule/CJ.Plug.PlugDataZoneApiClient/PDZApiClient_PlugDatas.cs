using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using Serilog;
using System.Net.Http.Json;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public partial class PDZApiClient
    {
        

            //统一使用该方法进行PDZ的创建和更新操作,直接更新PDZ的方法比较重，要慎用，优先直接更新PDZ中的子数据
            public async Task<PlugData?> UpdatePlugData(PlugData plugData)
            {
                var response = await httpClient.PutAsJsonAsync("/api/PDZ/updatePlugData", plugData);
                if (response.IsSuccessStatusCode)
                {
                    //var filter=new pdz
                    //var pdz = GetPDZByFilter();
                    StatusReporter.PlugUpdated(plugData.PlugDefinitionId);
                    var result = await response.Content.ReadFromJsonAsync<PlugData>();
                    return result;
                }
                Log.Information($"UpdatePlugData failed: {response.StatusCode}");
                return null;
            }


            public async Task<PlugData?> CreatePlugData(PlugData plugData)
            {
                var response = await httpClient.PostAsJsonAsync("/api/PDZ/createPlugData", plugData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PlugData>();
                    return result;
                }
                Log.Information($"UpdatePlugData failed: {response.StatusCode}");
                return null;
            }

            //根据DefinitionId获取PlugData
            public async Task<PlugData?> GetPlugDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync($"/api/PDZ/getPlugDataByDefinitionId/{definitionId}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<PlugData>(cancellationToken: cancellationToken);
                    }
                    CLog.Error($"GetPlugDataByDefinitionId failed: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    CLog.Error($"GetPlugDataByDefinitionId failed for DefinitionId: {definitionId}");
                    return null;
                }
            }


            public async Task<List<PlugData>> GetAllPlugDatas(CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync("/api/PDZ/getPlugDatas", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<List<PlugData>>(cancellationToken: cancellationToken) ?? new List<PlugData>();
                    }
                    CLog.Error($"GetAllPlugDatas failed: {response.StatusCode}");
                    return new List<PlugData>();
                }
                catch (Exception ex)
                {
                    CLog.Error("GetAllPlugDatas failed");
                    return new List<PlugData>();
                }
            }

        
    }
}
