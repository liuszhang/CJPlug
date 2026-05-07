using CJ.Plug.GuacamoleModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GuacamoleApiClient
{
    /// <summary>
    /// Guacamole API 客户端接口
    /// </summary>
    public interface IGuacamoleApiClient
    {
        /// <summary>
        /// 获取认证 Token
        /// </summary>
        Task<GuacamoleTokenDto?> GetAuthTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有连接列表
        /// </summary>
        Task<List<GuacamoleConnectionDto>?> GetAllConnectionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据 Station IP 获取连接
        /// </summary>
        Task<GuacamoleConnectionDto?> GetConnectionByStationIpAsync(string stationIp, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建连接
        /// </summary>
        Task<GuacamoleConnectionDto?> CreateConnectionAsync(GuacamoleConnectionDto connection, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新连接
        /// </summary>
        Task<GuacamoleConnectionDto?> UpdateConnectionAsync(string connectionId, GuacamoleConnectionDto connection, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除连接
        /// </summary>
        Task<bool> DeleteConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取连接嵌入 Token (用于 iframe)
        /// </summary>
        Task<GuacamoleConnectionTokenDto?> GetConnectionEmbedTokenAsync(string stationIp, CancellationToken cancellationToken = default);
    }
}
