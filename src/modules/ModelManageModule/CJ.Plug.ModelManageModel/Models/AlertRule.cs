namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M7 告警规则 — 定义质量指标的告警触发条件
    /// </summary>
    public class AlertRule
    {
        public int Id { get; set; }

        /// <summary>规则名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>规则编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>规则描述</summary>
        public string? Description { get; set; }

        /// <summary>关联质量指标ID（FK → QualityMetric）</summary>
        public int QualityMetricId { get; set; }

        /// <summary>关联质量指标导航属性</summary>
        public QualityMetric? QualityMetric { get; set; }

        /// <summary>比较条件：Above / Below / Equals</summary>
        public string? Condition { get; set; }

        /// <summary>告警阈值（字符串，支持数值/范围）</summary>
        public string? Threshold { get; set; }

        /// <summary>告警严重程度：Critical / High / Medium / Low</summary>
        public string? Severity { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
