using CJ.Plug.Models.MCPTools;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.MCPToolApiClient;

public class MCPToolApiClient : BaseApiClient, IMCPToolApiClient
{
    public MCPToolApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    public async Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mcp/addTool", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MCPTool>(cancellationToken: cancellationToken);
    }

    public async Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/mcp/deleteTool/{toolId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default)
    {
        var result = httpClient.GetFromJsonAsAsyncEnumerable<MCPTool>("/api/mcp/getTools", cancellationToken);
        return result.ToEnumerable();
    }

    public async Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/mcp/updateTool", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MCPTool>(cancellationToken: cancellationToken);
    }

    public async Task NotifyRefreshAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/api/mcp/notifyRefresh", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<PublishedWorkflowDto>>("/api/mcp/getPublishedWorkflows", cancellationToken);
        return result ?? new();
    }

    public async Task<(string content, string filePath)> GetTraePreviewAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>("/api/mcp/config/trae/preview", cancellationToken);
        var content = result.TryGetProperty("content", out var c) ? c.GetString() ?? "{}" : "{}";
        var filePath = result.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
        return (content, filePath);
    }

    public async Task<string> ConfigureTraeMcpAsync(string configContent, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mcp/config/trae", new { configContent }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "配置完成" : "配置完成";
    }

    public async Task<string> EnableTraeMcpAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/api/mcp/config/enable", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "已启用" : "已启用";
    }

    public async Task<(string content, string filePath)> GetClaudePreviewAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>("/api/mcp/config/claude/preview", cancellationToken);
        var content = result.TryGetProperty("content", out var c) ? c.GetString() ?? "{}" : "{}";
        var filePath = result.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
        return (content, filePath);
    }

    public async Task<string> ConfigureClaudeMcpAsync(string configContent, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mcp/config/claude", new { configContent }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "配置完成" : "配置完成";
    }

    public async Task<(string content, string filePath)> GetPreviewAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var encodedPath = Uri.EscapeDataString(filePath);
        var result = await httpClient.GetFromJsonAsync<JsonElement>($"/api/mcp/config/custom/preview?filePath={encodedPath}", cancellationToken);
        var content = result.TryGetProperty("content", out var c) ? c.GetString() ?? "{}" : "{}";
        var fp = result.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
        return (content, fp);
    }

    public async Task<string> ConfigureMcpAsync(string filePath, string configContent, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mcp/config/custom", new { filePath, configContent }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("message", out var msg) ? msg.GetString() ?? "配置完成" : "配置完成";
    }

    public async Task<string> PickFileAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>("/api/mcp/config/pick-file", cancellationToken);
        return result.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
    }

    public async Task<string> GetConfigPathAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>($"/api/mcp/config/path/{Uri.EscapeDataString(key)}", cancellationToken);
        return result.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
    }

    public async Task SaveConfigPathAsync(string key, string filePath, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/mcp/config/path/{Uri.EscapeDataString(key)}", new { filePath }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
