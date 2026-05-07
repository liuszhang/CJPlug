using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GuacamoleModels
{
    /// <summary>
    /// Guacamole Token 响应 DTO
    /// </summary>
    public class GuacamoleTokenDto
    {
        /// <summary>
        /// 认证 Token
        /// </summary>
        public string? AuthToken { get; set; }

        /// <summary>
        /// 数据源名称
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Guacamole 连接 Token (用于嵌入 iframe)
    /// </summary>
    public class GuacamoleConnectionTokenDto
    {
        /// <summary>
        /// 连接 ID
        /// </summary>
        public string? ConnectionId { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string? ConnectionName { get; set; }

        /// <summary>
        /// Guacamole 服务器 URL
        /// </summary>
        public string? GuacamoleUrl { get; set; }

        /// <summary>
        /// 认证 Token
        /// </summary>
        public string? AuthToken { get; set; }

        /// <summary>
        /// 完整的嵌入 URL (包含 token)
        /// </summary>
        public string? EmbedUrl { get; set; }

        /// <summary>
        /// Station IP
        /// </summary>
        public string? StationIp { get; set; }
    }
}
