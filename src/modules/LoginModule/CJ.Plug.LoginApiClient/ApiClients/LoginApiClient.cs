using CJ.Plug.Models.Shared;
using System.Net.Http.Json;

namespace CJ.Plug.LoginApiClient.ApiClients
{
    public class LoginApiClient:ILoginApiClient
    {
        private HttpClient httpClient = new();
        private readonly HttpClient DispatcherClient;


        public LoginApiClient(HttpClient dispatcherClient)
        {
            DispatcherClient = dispatcherClient;
            httpClient.BaseAddress = new Uri(DispatcherClient.GetStringAsync("api/dispatch/GetApiServer").Result);
        }


        public async Task<User?> Login(User user, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/user/sigin", user, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User?>(cancellationToken: cancellationToken);
        }

        public async Task<User?> Register(User user, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/user/sigup", user, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>(cancellationToken: cancellationToken);
        }

        public async Task Logout(string userId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"/api/user/logout/{userId}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
