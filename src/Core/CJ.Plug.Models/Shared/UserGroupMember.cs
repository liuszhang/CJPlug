namespace CJ.Plug.Models.Shared;

/// <summary>
/// 用户组成员 - 用户和用户组的多对多关系映射
/// </summary>
public class UserGroupMember
{
    /// <summary>
    /// 用户组 ID
    /// </summary>
    public int UserGroupId { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否管理员（用户组内）
    /// </summary>
    public bool IsAdmin { get; set; } = false;

    /// <summary>
    /// 用户导航属性
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 用户组导航属性
    /// </summary>
    public virtual UserGroup? UserGroup { get; set; }
}
