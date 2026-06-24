namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M7 改进措施 — 定义质量指标的改进行动计划
    /// </summary>
    public class ImprovementAction
    {
        public int Id { get; set; }

        /// <summary>措施名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>措施编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>措施描述</summary>
        public string? Description { get; set; }

        /// <summary>关联质量指标ID（FK → QualityMetric）</summary>
        public int QualityMetricId { get; set; }

        /// <summary>关联质量指标导航属性</summary>
        public QualityMetric? QualityMetric { get; set; }

        /// <summary>提出者</summary>
        public string? ProposedBy { get; set; }

        /// <summary>状态：Proposed / Approved / InProgress / Completed</summary>
        public string? Status { get; set; }

        /// <summary>执行结果（字符串）</summary>
        public string? Result { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
