using CJ.Plug.LicenseModels;
using System.Net.Http.Json;

namespace CJ.Plug.LicenseApiClient
{
    public interface ILicenseApiClient
    {
        Task<LicenseStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
        Task<LicenseStatusResponse> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default);
        Task<bool> RevokeAsync(CancellationToken cancellationToken = default);
        Task<GenerateLicenseResponse> GenerateAsync(GenerateLicenseRequest request, CancellationToken cancellationToken = default);
    }

    public class LicenseApiClient : BaseApiClient, ILicenseApiClient
    {
        public LicenseApiClient(HttpClient httpClient) : base(httpClient) { }

        public async Task<LicenseStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<LicenseStatusResponse>("/api/license/status", cancellationToken)
                ?? new LicenseStatusResponse { Message = "无法获取许可证状态" };
        }

        public async Task<LicenseStatusResponse> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/license/activate",
                new { licenseKey }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<LicenseStatusResponse>(cancellationToken: cancellationToken))!;
        }

        public async Task<bool> RevokeAsync(CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsync("/api/license/revoke", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<GenerateLicenseResponse> GenerateAsync(GenerateLicenseRequest request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/license/generate", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<GenerateLicenseResponse>(cancellationToken: cancellationToken))!;
        }

    }
}
