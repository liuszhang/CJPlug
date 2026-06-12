using CJ.Plug.AuditModels;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.ProcessManageApiClient;
using System.Text.Json;

public partial class MainApiClient : IProcessManageApiClient
{
    public async Task<Process> CreateNewWorkflow(Process newWorkflow, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ProcessManageApiClient.Value.CreateNewWorkflow(newWorkflow, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.ProcessManage, AuditOperationType.Create, 
                $"创建流程: {newWorkflow.Name}", JsonSerializer.Serialize(new { newWorkflow.Name, newWorkflow.PlugTypeKey }));
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.ProcessManage, AuditOperationType.Create, 
                $"创建流程异常: {newWorkflow.Name}", ex.Message);
            throw;
        }
    }

    public async Task<Process[]> GetWorkflowsAsync(int maxItems = 20, string? userName = null, CancellationToken cancellationToken = default)
    {
        var result = await ProcessManageApiClient.Value.GetWorkflowsAsync(maxItems, userName, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.ProcessManage, AuditOperationType.Other, "查询所有流程");
        return result;
    }

    public async Task<bool> UpdateProcessAsync(int? workflowId, Process workflow, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ProcessManageApiClient.Value.UpdateProcessAsync(workflowId, workflow, cancellationToken);
            if (result)
                await AuditLog.LogSuccessAsync(AuditModule.ProcessManage, AuditOperationType.Update, 
                    $"更新流程ID: {workflowId}", JsonSerializer.Serialize(new { workflowId, workflow.Name }));
            else
                await AuditLog.LogFailureAsync(AuditModule.ProcessManage, AuditOperationType.Update, 
                    $"更新流程失败ID: {workflowId}", "更新失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.ProcessManage, AuditOperationType.Update, 
                $"更新流程异常ID: {workflowId}", ex.Message);
            throw;
        }
    }

    public async Task<JsonElement> AiGenerateWorkflowAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ProcessManageApiClient.Value.AiGenerateWorkflowAsync(prompt, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.ProcessManage, AuditOperationType.Other,
                $"AI生成工作流: {prompt[..Math.Min(prompt.Length, 50)]}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.ProcessManage, AuditOperationType.Other,
                "AI生成工作流失败", ex.Message);
            throw;
        }
    }

    public async Task<JsonElement> AiSaveWorkflowAsync(JsonElement result, CancellationToken cancellationToken = default)
    {
        try
        {
            var r = await ProcessManageApiClient.Value.AiSaveWorkflowAsync(result, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.ProcessManage, AuditOperationType.Create,
                "AI生成工作流保存成功");
            return r;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.ProcessManage, AuditOperationType.Create,
                "AI生成工作流保存失败", ex.Message);
            throw;
        }
    }
}
