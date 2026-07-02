using CJ.Plug.AuditModels;
using CJ.Plug.MCPToolApiClient;
using CJ.Plug.Models.MCPTools;

public partial class MainApiClient : IMCPToolApiClient
{
    public async Task<MCPTool?> CreateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.CreateMCPToolAsync(request, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建MCP工具: {request.Name}");
        return result;
    }

    public async Task DeleteMCPToolAsync(int toolId, CancellationToken cancellationToken = default)
    {
        await MCPToolApiClient.Value.DeleteMCPToolAsync(toolId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除MCP工具ID: {toolId}");
    }

    public async Task<IEnumerable<MCPTool?>> GetAllMCPToolsAsync(CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.GetAllMCPToolsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有MCP工具");
        return result;
    }

    public async Task<MCPTool?> UpdateMCPToolAsync(MCPTool request, CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.UpdateMCPToolAsync(request, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新MCP工具: {request.Name}");
        return result;
    }

    public async Task NotifyRefreshAsync(CancellationToken cancellationToken = default)
    {
        await MCPToolApiClient.Value.NotifyRefreshAsync(cancellationToken);
    }

    public async Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ((MCPToolApiClient)MCPToolApiClient.Value).GetPublishedWorkflowsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "获取已发布工作流");
        return result;
    }

    public async Task<(string content, string filePath)> GetTraePreviewAsync(CancellationToken cancellationToken = default)
    {
        return await MCPToolApiClient.Value.GetTraePreviewAsync(cancellationToken);
    }

    public async Task<string> ConfigureTraeMcpAsync(string configContent, CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.ConfigureTraeMcpAsync(configContent, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "配置Trae MCP: 覆盖写入配置");
        return result;
    }

    public async Task<string> EnableTraeMcpAsync(CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.EnableTraeMcpAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "启用Trae MCP: 写入cj-mcpserver");
        return result;
    }

    public async Task<(string content, string filePath)> GetClaudePreviewAsync(CancellationToken cancellationToken = default)
    {
        return await MCPToolApiClient.Value.GetClaudePreviewAsync(cancellationToken);
    }

    public async Task<string> ConfigureClaudeMcpAsync(string configContent, CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.ConfigureClaudeMcpAsync(configContent, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "配置Claude Code MCP: 覆盖写入配置");
        return result;
    }

    public async Task<(string content, string filePath)> GetPreviewAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await MCPToolApiClient.Value.GetPreviewAsync(filePath, cancellationToken);
    }

    public async Task<string> ConfigureMcpAsync(string filePath, string configContent, CancellationToken cancellationToken = default)
    {
        var result = await MCPToolApiClient.Value.ConfigureMcpAsync(filePath, configContent, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"配置自定义 MCP: {filePath}");
        return result;
    }

    public async Task<string> PickFileAsync(CancellationToken cancellationToken = default)
    {
        return await MCPToolApiClient.Value.PickFileAsync(cancellationToken);
    }

    public async Task<string> GetConfigPathAsync(string key, CancellationToken cancellationToken = default)
    {
        return await MCPToolApiClient.Value.GetConfigPathAsync(key, cancellationToken);
    }

    public async Task SaveConfigPathAsync(string key, string filePath, CancellationToken cancellationToken = default)
    {
        await MCPToolApiClient.Value.SaveConfigPathAsync(key, filePath, cancellationToken);
    }
}
