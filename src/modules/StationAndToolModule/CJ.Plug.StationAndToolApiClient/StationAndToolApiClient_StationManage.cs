using CJ.Plug.Models.Station;
using Serilog;
using System.Net.Http.Json;

namespace CJ.Plug.StationAndToolApiClient
{
    public partial class StationAndToolApiClient
    {
        
            public async Task<string?> GetStationToUse(CancellationToken cancellationToken = default)
            {
                //通过调度服务获取使用的图站
                return await DispatcherClient.GetStringAsync("api/dispatch/GetStationToExecute", cancellationToken);
            }

            public async Task<List<Station>?> GetAllStationsAsync(CancellationToken cancellationToken = default)
            {
                var Stations = await httpClient.GetFromJsonAsync<IEnumerable<Station>?>("api/Station/GetAllStations", cancellationToken);
                return Stations?.ToList();
            }
            public async Task<Station?> GetStationByIdAsync(int id, CancellationToken cancellationToken = default)
            {
                return await httpClient.GetFromJsonAsync<Station>($"api/Station/GetById/{id}", cancellationToken);
            }

            public async Task<Station?> CheckOrCreateStation(string stationIp, CancellationToken cancellationToken = default)
            {
                var exist = await GetStationByIpAsync(stationIp, cancellationToken);
                if (exist != null)
                {
                    exist.UpdateTime = DateTime.Now.ToString();
                    return await UpdateStationAsync(exist);
                }
                else
                {
                    exist = new Station();
                    exist.UpdateTime = DateTime.Now.ToString();
                    exist.StationIp = stationIp;
                    return await CreateStationAsync(exist);
                }
            }

            public async Task<Station?> GetStationByIpAsync(string stationIp, CancellationToken cancellationToken = default)
            {
                //return await httpClient.GetFromJsonAsync<Station>($"api/Station/GetStationByStationIp/{stationIp}", cancellationToken);
                try
                {
                    //var uri = new Uri(stationIp);
                    string encodedIp = Uri.EscapeDataString(stationIp);
                    //Console.WriteLine($"编码前的IP为：{uri.Host}");
                    //Console.WriteLine($"编码后的IP为：{encodedIp}");
                    var result = await httpClient.GetFromJsonAsync<Station?>($"api/Station/GetStationByIp/{encodedIp}", cancellationToken);

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
                    Console.WriteLine($"An error3(GetToolAgentToolStationByIp) occurred while fetching data: {ex.Message}");
                    //return null;
                }
                catch (Exception ex)
                {
                    // 处理其他类型的异常
                    Console.WriteLine($"An error4(GetToolAgentToolStationByIp) occurred while fetching data: {ex.Message}");
                    //return null;
                }
                return null;
            }

            public async Task<Station?> CreateStationAsync(Station newStation, CancellationToken cancellationToken = default)
            {
                return await httpClient.PostAsJsonAsync("api/Station/CreateStation", newStation, cancellationToken).Result.Content.ReadFromJsonAsync<Station>(cancellationToken: cancellationToken);
            }

            public async Task<bool> DeleteStationAsync(int StationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.DeleteAsync($"api/Station/DeleteStation/{StationId}", cancellationToken);
                return response.IsSuccessStatusCode;
            }

            public async Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken cancellationToken = default)
            {
                return await httpClient.PutAsJsonAsync("api/Station/UpdateStation", updatedStation, cancellationToken).Result.Content.ReadFromJsonAsync<Station>(cancellationToken: cancellationToken);
            }

            public async Task<string?> GetToolPathOnIp(string ip, string toolName, string? version = null)
            {
                version ??= "latest";
                return await httpClient.GetStringAsync($"api/stationConfig/GetToolPathOnIp/{ip}/{toolName}/{version}");
            }

            public async Task<string?> GetToolPathByFilter(ToolConfigFilter ToolConfigFilter)
            {
                var response = await httpClient.PostAsJsonAsync($"api/stationConfig/GetToolPathByFilter", ToolConfigFilter);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }
                else
                {
                    return null;
                }
            }


            public async Task<Station?> GetStationToUseByTool(string toolName, string? version = null, CancellationToken cancellationToken = default)
            {
                var filter = new ToolConfigFilter()
                {
                    ToolName = toolName,
                    ToolVersion = version
                };
                var stationIp = await GetStationToUse(cancellationToken);
                if (string.IsNullOrEmpty(stationIp))
                {
                    Log.Warning("暂无可用图站，请稍后再试1");
                    return null;
                }
                return await GetStationByIpAsync(stationIp, cancellationToken);
            }
        
    }
}
