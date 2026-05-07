using CJ.Plug.AuditModels;
using CJ.Plug.Models.Job;
using System.Text.Json;

public partial class MainApiClient : IExecuteApiClient
{
    public async Task<ExecuteResultData?> ExecuteOnStation(PlugExecutionRequest stationExectionRequest)
    {
        try
        {
            var result = await ExecuteApiClient.Value.ExecuteOnStation(stationExectionRequest);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, "在工作站执行");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, "在工作站执行异常", ex.Message);
            throw;
        }
    }

    public async Task<ExecuteResultData?> ExecutePlug(PlugExecutionRequest plugExecutionRequest, CancellationToken ct = default)
    {
        try
        {
            var result = await ExecuteApiClient.Value.ExecutePlug(plugExecutionRequest, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, "执行插件");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, "执行插件异常", ex.Message);
            throw;
        }
    }

    public async Task<ExecuteResultData?> ExecutePlugByDefifnitionId(string definitionId, string? CorrelationId = null, CancellationToken ct = default)
    {
        try
        {
            var result = await ExecuteApiClient.Value.ExecutePlugByDefifnitionId(definitionId, CorrelationId, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, $"执行插件定义ID: {definitionId}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, $"执行插件异常定义ID: {definitionId}", ex.Message);
            throw;
        }
    }

    public async Task<ExecuteResultData?> ExecutePlugByType(PlugExecutionRequest PlugExecutionRequest, CancellationToken ct = default)
    {
        try
        {
            var result = await ExecuteApiClient.Value.ExecutePlugByType(PlugExecutionRequest, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, "按类型执行插件");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, "按类型执行插件异常", ex.Message);
            throw;
        }
    }

    public async Task ExecutePlugWithRequest(string DefinitionId, PlugExecutionRequest plugExecutionRequest, CancellationToken ct = default)
    {
        try
        {
            await ExecuteApiClient.Value.ExecutePlugWithRequest(DefinitionId, plugExecutionRequest, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, $"执行插件请求: {DefinitionId}");
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, $"执行插件请求异常: {DefinitionId}", ex.Message);
            throw;
        }
    }

    public async Task ExecuteResultReport(ExecuteResultData executeReport)
    {
        await ExecuteApiClient.Value.ExecuteResultReport(executeReport);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "执行结果报告");
    }

    public async Task<string?> ExecuteToolCommand(PlugExecutionRequest request)
    {
        try
        {
            var result = await ExecuteApiClient.Value.ExecuteToolCommand(request);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, "执行工具命令");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, "执行工具命令异常", ex.Message);
            throw;
        }
    }

    public async Task<(string?, string?)> SubmitExecute(PlugData PlugData, string UserName, PlugDataZone PlugDataZone, PDZTypeEnum PDZType)
    {
        try
        {
            var result = await ExecuteApiClient.Value.SubmitExecute(PlugData, UserName, PlugDataZone, PDZType);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, $"提交执行: {PlugData.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, $"提交执行异常: {PlugData.Name}", ex.Message);
            throw;
        }
    }
}
