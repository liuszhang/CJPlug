using CJ.Plug.UserManageModels;
using System.Net.Http.Json;

namespace CJ.Plug.UserManageApiClient
{
    public class RoleManageApiClient : BaseApiClient, IRoleManageApiClient
    {
        public RoleManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<List<RoleManageDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<RoleManageDto>>(
                "/api/role/getAll", cancellationToken);
            return result ?? [];
        }

        public async Task<RoleManageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<RoleManageDto>(
                $"/api/role/getById/{id}", cancellationToken);
        }

        public async Task<RoleManageDto?> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/role/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RoleManageDto>(cancellationToken: cancellationToken);
        }

        public async Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/role/update", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RoleManageDto>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"/api/role/delete/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
    }
}
