using System.IO;
using CJ.Plug.AuditModels;
using CJ.Plug.Models.Station;
using CJ.Plug.ToolResourceApiClient;

public partial class MainApiClient : IToolResourceApiClient
{
    public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.CreateToolAsync(newTool, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建工具ID: {newTool.Id}");
        return result;
    }

    public async Task<bool> DeleteToolAsync(int? ToolId, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.DeleteToolAsync(ToolId, ct);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除工具ID: {ToolId}");
        return result;
    }

    public async Task<List<Tool>?> GetAllToolsAsync(CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.GetAllToolsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有工具");
        return result;
    }

    public async Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.GetToolByDisplayNameAsync(toolDisplayName, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具显示名: {toolDisplayName}");
        return result;
    }

    public async Task<Tool?> GetToolByIdAsync(int? id, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.GetToolByIdAsync(id, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具ID: {id}");
        return result;
    }

    public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.UpdateToolAsync(updatedTool, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新工具ID: {updatedTool.Id}");
        return result;
    }

    public async Task<Stream> DownloadToolAsync(string toolName, string version, CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.DownloadToolAsync(toolName, version, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Download, $"下载工具: {toolName} v{version}");
        return result;
    }

    public async Task<bool> MoveToolFilesFromTmpAsync(string toolName, bool isSystemTool, string userName)
    {
        var result = await ToolResourceApiClient.Value.MoveToolFilesFromTmpAsync(toolName, isSystemTool, userName);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"移动工具包文件: {toolName}");
        return result;
    }

    public async Task<bool> DeleteToolTmpFilesAsync()
    {
        var result = await ToolResourceApiClient.Value.DeleteToolTmpFilesAsync();
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, "删除工具包临时文件");
        return result;
    }

    public async Task<int> ImportDefaultToolsAsync(CancellationToken ct = default)
    {
        var result = await ToolResourceApiClient.Value.ImportDefaultToolsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"导入默认工具，处理 {result} 个");
        return result;
    }
}
