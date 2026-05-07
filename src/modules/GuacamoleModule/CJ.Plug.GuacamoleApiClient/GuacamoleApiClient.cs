using CJ.Plug.GuacamoleModels;
using System.Net.Http.Json;

namespace CJ.Plug.GuacamoleApiClient
{
    /// <summary>
    /// Guacamole API 客户端实现
    /// </summary>
    public class GuacamoleApiClient : IGuacamoleApiClient
    {
        private readonly HttpClient _httpClient;

        public GuacamoleApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GuacamoleTokenDto?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GuacamoleTokenDto>("api/guacamole/token", cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<GuacamoleConnectionDto>?> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GuacamoleConnectionDto>>("api/guacamole/connections", cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<GuacamoleConnectionDto?> GetConnectionByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GuacamoleConnectionDto>($"api/guacamole/connections/station/{stationIp}", cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<GuacamoleConnectionDto?> CreateConnectionAsync(GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/guacamole/connections", connection, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GuacamoleConnectionDto>(cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<GuacamoleConnectionDto?> UpdateConnectionAsync(string connectionId, GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/guacamole/connections/{connectionId}", connection, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GuacamoleConnectionDto>(cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/guacamole/connections/{connectionId}", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<GuacamoleConnectionTokenDto?> GetConnectionEmbedTokenAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GuacamoleConnectionTokenDto>($"api/guacamole/embed/{stationIp}", cancellationToken);
            }
            catch
            {
                return null;
            }
        }
    }
}
