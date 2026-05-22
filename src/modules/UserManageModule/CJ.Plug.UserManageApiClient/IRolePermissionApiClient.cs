using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApiClient
{
    public interface IRolePermissionApiClient
    {
        Task<List<FunctionPermissionDefinitionDto>> GetAllPermissionDefinitionsAsync(CancellationToken cancellationToken = default);
        Task<RoleConfigDto?> GetRoleConfigAsync(int roleId, CancellationToken cancellationToken = default);
        Task<List<RoleFunctionPermissionDto>> GetRoleFunctionPermissionsAsync(int roleId, CancellationToken cancellationToken = default);
        Task<bool> SaveRoleFunctionPermissionsAsync(SaveRoleFunctionPermissionsRequest request, CancellationToken cancellationToken = default);
        Task<List<RoleDataPermissionDto>> GetRoleDataPermissionsAsync(int roleId, CancellationToken cancellationToken = default);
        Task<bool> SaveRoleDataPermissionsAsync(SaveRoleDataPermissionsRequest request, CancellationToken cancellationToken = default);
        Task<List<int>> GetRoleMemberIdsAsync(int roleId, CancellationToken cancellationToken = default);
        Task<bool> SaveRoleMembersAsync(SaveRoleMembersRequest request, CancellationToken cancellationToken = default);
    }
}
