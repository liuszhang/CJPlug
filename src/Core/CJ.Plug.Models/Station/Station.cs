using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class Station
    {
        public int? Id { get; set; }
        public string? StationIp { get; set; }
        public string? StationName { get; set; }
        public string? StationStatus { get; set; }
        public string? UpdateTime { get; set; }
        public bool IsStarted { get; set; } = false;
        /// <summary>
        /// 图站分类，可能是Windows、Linux等
        /// </summary>
        public string? StationCategory { get; set; } = "Windows";
        /// <summary>
        /// 图站存放数据的基础路径，可能是Windows的C:\PlugStation\或Linux的/home/plugstation/
        /// </summary>
        public string? StationBasePath { get; set; } = @"C:\\PlugStationData";

        // ========== Guacamole 远程桌面配置 ==========

        /// <summary>
        /// Guacamole 连接 ID (Guacamole 内部标识)
        /// </summary>
        public string? GuacamoleConnectionId { get; set; }

        /// <summary>
        /// 远程桌面协议类型: rdp, vnc, ssh
        /// </summary>
        public string? GuacamoleProtocol { get; set; } = "rdp";

        /// <summary>
        /// 远程桌面端口 (RDP默认3389, VNC默认5900, SSH默认22)
        /// </summary>
        public int? GuacamolePort { get; set; }

        /// <summary>
        /// 远程登录用户名
        /// </summary>
        public string? GuacamoleUsername { get; set; }

        /// <summary>
        /// 远程登录密码 (建议加密存储)
        /// </summary>
        public string? GuacamolePassword { get; set; }

        /// <summary>
        /// 是否启用远程桌面监控
        /// </summary>
        public bool GuacamoleEnabled { get; set; } = false;
    }
}
