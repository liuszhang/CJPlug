namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M4 场景 — 定义用例/场景的步骤编排（Steps 为 JSON 字符串数组）
    /// </summary>
    public class Scenario
    {
        public int Id { get; set; }

        /// <summary>场景名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>场景编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>场景描述</summary>
        public string? Description { get; set; }

        /// <summary>关联本体ID（FK → Ontology）</summary>
        public int? OntologyId { get; set; }

        /// <summary>
        /// 场景步骤（JSON 字符串数组）
        /// 格式：[{"StepOrder":1,"ActionId":"unlock","InputParams":{},"OutputParams":{},"Condition":"油门>30%"}, ...]
        /// </summary>
        public string? Steps { get; set; }

        /// <summary>是否激活</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
