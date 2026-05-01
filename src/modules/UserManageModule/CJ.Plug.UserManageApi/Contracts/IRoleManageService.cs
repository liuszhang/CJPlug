using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApi.Contracts
{
    public interface IRoleManageService
    {
        Task<List<RoleManageDto>> GetAllAsync();
        Task<RoleManageDto?> GetByIdAsync(int id);
        Task<RoleManageDto?> CreateAsync(CreateRoleRequest request);
        Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
