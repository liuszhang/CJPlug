namespace CJ.Plug.AuditApi.DbContext
{
    /// <summary>
    /// 审计日志数据库实体
    /// </summary>
    public class AuditLogEntity
    {
        public long Id { get; set; }
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public int OperationType { get; set; }
        
        /// <summary>
        /// 操作模块
        /// </summary>
        public int Module { get; set; }
        
        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作详情（JSON格式）
        /// </summary>
        public string? Detail { get; set; }
        
        /// <summary>
        /// 操作IP地址
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// 操作结果（成功/失败）
        /// </summary>
        public bool IsSuccess { get; set; } = true;
        
        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
