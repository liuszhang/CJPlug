using CJ.Plug.UserManageModels;
using System.Net.Http.Json;

namespace CJ.Plug.UserManageApiClient
{
    public class RolePermissionApiClient : BaseApiClient, IRolePermissionApiClient
    {
        public RolePermissionApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<List<FunctionPermissionDefinitionDto>> GetAllPermissionDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<FunctionPermissionDefinitionDto>>(
                "/api/role-permission/permission-definitions", cancellationToken);
            return result ?? [];
        }

        public async Task<RoleConfigDto?> GetRoleConfigAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<RoleConfigDto>(
                $"/api/role-permission/config/{roleId}", cancellationToken);
        }

        public async Task<List<RoleFunctionPermissionDto>> GetRoleFunctionPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<RoleFunctionPermissionDto>>(
                $"/api/role-permission/function-permissions/{roleId}", cancellationToken);
            return result ?? [];
        }

        public async Task<bool> SaveRoleFunctionPermissionsAsync(SaveRoleFunctionPermissionsRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/role-permission/function-permissions", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<RoleDataPermissionDto>> GetRoleDataPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<RoleDataPermissionDto>>(
                $"/api/role-permission/data-permissions/{roleId}", cancellationToken);
            return result ?? [];
        }

        public async Task<bool> SaveRoleDataPermissionsAsync(SaveRoleDataPermissionsRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/role-permission/data-permissions", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<int>> GetRoleMemberIdsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<int>>(
                $"/api/role-permission/members/{roleId}", cancellationToken);
            return result ?? [];
        }

        public async Task<bool> SaveRoleMembersAsync(SaveRoleMembersRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/role-permission/members", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
    }
}
