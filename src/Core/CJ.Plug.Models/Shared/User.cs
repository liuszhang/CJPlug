using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    /// <summary>
    /// 用户模型类
    /// </summary>
    public class User : IdentityUser<int>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public int? DepartmentId { get; set; }
        public string? Token { get; set; }
        public string? Phone { get; set; }
        /// <summary>
        /// 状态：-1=授权中，0=禁用，1=启用
        /// </summary>
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否系统内置数据（不可删除和编辑）
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 用户组成员列表（导航属性）
        /// </summary>
        public virtual ICollection<UserGroupMember> GroupMembers { get; set; } = new List<UserGroupMember>();

        /// <summary>
        /// 用户组列表（通过 UserGroupMember 导航）
        /// </summary>
        public virtual ICollection<UserGroup> Groups => GroupMembers.Select(m => m.UserGroup).ToList();
    }
}
