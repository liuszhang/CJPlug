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

        /// <summary>
        /// 获取部门下的所有用户
        /// </summary>
        Task<List<DepartmentUserInfo>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户添加到部门
        /// </summary>
        Task<bool> AddUserToDepartmentAsync(int departmentId, int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户从部门移除
        /// </summary>
        Task<bool> RemoveUserFromDepartmentAsync(int userId, CancellationToken cancellationToken = default);
    }
}
