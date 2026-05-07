using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApi.Contracts
{
    public interface IUserManageService : IBaseRepositoryService<User, int>
    {
        Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<User?> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<User?> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default);
        Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    }
}
