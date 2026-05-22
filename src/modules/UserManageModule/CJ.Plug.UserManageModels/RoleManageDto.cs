namespace CJ.Plug.UserManageModels
{
    public class RoleManageDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
        public bool IsSystem { get; set; } = false;
        public DataStatus Status { get; set; } = DataStatus.Active;
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CreateRoleRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    public class UpdateRoleRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    /// <summary>
    /// 角色人员信息（轻量DTO，用于角色人员列表展示）
    /// </summary>
    public class RoleUserInfo
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    /// <summary>
    /// 将用户添加到角色
    /// </summary>
    public class AddRoleToUserRequest
    {
        public int RoleId { get; set; }
        public int UserId { get; set; }
    }

    /// <summary>
    /// 从角色中移除用户
    /// </summary>
    public class RemoveRoleUserRequest
    {
        public int RoleId { get; set; }
        public int UserId { get; set; }
    }
}
