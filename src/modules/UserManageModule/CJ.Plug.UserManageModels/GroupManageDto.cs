namespace CJ.Plug.UserManageModels;

/// <summary>
/// 用户组管理 DTO
/// </summary>
public class GroupManageDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;
    public DataStatus Status { get; set; } = DataStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Creator { get; set; }
    
    /// <summary>
    /// 用户组成员数量
    /// </summary>
    public int MemberCount { get; set; }
}

/// <summary>
/// 创建用户组请求
/// </summary>
public class CreateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DataStatus Status { get; set; } = DataStatus.Active;
    public string? Creator { get; set; }
}

/// <summary>
/// 更新用户组请求
/// </summary>
public class UpdateGroupRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DataStatus Status { get; set; } = DataStatus.Active;
}

/// <summary>
/// 用户组成员管理 - 添加用户到用户组
/// </summary>
public class AddGroupUserRequest
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// 用户组成员管理 - 从用户组移除用户
/// </summary>
public class RemoveGroupUserRequest
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// 用户组成员信息（轻量DTO，用于成员列表展示）
/// </summary>
public class GroupUserInfo
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
/// 用户组列表（用于用户详情页面）
/// </summary>
public class UserGroupInfo
{
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? GroupDescription { get; set; }
}
