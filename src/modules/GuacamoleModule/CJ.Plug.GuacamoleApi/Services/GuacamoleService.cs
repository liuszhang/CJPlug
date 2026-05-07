using CJ.Plug.GuacamoleApi.Contracts;
using CJ.Plug.GuacamoleModels;
using CJ.Plug.Models.Station;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CJ.Plug.GuacamoleApi.Services
{
    /// <summary>
    /// Guacamole 服务实现
    /// 通过 REST API 与 Apache Guacamole 交互
    /// 支持 .NET Aspire 服务发现
    /// </summary>
    public class GuacamoleService : IGuacamoleService
    {
        private readonly HttpClient _httpClient;
        private readonly GuacamoleConfig _config;
        private readonly IStationManageService _stationService;
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public GuacamoleService(
            HttpClient httpClient,
            IOptions<GuacamoleConfig> config,
            IStationManageService stationService)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _stationService = stationService;

            // 支持 Aspire 服务发现
            // 如果配置了服务名，HttpClient 会通过服务发现解析地址
            if (!string.IsNullOrEmpty(_config.ServerUrl))
            {
                _httpClient.BaseAddress = new Uri(_config.ServerUrl.TrimEnd('/') + "/");
            }
        }

        /// <summary>
        /// 获取管理员认证 Token
        /// Guacamole REST API: POST /api/tokens
        /// </summary>
        public async Task<GuacamoleTokenDto?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        {
            // 使用缓存的 token
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry)
            {
                return new GuacamoleTokenDto
                {
                    AuthToken = _cachedToken,
                    DataSource = _config.DataSource,
                    ExpiresAt = _tokenExpiry
                };
            }

            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "username", _config.AdminUsername },
                    { "password", _config.AdminPassword }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "api/tokens")
                {
                    Content = new FormUrlEncodedContent(formData)
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GuacamoleTokenResponse>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    _cachedToken = result.AuthToken;
                    _tokenExpiry = DateTime.Now.AddHours(1); // Token 通常 1 小时过期

                    return new GuacamoleTokenDto
                    {
                        AuthToken = result.AuthToken,
                        DataSource = result.DataSource ?? _config.DataSource,
                        ExpiresAt = _tokenExpiry
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取 Guacamole 认证 Token 失败");
            }

            return null;
        }

        /// <summary>
        /// 获取所有连接列表
        /// Guacamole REST API: GET /api/session/data/{dataSource}/connections
        /// </summary>
        public async Task<List<GuacamoleConnectionDto>?> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (token == null) return null;

            try
            {
                var url = $"api/session/data/{token.DataSource}/connections?token={token.AuthToken}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var connections = await response.Content.ReadFromJsonAsync<Dictionary<string, GuacamoleConnectionResponse>>(cancellationToken: cancellationToken);
                if (connections == null) return null;

                return connections.Select(c => new GuacamoleConnectionDto
                {
                    ConnectionId = c.Value.Identifier,
                    Name = c.Value.Name,
                    Protocol = c.Value.Protocol,
                    Hostname = c.Value.GetParameter("hostname"),
                    Port = int.TryParse(c.Value.GetParameter("port"), out var port) ? port : 3389,
                    Username = c.Value.GetParameter("username"),
                    Domain = c.Value.GetParameter("domain")
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取 Guacamole 连接列表失败");
                return null;
            }
        }

        /// <summary>
        /// 根据 Station IP 获取连接
        /// </summary>
        public async Task<GuacamoleConnectionDto?> GetConnectionByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            var connections = await GetAllConnectionsAsync(cancellationToken);
            return connections?.FirstOrDefault(c =>
                c.Hostname?.Equals(stationIp, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// 根据 Station ID 获取连接
        /// </summary>
        public async Task<GuacamoleConnectionDto?> GetConnectionByStationIdAsync(int stationId, CancellationToken cancellationToken = default)
        {
            var station = await _stationService.GetByIdAsync(stationId, cancellationToken);
            if (station == null || string.IsNullOrEmpty(station.StationIp)) return null;

            return await GetConnectionByStationIpAsync(station.StationIp, cancellationToken);
        }

        /// <summary>
        /// 创建新的连接
        /// Guacamole REST API: POST /api/session/data/{dataSource}/connections
        /// </summary>
        public async Task<GuacamoleConnectionDto?> CreateConnectionAsync(GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (token == null) return null;

            try
            {
                var guacConnection = new
                {
                    parentIdentifier = "ROOT",
                    name = connection.Name ?? $"Station-{connection.Hostname}",
                    protocol = connection.Protocol,
                    parameters = new Dictionary<string, string>
                    {
                        { "hostname", connection.Hostname ?? "" },
                        { "port", connection.Port.ToString() },
                        { "username", connection.Username ?? "" },
                        { "password", connection.Password ?? "" },
                        { "domain", connection.Domain ?? "" },
                        { "ignore-cert", "true" },
                        { "disable-audio", "true" },
                        { "enable-wallpaper", "false" },
                        { "enable-theming", "false" },
                        { "enable-font-smoothing", "true" },
                        { "enable-full-window-drag", "false" },
                        { "enable-desktop-composition", "false" },
                        { "enable-menu-animations", "false" }
                    },
                    attributes = new Dictionary<string, string>
                    {
                        { "max-connections", "5" },
                        { "max-connections-per-user", "2" }
                    }
                };

                var url = $"api/session/data/{token.DataSource}/connections?token={token.AuthToken}";
                var response = await _httpClient.PostAsJsonAsync(url, guacConnection, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GuacamoleConnectionResponse>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    connection.ConnectionId = result.Identifier;
                    return connection;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建 Guacamole 连接失败: {StationIp}", connection.Hostname);
            }

            return null;
        }

        /// <summary>
        /// 更新连接
        /// Guacamole REST API: PUT /api/session/data/{dataSource}/connections/{id}
        /// </summary>
        public async Task<GuacamoleConnectionDto?> UpdateConnectionAsync(string connectionId, GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (token == null) return null;

            try
            {
                var guacConnection = new
                {
                    parentIdentifier = "ROOT",
                    name = connection.Name ?? $"Station-{connection.Hostname}",
                    protocol = connection.Protocol,
                    parameters = new Dictionary<string, string>
                    {
                        { "hostname", connection.Hostname ?? "" },
                        { "port", connection.Port.ToString() },
                        { "username", connection.Username ?? "" },
                        { "password", connection.Password ?? "" },
                        { "domain", connection.Domain ?? "" },
                        { "ignore-cert", "true" }
                    }
                };

                var url = $"api/session/data/{token.DataSource}/connections/{connectionId}?token={token.AuthToken}";
                var response = await _httpClient.PutAsJsonAsync(url, guacConnection, cancellationToken);
                response.EnsureSuccessStatusCode();

                return connection;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新 Guacamole 连接失败: {ConnectionId}", connectionId);
                return null;
            }
        }

        /// <summary>
        /// 删除连接
        /// Guacamole REST API: DELETE /api/session/data/{dataSource}/connections/{id}
        /// </summary>
        public async Task<bool> DeleteConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (token == null) return false;

            try
            {
                var url = $"api/session/data/{token.DataSource}/connections/{connectionId}?token={token.AuthToken}";
                var response = await _httpClient.DeleteAsync(url, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除 Guacamole 连接失败: {ConnectionId}", connectionId);
                return false;
            }
        }

        /// <summary>
        /// 获取连接的嵌入 Token
        /// 用于在 iframe 中嵌入远程桌面
        /// </summary>
        public async Task<GuacamoleConnectionTokenDto?> GetConnectionTokenAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            var connection = await GetConnectionByStationIpAsync(stationIp, cancellationToken);
            if (connection?.ConnectionId == null) return null;

            var token = await GetAuthTokenAsync(cancellationToken);
            if (token == null) return null;

            // 构建嵌入 URL
            var baseUrl = _config.ServerUrl.TrimEnd('/');
            var embedUrl = $"{baseUrl}/#/client/{connection.ConnectionId}?token={token.AuthToken}";

            return new GuacamoleConnectionTokenDto
            {
                ConnectionId = connection.ConnectionId,
                ConnectionName = connection.Name,
                GuacamoleUrl = baseUrl,
                AuthToken = token.AuthToken,
                EmbedUrl = embedUrl,
                StationIp = stationIp
            };
        }

        #region Guacamole API Response Models

        private class GuacamoleTokenResponse
        {
            public string? AuthToken { get; set; }
            public string? DataSource { get; set; }
            public string? Username { get; set; }
        }

        private class GuacamoleConnectionResponse
        {
            public string? Identifier { get; set; }
            public string? Name { get; set; }
            public string? Protocol { get; set; }
            public Dictionary<string, string>? Parameters { get; set; }

            public string? GetParameter(string name)
            {
                return Parameters?.GetValueOrDefault(name);
            }
        }

        #endregion
    }
}
