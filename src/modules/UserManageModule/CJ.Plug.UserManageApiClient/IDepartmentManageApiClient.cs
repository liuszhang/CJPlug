using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApiClient
{
    public interface IDepartmentManageApiClient
    {
        Task<List<DepartmentManageDto>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> UpdateDepartmentAsync(UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default);
    }
}
