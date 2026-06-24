namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// M6 异常类型 — 定义系统/业务/集成/安全领域的异常分类
    /// </summary>
    public class ExceptionType
    {
        public int Id { get; set; }

        /// <summary>异常名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>异常编码（唯一标识）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>异常描述</summary>
        public string? Description { get; set; }

        /// <summary>严重程度：Critical / High / Medium / Low</summary>
        public string? Severity { get; set; }

        /// <summary>异常类别：System / Business / Integration / Security</summary>
        public string? Category { get; set; }

        /// <summary>补偿动作列表</summary>
        public List<CompensationAction> CompensationActions { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
