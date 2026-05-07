using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.GuacamoleModels;


    /// <summary>
    /// MainApiClient - Guacamole API 客户端集成
    /// </summary>
    public partial class MainApiClient : IGuacamoleApiClient
    {
        public async Task<GuacamoleTokenDto?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.GetAuthTokenAsync(cancellationToken);
        }

        public async Task<List<GuacamoleConnectionDto>?> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.GetAllConnectionsAsync(cancellationToken);
        }

        public async Task<GuacamoleConnectionDto?> GetConnectionByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.GetConnectionByStationIpAsync(stationIp, cancellationToken);
        }

        public async Task<GuacamoleConnectionDto?> CreateConnectionAsync(GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.CreateConnectionAsync(connection, cancellationToken);
        }

        public async Task<GuacamoleConnectionDto?> UpdateConnectionAsync(string connectionId, GuacamoleConnectionDto connection, CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.UpdateConnectionAsync(connectionId, connection, cancellationToken);
        }

        public async Task<bool> DeleteConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.DeleteConnectionAsync(connectionId, cancellationToken);
        }

        public async Task<GuacamoleConnectionTokenDto?> GetConnectionEmbedTokenAsync(string stationIp, CancellationToken cancellationToken = default)
        {
            return await GuacamoleApiClient.Value.GetConnectionEmbedTokenAsync(stationIp, cancellationToken);
        }
    }

