namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M5 主体 — 定义组织/部门/角色/人员及其层级关系
    /// </summary>
    public class Subject
    {
        public int Id { get; set; }

        /// <summary>主体名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>主体编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>主体类型：Person / Department / Role</summary>
        public string? SubjectType { get; set; }

        /// <summary>主体描述</summary>
        public string? Description { get; set; }

        /// <summary>父主体ID（FK → Subject，自引用层级）</summary>
        public int? ParentId { get; set; }

        /// <summary>父主体导航属性</summary>
        public Subject? Parent { get; set; }

        /// <summary>子主体集合</summary>
        public List<Subject> Children { get; set; } = new();

        /// <summary>关联本体ID（可选，关联到 M1 对象定义）</summary>
        public int? OntologyId { get; set; }

        /// <summary>扩展属性（JSON 字符串）</summary>
        public string? Properties { get; set; }

        /// <summary>权限列表</summary>
        public List<Permission> Permissions { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
