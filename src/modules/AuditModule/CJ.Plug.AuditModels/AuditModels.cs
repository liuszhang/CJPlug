namespace CJ.Plug.AuditModels
{
    /// <summary>
    /// 操作类型枚举
    /// </summary>
    public enum AuditOperationType
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        Login = 0,
        
        /// <summary>
        /// 用户登出
        /// </summary>
        Logout = 1,
        
        /// <summary>
        /// 创建操作
        /// </summary>
        Create = 10,
        
        /// <summary>
        /// 更新操作
        /// </summary>
        Update = 11,
        
        /// <summary>
        /// 删除操作
        /// </summary>
        Delete = 12,
        
        /// <summary>
        /// 授权审批
        /// </summary>
        Approve = 20,
        
        /// <summary>
        /// 授权拒绝
        /// </summary>
        Reject = 21,
        
        /// <summary>
        /// 授权撤回
        /// </summary>
        Cancel = 22,
        
        /// <summary>
        /// 流程执行
        /// </summary>
        Execute = 30,
        
        /// <summary>
        /// 其他操作
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// 操作模块枚举
    /// </summary>
    public enum AuditModule
    {
        /// <summary>
        /// 用户管理
        /// </summary>
        UserManage = 0,
        
        /// <summary>
        /// 角色管理
        /// </summary>
        RoleManage = 1,
        
        /// <summary>
        /// 部门管理
        /// </summary>
        DepartmentManage = 2,
        
        /// <summary>
        /// 授权管理
        /// </summary>
        AuthManage = 3,
        
        /// <summary>
        /// 流程管理
        /// </summary>
        ProcessManage = 4,
        
        /// <summary>
        /// 插件管理
        /// </summary>
        PlugManage = 5,
        
        /// <summary>
        /// 系统设置
        /// </summary>
        System = 9,

        /// <summary>
        /// 用户组管理
        /// </summary>
        UserGroupManage= 10,
        
        /// <summary>
        /// 其他
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// 审计日志DTO
    /// </summary>
    public class AuditLogDto
    {
        public long Id { get; set; }
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; }
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public AuditOperationType OperationType { get; set; }
        
        /// <summary>
        /// 操作类型名称
        /// </summary>
        public string OperationTypeName => OperationType switch
        {
            AuditOperationType.Login => "登录",
            AuditOperationType.Logout => "登出",
            AuditOperationType.Create => "创建",
            AuditOperationType.Update => "更新",
            AuditOperationType.Delete => "删除",
            AuditOperationType.Approve => "审批通过",
            AuditOperationType.Reject => "审批拒绝",
            AuditOperationType.Cancel => "撤回",
            AuditOperationType.Execute => "执行",
            AuditOperationType.Other => "其他",
            _ => OperationType.ToString()
        };
        
        /// <summary>
        /// 操作模块
        /// </summary>
        public AuditModule Module { get; set; }
        
        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName => Module switch
        {
            AuditModule.UserManage => "用户管理",
            AuditModule.RoleManage => "角色管理",
            AuditModule.DepartmentManage => "部门管理",
            AuditModule.AuthManage => "授权管理",
            AuditModule.ProcessManage => "流程管理",
            AuditModule.PlugManage => "插件管理",
            AuditModule.System => "系统设置",
            AuditModule.UserGroupManage => "用户组管理",
            AuditModule.Other => "其他",
            _ => Module.ToString()
        };
        
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

    /// <summary>
    /// 创建审计日志请求
    /// </summary>
    public class CreateAuditLogRequest
    {
        public string UserName { get; set; } = string.Empty;
        public AuditOperationType OperationType { get; set; }
        public AuditModule Module { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? IpAddress { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 审计日志查询请求
    /// </summary>
    public class AuditLogQueryRequest
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string? UserName { get; set; }
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public AuditOperationType? OperationType { get; set; }
        
        /// <summary>
        /// 操作模块
        /// </summary>
        public AuditModule? Module { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool? IsSuccess { get; set; }
        
        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
