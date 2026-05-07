using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.UserManageApiClient
{
    public interface IUserManageApiClient
    {
        Task<IEnumerable<User?>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<User?> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<User?> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default);
        Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    }
}
