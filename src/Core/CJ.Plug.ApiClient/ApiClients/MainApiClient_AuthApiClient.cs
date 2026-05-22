using CJ.Plug.AuditModels;
using CJ.Plug.AuthApiClient;
using CJ.Plug.AuthModels;
using System.Text.Json;

public partial class MainApiClient : IAuthApiClient
{
    public async Task<List<AuthRequestDto>> GetAllAuthRequestAsync(AuthRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var result = await AuthApiClient.Value.GetAllAuthRequestAsync(status, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.AuthManage, AuditOperationType.Other, "查询授权请求列表");
        return result;
    }

    async Task<AuthRequestDto?> IAuthApiClient.GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await AuthApiClient.Value.GetByIdAsync(id, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.AuthManage, AuditOperationType.Other, $"查询授权请求ID: {id}");
        return result;
    }

    public async Task<AuthRequestDto> CreateAsync(CreateAuthRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await AuthApiClient.Value.CreateAsync(request, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.AuthManage, AuditOperationType.Create, 
                $"创建授权请求: {request.OperationType} - {request.TargetDescription}", JsonSerializer.Serialize(request));
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.AuthManage, AuditOperationType.Create, 
                $"创建授权请求失败: {request.OperationType} - {request.TargetDescription}", ex.Message);
            throw;
        }
    }

    public async Task<AuthRequestDto?> ApproveAsync(ApproveAuthRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await AuthApiClient.Value.ApproveAsync(request, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.AuthManage, request.IsApproved ? AuditOperationType.Approve : AuditOperationType.Reject, 
                    $"{(request.IsApproved ? "批准" : "拒绝")}授权请求ID: {request.RequestId}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.AuthManage, request.IsApproved ? AuditOperationType.Approve : AuditOperationType.Reject, 
                    $"{(request.IsApproved ? "批准" : "拒绝")}授权请求失败ID: {request.RequestId}", "请求不存在或已处理");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.AuthManage, request.IsApproved ? AuditOperationType.Approve : AuditOperationType.Reject, 
                $"{(request.IsApproved ? "批准" : "拒绝")}授权请求异常ID: {request.RequestId}", ex.Message);
            throw;
        }
    }

    public async Task<AuthRequestDto?> CancelAsync(int id, string cancelledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await AuthApiClient.Value.CancelAsync(id, cancelledBy, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.AuthManage, AuditOperationType.Cancel, 
                    $"撤回授权请求ID: {id}，操作人: {cancelledBy}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.AuthManage, AuditOperationType.Cancel, 
                    $"撤回授权请求失败ID: {id}", "请求不存在、已处理或无权限");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.AuthManage, AuditOperationType.Cancel, 
                $"撤回授权请求异常ID: {id}", ex.Message);
            throw;
        }
    }

    public async Task<bool> HasPendingRequestAsync(AuthOperationType operationType, string target, CancellationToken cancellationToken = default)
    {
        var result = await AuthApiClient.Value.HasPendingRequestAsync(operationType, target, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.AuthManage, AuditOperationType.Other, 
            $"查询待审批请求: {operationType} - {target}");
        return result;
    }
}
