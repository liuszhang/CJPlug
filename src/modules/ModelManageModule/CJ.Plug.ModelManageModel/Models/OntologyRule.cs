namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 本体规则定义 — 定义验证、约束、推导及业务规则（参考Palantir Rule-Based Governance）
    /// </summary>
    public class OntologyRule
    {
        public int Id { get; set; }

        /// <summary>规则名称</summary>
        public string? Name { get; set; }

        /// <summary>规则描述</summary>
        public string? Description { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>生效开始日期</summary>
        public DateTime? EffectiveFrom { get; set; }

        /// <summary>生效结束日期</summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>排序</summary>
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
