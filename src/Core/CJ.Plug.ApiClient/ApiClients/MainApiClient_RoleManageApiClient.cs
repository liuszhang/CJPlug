using CJ.Plug.AuditModels;
using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;
using System.Text.Json;

public partial class MainApiClient : IRoleManageApiClient
{
    async Task<List<RoleManageDto>> IRoleManageApiClient.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await RoleManageApiClient.Value.GetAllAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Other, "查询所有角色");
        return result;
    }

    async Task<RoleManageDto?> IRoleManageApiClient.GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await RoleManageApiClient.Value.GetByIdAsync(id, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Other, $"查询角色ID: {id}");
        return result;
    }

    async Task<RoleManageDto?> IRoleManageApiClient.CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RoleManageApiClient.Value.CreateAsync(request, cancellationToken);
            if (result != null)
                await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Create, $"创建角色: {request.Name}", JsonSerializer.Serialize(request));
            else
                await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Create, $"创建角色失败: {request.Name}", "创建失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Create, $"创建角色异常: {request.Name}", ex.Message);
            throw;
        }
    }

    async Task<RoleManageDto?> IRoleManageApiClient.UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RoleManageApiClient.Value.UpdateAsync(request, cancellationToken);
            if (result != null)
                await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Update, $"更新角色ID: {request.Id}", JsonSerializer.Serialize(request));
            else
                await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Update, $"更新角色失败ID: {request.Id}", "角色不存在");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Update, $"更新角色异常ID: {request.Id}", ex.Message);
            throw;
        }
    }

    async Task<bool> IRoleManageApiClient.DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RoleManageApiClient.Value.DeleteAsync(id, cancellationToken);
            if (result)
                await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"删除角色ID: {id}");
            else
                await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"删除角色失败ID: {id}", "角色不存在");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"删除角色异常ID: {id}", ex.Message);
            throw;
        }
    }
}
