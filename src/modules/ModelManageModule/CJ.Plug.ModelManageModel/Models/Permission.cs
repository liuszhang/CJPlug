namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M5 权限 — 定义主体对资源的访问权限
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        /// <summary>权限名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>权限编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>资源类型</summary>
        public string? ResourceType { get; set; }

        /// <summary>资源ID</summary>
        public int? ResourceId { get; set; }

        /// <summary>操作类型：Read / Write / Delete / Execute / Admin</summary>
        public string? Action { get; set; }

        /// <summary>所属主体ID（FK → Subject）</summary>
        public int SubjectId { get; set; }

        /// <summary>所属主体导航属性</summary>
        public Subject? Subject { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
