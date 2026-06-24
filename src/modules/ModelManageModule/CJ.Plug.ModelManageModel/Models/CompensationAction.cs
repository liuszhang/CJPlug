namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M6 补偿动作 — 定义异常发生后的补偿/恢复策略
    /// </summary>
    public class CompensationAction
    {
        public int Id { get; set; }

        /// <summary>动作名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>动作编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>动作描述</summary>
        public string? Description { get; set; }

        /// <summary>关联异常类型ID（FK → ExceptionType）</summary>
        public int? ExceptionTypeId { get; set; }

        /// <summary>关联异常类型导航属性</summary>
        public ExceptionType? ExceptionType { get; set; }

        /// <summary>动作类型：Retry / Rollback / Notify / Skip / Escalate / Custom</summary>
        public string? ActionType { get; set; }

        /// <summary>动作配置（JSON 字符串）</summary>
        public string? ActionConfig { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
