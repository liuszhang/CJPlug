using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;

public partial class MainApiClient : IRolePermissionApiClient
{
    async Task<List<FunctionPermissionDefinitionDto>> IRolePermissionApiClient.GetAllPermissionDefinitionsAsync(CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.GetAllPermissionDefinitionsAsync(cancellationToken);
    }

    async Task<RoleConfigDto?> IRolePermissionApiClient.GetRoleConfigAsync(int roleId, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.GetRoleConfigAsync(roleId, cancellationToken);
    }

    async Task<List<RoleFunctionPermissionDto>> IRolePermissionApiClient.GetRoleFunctionPermissionsAsync(int roleId, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.GetRoleFunctionPermissionsAsync(roleId, cancellationToken);
    }

    async Task<bool> IRolePermissionApiClient.SaveRoleFunctionPermissionsAsync(SaveRoleFunctionPermissionsRequest request, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.SaveRoleFunctionPermissionsAsync(request, cancellationToken);
    }

    async Task<List<RoleDataPermissionDto>> IRolePermissionApiClient.GetRoleDataPermissionsAsync(int roleId, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.GetRoleDataPermissionsAsync(roleId, cancellationToken);
    }

    async Task<bool> IRolePermissionApiClient.SaveRoleDataPermissionsAsync(SaveRoleDataPermissionsRequest request, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.SaveRoleDataPermissionsAsync(request, cancellationToken);
    }

    async Task<List<int>> IRolePermissionApiClient.GetRoleMemberIdsAsync(int roleId, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.GetRoleMemberIdsAsync(roleId, cancellationToken);
    }

    async Task<bool> IRolePermissionApiClient.SaveRoleMembersAsync(SaveRoleMembersRequest request, CancellationToken cancellationToken)
    {
        return await RolePermissionApiClient.Value.SaveRoleMembersAsync(request, cancellationToken);
    }
}
