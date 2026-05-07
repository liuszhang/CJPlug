using CJ.Plug.AuditModels;
using CJ.Plug.Models;
using System.Net.Http.Json;

namespace CJ.Plug.AuditApiClient
{
    public class AuditApiClient : BaseApiClient, IAuditApiClient
    {
        public AuditApiClient(HttpClient httpClient) : base(httpClient) { }

        public async Task<AuditLogDto> LogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/audit/log", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuditLogDto>(cancellationToken: cancellationToken))!;
        }

        public async Task<PagedResult<AuditLogDto>> QueryAsync(AuditLogQueryRequest query, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/audit/query", query, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>(cancellationToken: cancellationToken))!;
        }

        public async Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<AuditLogDto>($"/api/audit/getById/{id}", cancellationToken);
        }

        public async Task<int> CleanupAsync(int daysToKeep, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"/api/audit/cleanup/{daysToKeep}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CleanupResult>(cancellationToken: cancellationToken);
            return result?.DeletedCount ?? 0;
        }

        private class CleanupResult
        {
            public int DeletedCount { get; set; }
        }
    }
}
