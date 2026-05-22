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

        /// <summary>
        /// 获取部门下的所有用户
        /// </summary>
        Task<List<DepartmentUserInfo>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户添加到部门
        /// </summary>
        Task<bool> AddUserToDepartmentAsync(int departmentId, int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户从部门移除（DepartmentId 设为 null）
        /// </summary>
        Task<bool> RemoveUserFromDepartmentAsync(int userId, CancellationToken cancellationToken = default);
    }
}
