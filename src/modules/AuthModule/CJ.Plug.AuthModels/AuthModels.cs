namespace CJ.Plug.AuthModels
{
    /// <summary>
    /// 授权请求状态
    /// </summary>
    public enum AuthRequestStatus
    {
        Pending = 0,    // 待审批
        Approved = 1,   // 已批准
        Rejected = 2,   // 已拒绝
        Cancelled = 3   // 已撤回
    }

    /// <summary>
    /// 授权操作类型
    /// </summary>
    public enum AuthOperationType
    {
        CreateUser,
        UpdateUser,
        DeleteUser,
        CreateRole,
        UpdateRole,
        DeleteRole,
        CreateDepartment,
        UpdateDepartment,
        DeleteDepartment,
        CreateGroup,
        UpdateGroup,
        DeleteGroup
    }

    /// <summary>
    /// 授权请求
    /// </summary>
    public class AuthRequest
    {
        public int Id { get; set; }
        
        /// <summary>
        /// 请求类型
        /// </summary>
        public AuthOperationType OperationType { get; set; }
        
        /// <summary>
        /// 操作目标描述（如：用户admin、角色管理员、部门研发部）
        /// </summary>
        public string TargetDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作详情JSON（存储原始请求数据）
        /// </summary>
        public string OperationData { get; set; } = string.Empty;
        
        /// <summary>
        /// 关联的目标数据ID（创建操作时记录）
        /// </summary>
        public int TargetId { get; set; }
        
        /// <summary>
        /// 请求人用户名
        /// </summary>
        public string RequestedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// 请求时间
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 审批人用户名
        /// </summary>
        public string? ApprovedBy { get; set; }
        
        /// <summary>
        /// 审批时间
        /// </summary>
        public DateTime? ApprovedAt { get; set; }
        
        /// <summary>
        /// 状态
        /// </summary>
        public AuthRequestStatus Status { get; set; } = AuthRequestStatus.Pending;
        
        /// <summary>
        /// 审批备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建授权请求DTO
    /// </summary>
    public class CreateAuthRequestDto
    {
        public AuthOperationType OperationType { get; set; }
        public string TargetDescription { get; set; } = string.Empty;
        public string OperationData { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// 审批请求DTO
    /// </summary>
    public class ApproveAuthRequestDto
    {
        public int RequestId { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 授权请求查询DTO
    /// </summary>
    public class AuthRequestDto
    {
        public int Id { get; set; }
        public AuthOperationType OperationType { get; set; }
        public string OperationTypeName => OperationType switch
        {
            AuthOperationType.CreateUser => "创建用户",
            AuthOperationType.UpdateUser => "编辑用户",
            AuthOperationType.DeleteUser => "删除用户",
            AuthOperationType.CreateRole => "创建角色",
            AuthOperationType.UpdateRole => "编辑角色",
            AuthOperationType.DeleteRole => "删除角色",
            AuthOperationType.CreateDepartment => "创建部门",
            AuthOperationType.UpdateDepartment => "编辑部门",
            AuthOperationType.DeleteDepartment => "删除部门",
            AuthOperationType.CreateGroup => "创建用户组",
            AuthOperationType.UpdateGroup => "编辑用户组",
            AuthOperationType.DeleteGroup => "删除用户组",
            _ => "未知操作"
        };
        public string TargetDescription { get; set; } = string.Empty;
        public string OperationData { get; set; } = string.Empty;
        /// <summary>
        /// 关联的目标数据ID
        /// </summary>
        public int TargetId { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public AuthRequestStatus Status { get; set; }
        public string StatusName => Status switch
        {
            AuthRequestStatus.Pending => "待审批",
            AuthRequestStatus.Approved => "已批准",
            AuthRequestStatus.Rejected => "已拒绝",
            AuthRequestStatus.Cancelled => "已撤回",
            _ => "未知"
        };
        public string? Remark { get; set; }
    }
}
