using CJ.Plug.UserManageModels;
using System.Net.Http.Json;

namespace CJ.Plug.UserManageApiClient
{
    public class DepartmentManageApiClient : BaseApiClient, IDepartmentManageApiClient
    {
        public DepartmentManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<List<DepartmentManageDto>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<DepartmentManageDto>>(
                "/api/department/getAll", cancellationToken);
            return result ?? [];
        }

        public async Task<DepartmentManageDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<DepartmentManageDto>(
                $"/api/department/getById/{id}", cancellationToken);
        }

        public async Task<DepartmentManageDto?> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/department/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DepartmentManageDto>(cancellationToken: cancellationToken);
        }

        public async Task<DepartmentManageDto?> UpdateDepartmentAsync(UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/department/update", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DepartmentManageDto>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"/api/department/delete/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<DepartmentUserInfo>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<DepartmentUserInfo>>(
                $"/api/department/getUsers/{departmentId}", cancellationToken);
            return result ?? [];
        }

        public async Task<bool> AddUserToDepartmentAsync(int departmentId, int userId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/department/addUser",
                new AddDepartmentUserRequest { DepartmentId = departmentId, UserId = userId }, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveUserFromDepartmentAsync(int userId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/department/removeUser",
                new RemoveDepartmentUserRequest { UserId = userId }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
    }
}
