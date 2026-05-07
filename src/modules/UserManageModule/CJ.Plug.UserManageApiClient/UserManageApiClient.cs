using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageModels;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.UserManageApiClient
{
    public partial class UserManageApiClient : BaseApiClient, IUserManageApiClient
    {
        public UserManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<IEnumerable<User?>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await httpClient.GetFromJsonAsync<IEnumerable<User?>>(
                requestUri: "/api/user/getAllUsers",
                cancellationToken: cancellationToken
            );

            return users ?? Enumerable.Empty<User?>();
        }

        public async Task<User?> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync(
                requestUri: "/api/user/createUser",
                value: request,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>(cancellationToken: cancellationToken);
            }

            return null;
        }

        public async Task<User?> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PutAsJsonAsync(
                requestUri: "/api/user/updateUser",
                value: request,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>(cancellationToken: cancellationToken);
            }

            return null;
        }

        public async Task<bool> AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync(
                requestUri: "/api/user/assignRoles",
                value: request,
                cancellationToken: cancellationToken
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetFromJsonAsync<List<string>>(
                requestUri: $"/api/user/getUserRoles/{userId}",
                cancellationToken: cancellationToken
            );

            return result ?? [];
        }
    }
}
