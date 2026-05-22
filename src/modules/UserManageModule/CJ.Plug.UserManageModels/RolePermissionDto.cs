using System;
using System.Collections.Generic;

namespace CJ.Plug.UserManageModels
{
    // ===================== 实体 =====================

    /// <summary>
    /// 角色功能权限实体 - 存储角色拥有的功能权限
    /// </summary>
    public class RoleFunctionPermission
    {
        public int Id { get; set; }

        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 权限标识（如：Process.Create）
        /// </summary>
        public string PermissionKey { get; set; } = string.Empty;

        /// <summary>
        /// 权限显示名称（如：创建流程）
        /// </summary>
        public string PermissionName { get; set; } = string.Empty;

        /// <summary>
        /// 所属模块名称
        /// </summary>
        public string ModuleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 角色数据权限实体 - 存储角色的数据范围权限
    /// </summary>
    public class RoleDataPermission
    {
        public int Id { get; set; }

        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 数据范围类型（All=全部, Department=本部门, Self=仅本人）
        /// </summary>
        public string DataScope { get; set; } = "Self";

        /// <summary>
        /// 关联部门ID（当DataScope=Department时使用）
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 数据资源类型（如：Process, Job, PDZ等）
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;
    }

    // ===================== DTO =====================

    /// <summary>
    /// 角色配置DTO - 完整的角色配置信息
    /// </summary>
    public class RoleConfigDto
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? RoleDescription { get; set; }
        public string? RoleType { get; set; }

        /// <summary>
        /// 角色成员用户ID列表
        /// </summary>
        public List<int> MemberUserIds { get; set; } = [];

        /// <summary>
        /// 功能权限列表
        /// </summary>
        public List<RoleFunctionPermissionDto> FunctionPermissions { get; set; } = [];

        /// <summary>
        /// 数据权限列表
        /// </summary>
        public List<RoleDataPermissionDto> DataPermissions { get; set; } = [];
    }

    public class RoleFunctionPermissionDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
    }

    public class RoleDataPermissionDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string DataScope { get; set; } = "Self";
        public int? DepartmentId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 功能权限定义DTO（从各模块收集）
    /// </summary>
    public class FunctionPermissionDefinitionDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public string? ModuleIcon { get; set; }
        public List<FunctionPermissionItemDto> Permissions { get; set; } = [];
    }

    public class FunctionPermissionItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; }
    }

    /// <summary>
    /// 保存角色功能权限请求
    /// </summary>
    public class SaveRoleFunctionPermissionsRequest
    {
        public int RoleId { get; set; }
        public List<string> PermissionKeys { get; set; } = [];
    }

    /// <summary>
    /// 保存角色数据权限请求
    /// </summary>
    public class SaveRoleDataPermissionsRequest
    {
        public int RoleId { get; set; }
        public List<RoleDataPermissionDto> Permissions { get; set; } = [];
    }

    /// <summary>
    /// 保存角色成员请求
    /// </summary>
    public class SaveRoleMembersRequest
    {
        public int RoleId { get; set; }
        public List<int> UserIds { get; set; } = [];
    }

    /// <summary>
    /// 数据范围枚举
    /// </summary>
    public enum DataScopeType
    {
        /// <summary>
        /// 全部数据
        /// </summary>
        All = 0,

        /// <summary>
        /// 本部门数据
        /// </summary>
        Department = 1,

        /// <summary>
        /// 仅本人数据
        /// </summary>
        Self = 2
    }
}
