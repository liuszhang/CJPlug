using CJ.Plug.Models.Station;
using System.Net.Http.Json;

namespace CJ.Plug.StationManageApiClient
{
    public partial class StationManageApiClient
    {
        public async Task<List<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken cancellationToken = default)
        {
            var configs = await httpClient.GetFromJsonAsync<IEnumerable<StationConfigTable>?>("api/StationConfig/GetAllConfigs", cancellationToken);
            return configs?.ToList();
        }
        public async Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<StationConfigTable>($"api/StationConfig/GetById/{id}", cancellationToken);
        }

        public async Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            //return await httpClient.GetFromJsonAsync<StationConfigTable>($"api/StationConfig/GetConfigByStationIp/{stationIp}", cancellationToken);
            try
            {
                //var uri = new Uri(stationIp);
                string encodedIp = Uri.EscapeDataString(stationIp);
                //Console.WriteLine($"编码前的IP为：{uri.Host}");
                //Console.WriteLine($"编码后的IP为：{encodedIp}");
                var result = await httpClient.GetFromJsonAsync<StationConfigTable?>($"api/StationConfig/GetConfigByStationIp/{encodedIp}", cancellationToken);

                if (result == null)
                {
                    return null;
                }
                else
                {
                    return result;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error3(GetToolAgentToolConfigByIp) occurred while fetching data: {ex.Message}");
                //return null;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                Console.WriteLine($"An error4(GetToolAgentToolConfigByIp) occurred while fetching data: {ex.Message}");
                //return null;
            }
            return null;
        }

        public async Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("api/stationConfig/CreateConfig", newStationToolConfig, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StationConfigTable>(cancellationToken: cancellationToken);
        }

        public async Task CreateOrSetToolConfig(StationConfigTable stationToolConfig)
        {
            var config = await GetByStationIpAsync(stationToolConfig.StationIp);
            if (config == null)
            {
                Console.WriteLine($"Creating new station tool config for IP: {stationToolConfig.StationIp}");
                await CreateStationToolConfigAsync(stationToolConfig);
            }
            else
            {
                Console.WriteLine($"Updating existing station tool config for IP: {stationToolConfig.StationIp}");
                stationToolConfig.Id = config.Id;
                await UpdateStationToolConfigAsync(stationToolConfig);
            }
        }

        public async Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"api/StationConfig/DeleteStationToolConfig/{stationToolConfigId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken cancellationToken = default)
        {
            return await httpClient.PutAsJsonAsync("api/StationConfig/UpdateConfig", updatedStationToolConfig, cancellationToken).Result.Content.ReadFromJsonAsync<StationConfigTable>(cancellationToken: cancellationToken);
        }

    }
}
