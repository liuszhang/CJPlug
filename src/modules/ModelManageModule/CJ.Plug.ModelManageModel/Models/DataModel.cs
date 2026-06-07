namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 本体定义 — 定义对象类型及其元数据（参考Palantir Object Types）
    /// </summary>
    public class Ontology
    {
        public int Id { get; set; }

        /// <summary>本体名称（如"文本文件"、"三维模型"等）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>本体显示名称</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>本体编码（唯一标识，如 TextFile、ThreeDModel）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>本体描述</summary>
        public string? Description { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>排序（数字越小越靠前）</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>本体版本号</summary>
        public string Version { get; set; } = "1.0";

        /// <summary>分类标签</summary>
        public string? Category { get; set; }

        /// <summary>是否系统内置</summary>
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public string? CreatedBy { get; set; }

        /// <summary>该本体定义下的属性字段列表</summary>
        public List<Property> Properties { get; set; } = new();

        /// <summary>该本体作为源的关系列表</summary>
        public List<OntologyRelationship> OutgoingRelationships { get; set; } = new();

        /// <summary>该本体作为目标的关系列表</summary>
        public List<OntologyRelationship> IncomingRelationships { get; set; } = new();

        /// <summary>该本体下的行为列表</summary>
        public List<ObjectBehavior> Behaviors { get; set; } = new();
    }
}
