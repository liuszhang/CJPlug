using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApi.Contracts
{
    public interface IDepartmentManageService
    {
        Task<List<DepartmentManageDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<DepartmentManageDto?> UpdateAsync(UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
