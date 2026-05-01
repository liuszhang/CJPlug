using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApiClient
{
    public interface IRoleManageApiClient
    {
        Task<List<RoleManageDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<RoleManageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<RoleManageDto?> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
