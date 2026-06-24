namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 属性约束定义 — 属性字段的细粒度验证约束（参考 CJOntology PropertyConstraint）
    /// </summary>
    public class PropertyConstraint
    {
        public int Id { get; set; }

        /// <summary>所属属性ID</summary>
        public int PropertyId { get; set; }

        /// <summary>约束类型</summary>
        public ConstraintType ConstraintType { get; set; }

        /// <summary>约束值（如正则表达式、最大长度值等）</summary>
        public string? Value { get; set; }

        /// <summary>违反约束时的提示消息</summary>
        public string? Message { get; set; }

        /// <summary>排序</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>导航属性 — 所属属性</summary>
        public Property? Property { get; set; }
    }
}
