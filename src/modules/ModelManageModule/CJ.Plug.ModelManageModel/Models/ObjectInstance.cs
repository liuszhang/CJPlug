namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 对象实例 — 本体的具体实例化对象（参考Palantir Objects）
    /// </summary>
    public class ObjectInstance
    {
        public int Id { get; set; }

        /// <summary>所属本体ID</summary>
        public int OntologyId { get; set; }

        /// <summary>实例名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>实例描述</summary>
        public string? Description { get; set; }

        /// <summary>
        /// 属性值（JSON 对象格式，键为属性编码，值为属性值）
        /// </summary>
        public string PropertiesJson { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public string? CreatedBy { get; set; }
    }
}
