using CJ.Plug.AuditModels;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.JobManageApiClient;
using System.Text.Json;

public partial class MainApiClient : IJobManageApiClient
{
    public async Task<BaseJob?> CreateJobAsync(BaseJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.CreateJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"创建作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<ProcessJob?> CreateJobAsync(ProcessJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.CreateJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建流程作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"创建流程作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<ToolJob?> CreateToolJobAsync(ToolJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.CreateToolJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"创建工具作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"创建工具作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteJobAsync(BaseJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.DeleteJobAsync(request, ct);
            if (result)
                await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除作业: {request.Name}");
            else
                await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Delete, $"删除作业失败: {request.Name}", "删除失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Delete, $"删除作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<BaseJob?>> GetAllJobsAsync(CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetAllJobsAsync(ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有作业");
        return result;
    }

    public async Task<BaseJob?> GetJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetJobByCorrelationIdAsync(CorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询作业关联ID: {CorrelationId}");
        return result;
    }

    public async Task<BaseJob?> GetJobByDefinitionIdAsync(string DefinitionId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetJobByDefinitionIdAsync(DefinitionId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询作业定义ID: {DefinitionId}");
        return result;
    }

    public async Task<List<BaseJob>?> GetJobsByFilter(JobFilter filter, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetJobsByFilter(filter, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询作业列表");
        return result;
    }

    public async Task<ProcessJob?> GetProcessJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetProcessJobByCorrelationIdAsync(CorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询流程作业关联ID: {CorrelationId}");
        return result;
    }

    public async Task<ToolJob?> GetToolJobByCorrelationIdAsync(string CorrelationId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetToolJobByCorrelationIdAsync(CorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具作业关联ID: {CorrelationId}");
        return result;
    }

    public async Task<ExecuteResultData?> GetToolJobResultByCorrelationIdAsync(string? CorrelationId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetToolJobResultByCorrelationIdAsync(CorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询工具作业结果: {CorrelationId}");
        return result;
    }

    public async Task<List<ToolJob>?> GetToolJobsByParentJobAsync(string ParentJobCorrelationId, CancellationToken ct = default)
    {
        var result = await JobManageApiClient.Value.GetToolJobsByParentJobAsync(ParentJobCorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"查询父作业的工具作业: {ParentJobCorrelationId}");
        return result;
    }

    public async Task<ExecuteResultData?> SubmitNewToolExecute(string stationIp, PlugExecutionRequest request)
    {
        try
        {
            var result = await JobManageApiClient.Value.SubmitNewToolExecute(stationIp, request);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Execute, $"提交工具执行: {stationIp}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Execute, $"提交工具执行异常: {stationIp}", ex.Message);
            throw;
        }
    }

    public async Task SyncJournalData(string JobCorrelationId, CancellationToken ct = default)
    {
        await JobManageApiClient.Value.SyncJournalData(JobCorrelationId, ct);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"同步日志数据: {JobCorrelationId}");
    }

    public async Task<BaseJob?> UpdateJobAsync(BaseJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.UpdateJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Update, $"更新作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<ProcessJob?> UpdateProcessJobAsync(ProcessJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.UpdateProcessJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新流程作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Update, $"更新流程作业异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<ToolJob?> UpdateToolJobAsync(ToolJob request, CancellationToken ct = default)
    {
        try
        {
            var result = await JobManageApiClient.Value.UpdateToolJobAsync(request, ct);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"更新工具作业: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Update, $"更新工具作业异常: {request.Name}", ex.Message);
            throw;
        }
    }
}
