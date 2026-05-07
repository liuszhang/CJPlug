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
    }
}
