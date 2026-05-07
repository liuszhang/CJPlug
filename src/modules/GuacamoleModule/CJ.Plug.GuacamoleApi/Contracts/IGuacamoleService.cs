using CJ.Plug.GuacamoleModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GuacamoleApi.Contracts
{
    /// <summary>
    /// Guacamole 服务接口
    /// 负责与 Apache Guacamole REST API 交互
    /// </summary>
    public interface IGuacamoleService
    {
        /// <summary>
        /// 获取管理员认证 Token
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
        /// 创建新的连接
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
        /// 获取连接的嵌入 Token (用于 iframe 嵌入)
        /// </summary>
        Task<GuacamoleConnectionTokenDto?> GetConnectionTokenAsync(string stationIp, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据 Station ID 获取连接配置
        /// </summary>
        Task<GuacamoleConnectionDto?> GetConnectionByStationIdAsync(int stationId, CancellationToken cancellationToken = default);
    }
}
