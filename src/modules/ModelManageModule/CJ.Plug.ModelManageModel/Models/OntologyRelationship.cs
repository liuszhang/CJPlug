using System.ComponentModel.DataAnnotations;

namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 本体关系定义 — 定义本体类型之间的链接关系（参考Palantir Link Types）
    /// </summary>
    public class OntologyRelationship
    {
        public int Id { get; set; }

        /// <summary>所属源本体ID</summary>
        public int SourceOntologyId { get; set; }

        /// <summary>目标本体ID</summary>
        public int TargetOntologyId { get; set; }

        /// <summary>关系名称（如"包含"、"依赖"、"关联"）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>关系显示名称</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>关系编码（唯一标识，如 Contains、DependsOn）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>基数类型：OneToOne / OneToMany / ManyToMany</summary>
        public string Cardinality { get; set; } = "OneToMany";

        /// <summary>关系类型（关联基础枚举项 Code，如 Association、Dependency）</summary>
        public string? RelationshipType { get; set; }

        /// <summary>反向关系名称（如"被包含"、"被依赖"）</summary>
        public string? InverseName { get; set; }

        /// <summary>是否必填</summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>关系描述</summary>
        public string? Description { get; set; }

        /// <summary>排序</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
