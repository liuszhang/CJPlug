using CJ.Plug.Models.LogModels;
using Serilog;
using System.Net.Http.Json;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public partial class PDZApiClient
    {
        
            //创建数据流
            public async Task<PlugVariableData?> CreatePlugVariableData(PlugVariableData PlugVariableData)
            {
                var response = await httpClient.PostAsJsonAsync("/api/PDZ/createPlugVariable", PlugVariableData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PlugVariableData>();
                    return result;
                }
                Log.Information($"CreatePlugVariableData failed: {response.StatusCode}");
                return null;
            }

            //删除数据流
            public async Task<bool> DeletePlugVariableData(int? Id)
            {
                var response = await httpClient.DeleteAsync($"/api/PDZ/deletePlugVariable/{Id}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                Log.Information($"DeletePlugVariableData failed: {response.StatusCode}");
                return false;
            }


            public async Task<PlugVariableData?> UpdatePlugVariableData(PlugVariableData PlugVariableData)
            {
            if (!PlugVariableData.IsValueFromOtherVariable)
            {
                PlugVariableData.DisplayValue=PlugVariableData.Value;
            }


                var response = await httpClient.PutAsJsonAsync("/api/PDZ/updatePlugVariable", PlugVariableData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PlugVariableData>();
                    return result;
                }
                CLog.Error($"UpdatePlugVariableData failed: {response.StatusCode}");
                return null;
            }


            //根据DefinitionId获取PlugVariableData
            public async Task<PlugVariableData?> GetPlugVariableDataByDefinitionId(string definitionId, CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync($"/api/PDZ/getPlugVariableByDefinitionId/{definitionId}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<PlugVariableData>(cancellationToken: cancellationToken);
                    }
                    CLog.Error($"GetPlugVariableDataByDefinitionId failed: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    CLog.Error($"GetPlugVariableDataByDefinitionId failed for DefinitionId: {definitionId}");
                    return null;
                }
            }

            //根据Id获取PlugVariableData
            public async Task<PlugVariableData?> GetPlugVariableDataById(int? Id, CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await httpClient.GetAsync($"/api/PDZ/getPlugVariableById/{Id}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<PlugVariableData>(cancellationToken: cancellationToken);
                    }
                    CLog.Error($"GetPlugVariableDataById failed: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    CLog.Error($"GetPlugVariableDataById failed for DefinitionId: {Id}");
                    return null;
                }
            }



        
    }
}
