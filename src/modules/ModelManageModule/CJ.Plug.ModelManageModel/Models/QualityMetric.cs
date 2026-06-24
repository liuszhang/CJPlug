namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M7 质量指标 — 定义系统质量度量指标
    /// </summary>
    public class QualityMetric
    {
        public int Id { get; set; }

        /// <summary>指标名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>指标编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>指标描述</summary>
        public string? Description { get; set; }

        /// <summary>度量单位</summary>
        public string? Unit { get; set; }

        /// <summary>目标值（字符串，支持数值/范围）</summary>
        public string? TargetValue { get; set; }

        /// <summary>当前值（字符串）</summary>
        public string? CurrentValue { get; set; }

        /// <summary>度量类型：Throughput / Latency / ErrorRate / Availability / Accuracy</summary>
        public string? MeasureType { get; set; }

        /// <summary>关联本体ID（可选）</summary>
        public int? OntologyId { get; set; }

        /// <summary>告警规则列表</summary>
        public List<AlertRule> AlertRules { get; set; } = new();

        /// <summary>改进措施列表</summary>
        public List<ImprovementAction> ImprovementActions { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
