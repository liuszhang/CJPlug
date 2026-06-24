namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M5.5 接口契约 — 定义外部系统的 API 接口规范
    /// </summary>
    public class InterfaceContract
    {
        public int Id { get; set; }

        /// <summary>接口名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>接口编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>接口描述</summary>
        public string? Description { get; set; }

        /// <summary>所属外部系统ID（FK → ExternalSystem）</summary>
        public int ExternalSystemId { get; set; }

        /// <summary>所属外部系统导航属性</summary>
        public ExternalSystem? ExternalSystem { get; set; }

        /// <summary>HTTP 方法：GET / POST / PUT / DELETE / PATCH</summary>
        public string? Method { get; set; }

        /// <summary>接口端点路径（如 /weather/current）</summary>
        public string? Endpoint { get; set; }

        /// <summary>请求 Schema（JSON 字符串）</summary>
        public string? RequestSchema { get; set; }

        /// <summary>响应 Schema（JSON 字符串）</summary>
        public string? ResponseSchema { get; set; }

        /// <summary>状态：Active / Deprecated / Draft</summary>
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
