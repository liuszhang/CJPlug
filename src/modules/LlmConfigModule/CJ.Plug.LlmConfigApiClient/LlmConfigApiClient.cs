using CJ.Plug.LlmConfigModel.Models;
using CJ.Plug.Models;
using System.Net.Http.Json;

namespace CJ.Plug.LlmConfigApiClient;

public class LlmConfigApiClient : BaseApiClient, ILlmConfigApiClient
{
    public LlmConfigApiClient(HttpClient dispatcherClient) : base(dispatcherClient) { }

    // 供应商 CRUD
    public async Task<IEnumerable<LlmProvider>> GetAllProvidersAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<LlmProvider>>("/api/llm/getAllProviders", ct) ?? new List<LlmProvider>();
    }

    public async Task<LlmProvider?> GetProviderByIdAsync(int id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<LlmProvider>($"/api/llm/getProviderById/{id}", ct);
    }

    public async Task<LlmProvider?> CreateProviderAsync(LlmProvider provider, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/llm/createProvider", provider, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LlmProvider>(cancellationToken: ct);
    }

    public async Task<LlmProvider?> UpdateProviderAsync(LlmProvider provider, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/llm/updateProvider", provider, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LlmProvider>(cancellationToken: ct);
    }

    public async Task<bool> DeleteProviderAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/llm/deleteProvider/{id}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
    }

    // 模型配置 CRUD
    public async Task<IEnumerable<LlmModelConfig>> GetAllModelConfigsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<LlmModelConfig>>("/api/llm/getAllModelConfigs", ct) ?? new List<LlmModelConfig>();
    }

    public async Task<IEnumerable<LlmModelConfig>> GetModelConfigsByProviderAsync(int providerId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<LlmModelConfig>>($"/api/llm/getModelConfigsByProvider/{providerId}", ct) ?? new List<LlmModelConfig>();
    }

    public async Task<LlmModelConfig?> GetModelConfigByIdAsync(int id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<LlmModelConfig>($"/api/llm/getModelConfigById/{id}", ct);
    }

    public async Task<LlmModelConfig?> CreateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/llm/createModelConfig", config, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LlmModelConfig>(cancellationToken: ct);
    }

    public async Task<LlmModelConfig?> UpdateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/llm/updateModelConfig", config, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LlmModelConfig>(cancellationToken: ct);
    }

    public async Task<bool> DeleteModelConfigAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/llm/deleteModelConfig/{id}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
    }

    // 获取默认模型信息
    public async Task<DefaultModelInfoResponse?> GetDefaultModelInfoAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<DefaultModelInfoResponse>("/api/llm/getDefaultModelInfo", ct);
    }

    // 设置默认模型
    public async Task<bool> SetDefaultModelAsync(int modelConfigId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/api/llm/setDefaultModel/{modelConfigId}", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
    }

    // 测试连接
    public async Task<(bool Success, string Message)> TestConnectionAsync(int modelConfigId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/api/llm/testConnection/{modelConfigId}", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TestConnectionResult>(cancellationToken: ct);
        return (result?.Success ?? false, result?.Message ?? "未知结果");
    }

    private record TestConnectionResult(bool Success, string Message);
}
