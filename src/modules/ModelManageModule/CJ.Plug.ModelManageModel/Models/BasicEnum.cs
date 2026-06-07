namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 基础枚举定义 — 可配置的枚举（如关系类型、基础属性类型等）
    /// </summary>
    public class BasicEnum
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        /// <summary>枚举编码（唯一，如 Relation）</summary>
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystem { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

        public List<BasicEnumItem> Items { get; set; } = new();
    }
}