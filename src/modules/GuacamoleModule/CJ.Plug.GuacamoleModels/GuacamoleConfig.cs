using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GuacamoleModels
{
    /// <summary>
    /// Guacamole 配置
    /// 支持 .NET Aspire 服务发现
    /// </summary>
    public class GuacamoleConfig
    {
        /// <summary>
        /// Guacamole 服务器地址
        /// 通过 Aspire 服务发现自动配置: http://guacamole:8080/guacamole
        /// </summary>
        public string ServerUrl { get; set; } = "http://localhost:8080/guacamole";

        /// <summary>
        /// Guacd 守护进程地址
        /// 通过 Aspire 服务发现自动配置: guacd:4822
        /// </summary>
        public string GuacdHost { get; set; } = "localhost";
        public int GuacdPort { get; set; } = 4822;

        /// <summary>
        /// Guacamole 管理员用户名
        /// </summary>
        public string AdminUsername { get; set; } = "guacadmin";

        /// <summary>
        /// Guacamole 管理员密码
        /// </summary>
        public string AdminPassword { get; set; } = "guacadmin";

        /// <summary>
        /// 数据源名称
        /// 使用 JSON 文件认证时不需要数据库
        /// </summary>
        public string DataSource { get; set; } = "json";

        /// <summary>
        /// 是否使用 JSON 文件认证 (内嵌模式，无需数据库)
        /// </summary>
        public bool UseJsonAuth { get; set; } = true;

        /// <summary>
        /// JSON 认证文件路径 (Guacamole 容器内路径)
        /// </summary>
        public string JsonAuthFile { get; set; } = "/etc/guacamole/user-mapping.xml";
    }
}
