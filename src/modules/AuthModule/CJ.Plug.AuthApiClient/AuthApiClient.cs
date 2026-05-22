using CJ.Plug.AuthModels;
using CJ.Plug.Models;
using System.Net.Http.Json;

namespace CJ.Plug.AuthApiClient
{
    public interface IAuthApiClient
    {
        Task<List<AuthRequestDto>> GetAllAuthRequestAsync(AuthRequestStatus? status = null, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<AuthRequestDto> CreateAsync(CreateAuthRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> ApproveAsync(ApproveAuthRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> CancelAsync(int id, string cancelledBy, CancellationToken cancellationToken = default);
        Task<bool> HasPendingRequestAsync(AuthOperationType operationType, string target, CancellationToken cancellationToken = default);
    }

    public class AuthApiClient : BaseApiClient, IAuthApiClient
    {
        public AuthApiClient(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<AuthRequestDto>> GetAllAuthRequestAsync(AuthRequestStatus? status = null, CancellationToken cancellationToken = default)
        {
            var url = "/api/auth/getAll";
            if (status.HasValue)
                url += $"?status={status.Value}";
            return await httpClient.GetFromJsonAsync<List<AuthRequestDto>>(url, cancellationToken) ?? [];
        }

        public async Task<AuthRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<AuthRequestDto>($"/api/auth/getById/{id}", cancellationToken);
        }

        public async Task<AuthRequestDto> CreateAsync(CreateAuthRequestDto request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthRequestDto>(cancellationToken: cancellationToken))!;
        }

        public async Task<AuthRequestDto?> ApproveAsync(ApproveAuthRequestDto request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/approve", request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AuthRequestDto>(cancellationToken: cancellationToken);
        }

        public async Task<AuthRequestDto?> CancelAsync(int id, string cancelledBy, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync($"/api/auth/cancel/{id}", new { cancelledBy }, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AuthRequestDto>(cancellationToken: cancellationToken);
        }

        public async Task<bool> HasPendingRequestAsync(AuthOperationType operationType, string target, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<bool>($"/api/auth/hasPending?operationType={operationType}&target={target}", cancellationToken);
        }
    }
}
