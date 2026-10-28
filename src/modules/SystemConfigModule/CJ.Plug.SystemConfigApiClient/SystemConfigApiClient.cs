using CJ.Plug.SystemConfigModel.Models;
using System.Net.Http.Json;

namespace CJ.Plug.SystemConfigApiClient;

public class SystemConfigApiClient : BaseApiClient, ISystemConfigApiClient
{
    public SystemConfigApiClient(HttpClient dispatcherClient) : base(dispatcherClient) { }

    public async Task<List<SystemConfigItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<SystemConfigItem>>("/api/systemconfig/getAll", ct) ?? new List<SystemConfigItem>();
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/systemconfig/get/{key}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<string>(cancellationToken: ct);
    }

    public async Task<SystemConfigItem?> SetValueAsync(string key, string value, string? description = null, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/systemconfig/set", new { key, value, description }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SystemConfigItem>(cancellationToken: ct);
    }
}
