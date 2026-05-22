using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApi.Contracts
{
    public interface IRolePermissionService
    {
        /// <summary>
        /// 获取所有已注册的功能权限定义（从各模块收集）
        /// </summary>
        Task<List<FunctionPermissionDefinitionDto>> GetAllPermissionDefinitionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定角色的完整配置（成员、功能权限、数据权限）
        /// </summary>
        Task<RoleConfigDto?> GetRoleConfigAsync(int roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定角色的功能权限
        /// </summary>
        Task<List<RoleFunctionPermissionDto>> GetRoleFunctionPermissionsAsync(int roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存角色功能权限
        /// </summary>
        Task<bool> SaveRoleFunctionPermissionsAsync(SaveRoleFunctionPermissionsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定角色的数据权限
        /// </summary>
        Task<List<RoleDataPermissionDto>> GetRoleDataPermissionsAsync(int roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存角色数据权限
        /// </summary>
        Task<bool> SaveRoleDataPermissionsAsync(SaveRoleDataPermissionsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色成员用户ID列表
        /// </summary>
        Task<List<int>> GetRoleMemberIdsAsync(int roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存角色成员
        /// </summary>
        Task<bool> SaveRoleMembersAsync(SaveRoleMembersRequest request, CancellationToken cancellationToken = default);
    }
}
