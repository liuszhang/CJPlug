using CJ.Plug.Models.Shared;
using System.Net.Http.Json;

namespace CJ.Plug.LoginApiClient.ApiClients
{
    public interface ILoginApiClient
    {
        Task<User?> Login(User user, CancellationToken cancellationToken = default);
        Task Logout(string userId, CancellationToken cancellationToken = default);
        Task<User?> Register(User user, CancellationToken cancellationToken = default);
    }
}
