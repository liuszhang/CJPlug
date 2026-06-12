using CJ.Plug.Models.MCPTools;
using System.Net.Http.Json;

namespace CJ.Plug.MCPToolApiClient;

public class MCPToolApiClient : BaseApiClient, IMCPToolApiClient
{
    public MCPToolApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    public async Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mcptools/addTool", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MCPTool>(cancellationToken: cancellationToken);
    }

    public async Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/mcptools/deleteTool/{toolId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default)
    {
        var result = httpClient.GetFromJsonAsAsyncEnumerable<MCPTool>("/api/mcptools/getTools", cancellationToken);
        return result.ToEnumerable();
    }

    public async Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/mcptools/updateTool", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MCPTool>(cancellationToken: cancellationToken);
    }

    public async Task NotifyRefreshAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/api/mcptools/notifyRefresh", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<PublishedWorkflowDto>>("/api/mcptools/getPublishedWorkflows", cancellationToken);
        return result ?? new();
    }
}
