namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 对象行为定义 — 定义可对本体对象执行的动作（简化版）
    /// </summary>
    public class ObjectBehavior
    {
        public int Id { get; set; }

        /// <summary>所属本体ID</summary>
        public int OntologyId { get; set; }

        /// <summary>行为名称</summary>
        public string? Name { get; set; }

        /// <summary>行为描述</summary>
        public string? Description { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>排序</summary>
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
