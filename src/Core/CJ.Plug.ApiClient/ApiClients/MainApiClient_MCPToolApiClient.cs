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
}
