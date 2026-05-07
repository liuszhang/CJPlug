using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GuacamoleModels
{
    /// <summary>
    /// Guacamole 连接配置 DTO
    /// 用于配置 Station 的远程桌面连接
    /// </summary>
    public class GuacamoleConnectionDto
    {
        /// <summary>
        /// 连接 ID (Guacamole 内部 ID)
        /// </summary>
        public string? ConnectionId { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 协议类型: rdp, vnc, ssh, telnet
        /// </summary>
        public string Protocol { get; set; } = "rdp";

        /// <summary>
        /// 目标主机 IP
        /// </summary>
        public string? Hostname { get; set; }

        /// <summary>
        /// 目标端口
        /// </summary>
        public int Port { get; set; } = 3389;

        /// <summary>
        /// 远程登录用户名
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 远程登录密码
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 域名 (RDP)
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// 关联的 Station ID
        /// </summary>
        public int? StationId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
