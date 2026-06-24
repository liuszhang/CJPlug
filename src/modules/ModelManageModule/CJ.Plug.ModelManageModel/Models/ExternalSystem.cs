namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M5.5 外部系统 — 定义集成的外部服务/系统及其认证信息
    /// </summary>
    public class ExternalSystem
    {
        public int Id { get; set; }

        /// <summary>系统名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>系统编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>系统描述</summary>
        public string? Description { get; set; }

        /// <summary>基础URL</summary>
        public string? BaseUrl { get; set; }

        /// <summary>认证类型：None / Basic / Bearer / ApiKey / OAuth2</summary>
        public string? AuthType { get; set; }

        /// <summary>认证配置（JSON 字符串）</summary>
        public string? AuthConfig { get; set; }

        /// <summary>接口契约列表</summary>
        public List<InterfaceContract> InterfaceContracts { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
