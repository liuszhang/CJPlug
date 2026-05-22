namespace CJ.Plug.UserManageModels
{
    public class DepartmentManageDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
        public string? Creator { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CreateDepartmentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
        public string? Creator { get; set; }
    }

    public class UpdateDepartmentRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    /// <summary>
    /// 部门人员管理 - 将用户添加到部门
    /// </summary>
    public class AddDepartmentUserRequest
    {
        public int DepartmentId { get; set; }
        public int UserId { get; set; }
    }

    /// <summary>
    /// 部门人员管理 - 从部门移除用户（将 DepartmentId 设为 null）
    /// </summary>
    public class RemoveDepartmentUserRequest
    {
        public int UserId { get; set; }
    }

    /// <summary>
    /// 部门人员信息（轻量DTO，用于人员列表展示）
    /// </summary>
    public class DepartmentUserInfo
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }
}
