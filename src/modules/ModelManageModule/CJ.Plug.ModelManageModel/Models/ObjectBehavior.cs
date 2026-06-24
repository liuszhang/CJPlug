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

        /// <summary>行为动作类型（Submit/Button/Reset/Navigate/Custom）</summary>
        public ActionType? ActionType { get; set; } = Models.ActionType.Submit;

        /// <summary>API 调用地址（Submit/Custom 类型时使用）</summary>
        public string? ApiUrl { get; set; }

        /// <summary>执行前确认提示消息</summary>
        public string? ConfirmMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
