using CJ.Plug.Models.Knowledge;
using System.Net.Http.Json;

namespace CJ.Plug.KnowledgeApiClient;

/// <summary>
/// 知识管理 API 客户端实现，继承 BaseApiClient
/// </summary>
public class KnowledgeApiClient : BaseApiClient, IKnowledgeApiClient
{
    public KnowledgeApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    // ===== 知识库 CRUD =====

    public async Task<List<KnowledgeBase>> GetAllBasesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/api/knowledge/bases", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<KnowledgeBase>>(cancellationToken: ct) ?? new();
    }

    public async Task<KnowledgeBase?> GetBaseByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/knowledge/bases/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeBase>(cancellationToken: ct);
    }

    public async Task<KnowledgeBase> CreateBaseAsync(KnowledgeBase kb, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/knowledge/bases", kb, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeBase>(cancellationToken: ct))!;
    }

    public async Task<KnowledgeBase?> UpdateBaseAsync(KnowledgeBase kb, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/knowledge/bases/{kb.Id}", kb, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeBase>(cancellationToken: ct);
    }

    public async Task DeleteBaseAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/knowledge/bases/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ===== 文件夹 CRUD =====

    public async Task<List<KnowledgeFolder>> GetFoldersByBaseAsync(int baseId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/knowledge/folders?baseId={baseId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<KnowledgeFolder>>(cancellationToken: ct) ?? new();
    }

    public async Task<KnowledgeFolder?> GetFolderByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/knowledge/folders/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeFolder>(cancellationToken: ct);
    }

    public async Task<KnowledgeFolder> CreateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/knowledge/folders", folder, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeFolder>(cancellationToken: ct))!;
    }

    public async Task<KnowledgeFolder?> UpdateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/knowledge/folders/{folder.Id}", folder, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeFolder>(cancellationToken: ct);
    }

    public async Task DeleteFolderAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/knowledge/folders/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ===== 知识条目 CRUD =====

    public async Task<List<KnowledgeItem>> GetItemsByFolderAsync(int? folderId, CancellationToken ct = default)
    {
        var url = folderId.HasValue
            ? $"/api/knowledge/items?folderId={folderId.Value}"
            : "/api/knowledge/items";
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<KnowledgeItem>>(cancellationToken: ct) ?? new();
    }

    public async Task<List<KnowledgeItem>> GetItemsByBaseAsync(int baseId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/knowledge/items/byBase/{baseId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<KnowledgeItem>>(cancellationToken: ct) ?? new();
    }

    public async Task<KnowledgeItem?> GetItemByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/knowledge/items/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeItem>(cancellationToken: ct);
    }

    public async Task<KnowledgeItem> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/knowledge/items", item, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeItem>(cancellationToken: ct))!;
    }

    public async Task<KnowledgeItem?> UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/knowledge/items/{item.Id}", item, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KnowledgeItem>(cancellationToken: ct);
    }

    public async Task DeleteItemAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/knowledge/items/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ===== 搜索 =====

    public async Task<List<KnowledgeItem>> SearchItemsAsync(string? keyword, string? tag, int? baseId = null, CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            queryParams.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        if (!string.IsNullOrWhiteSpace(tag))
            queryParams.Add($"tag={Uri.EscapeDataString(tag.Trim())}");
        if (baseId.HasValue)
            queryParams.Add($"baseId={baseId.Value}");

        var url = "/api/knowledge/items/search";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<KnowledgeItem>>(cancellationToken: ct) ?? new();
    }
}
