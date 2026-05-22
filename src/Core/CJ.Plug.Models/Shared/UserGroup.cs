using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.Models.Shared;

/// <summary>
/// 用户组（角色组）- 用于组织用户，一个用户可以属于多个用户组
/// </summary>
public class UserGroup : IdentityRole<int>
{
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态：-1=授权中，0=禁用，1=启用
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 是否系统内置用户组（不可删除和编辑）
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// 创建人
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 用户组成员列表（导航属性）
    /// </summary>
    public virtual ICollection<UserGroupMember> GroupMembers { get; set; } = new List<UserGroupMember>();

    /// <summary>
    /// 用户列表（通过 UserGroupMember 导航）
    /// </summary>
    public virtual ICollection<User> Users => GroupMembers.Select(m => m.User).ToList();
}
