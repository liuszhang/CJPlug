using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Station;
using System.Net.Http.Json;

namespace CJ.Plug.StationAndToolApiClient
{
    public partial class StationAndToolApiClient
    {
        public async Task<List<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default)
        {
            var Tools = await httpClient.GetFromJsonAsync<IEnumerable<Tool>?>("api/Tool/GetAllTools", cancellationToken);
            return Tools?.ToList();
        }
        public async Task<Tool?> GetToolByIdAsync(int? id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<Tool?>($"api/Tool/GetToolById/{id}", cancellationToken);
            }
            catch (Exception ex)
            {
                CLog.Error($"获取工具失败: {ex.Message}");
                return null;
            }

        }

        public async Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(toolDisplayName))
            {
                return null;
            }
            try
            {
                var result = await httpClient.GetFromJsonAsync<Tool?>($"api/Tool/GetToolByDisplayName/{toolDisplayName}", cancellationToken);

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
                CLog.Error($"An error3(GetToolByDisplayName) occurred while fetching data: {ex.Message}");
                //return null;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                CLog.Error($"An error4(GetToolByDisplayName) occurred while fetching data: {ex.Message}");
                //return null;
            }
            return null;
        }

        public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default)
        {
            return await httpClient.PostAsJsonAsync("api/Tool/CreateTool", newTool, cancellationToken).Result.Content.ReadFromJsonAsync<Tool>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteToolAsync(int? ToolId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"api/Tool/DeleteTool/{ToolId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default)
        {
            return await httpClient.PutAsJsonAsync("api/Tool/UpdateTool", updatedTool, cancellationToken).Result.Content.ReadFromJsonAsync<Tool>(cancellationToken: cancellationToken);
        }
    }
}
