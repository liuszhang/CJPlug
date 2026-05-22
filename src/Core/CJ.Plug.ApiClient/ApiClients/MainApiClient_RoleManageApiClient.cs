using CJ.Plug.AuditModels;
using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;
using System.Text.Json;

public partial class MainApiClient : IRoleManageApiClient
{
    public async Task<List<RoleManageDto>> GetAllAsync()
    {
        var result = await RoleManageApiClient.Value.GetAllAsync();
        await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Other, "查询所有角色");
        return result;
    }

    public async Task<RoleManageDto?> GetByIdAsync(int id)
    {
        var result = await RoleManageApiClient.Value.GetByIdAsync(id);
        await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Other, $"查询角色ID: {id}");
        return result;
    }

    public async Task<RoleManageDto?> CreateAsync(CreateRoleRequest request)
    {
        try
        {
            var result = await RoleManageApiClient.Value.CreateAsync(request);
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

    public async Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request)
    {
        try
        {
            var result = await RoleManageApiClient.Value.UpdateAsync(request);
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

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var result = await RoleManageApiClient.Value.DeleteAsync(id);
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

    public async Task<List<RoleUserInfo>> GetRoleUsersAsync(int roleId)
    {
        var result = await RoleManageApiClient.Value.GetRoleUsersAsync(roleId);
        await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Other, $"查询角色ID {roleId} 的用户列表");
        return result;
    }

    public async Task<bool> AddRoleToUserAsync(AddRoleToUserRequest request)
    {
        try
        {
            var result = await RoleManageApiClient.Value.AddRoleToUserAsync(request);
            if (result)
                await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Update, $"添加用户到角色: 角色ID {request.RoleId}, 用户ID {request.UserId}");
            else
                await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Update, $"添加用户到角色失败: 角色ID {request.RoleId}, 用户ID {request.UserId}", "添加失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Update, $"添加用户到角色异常: 角色ID {request.RoleId}, 用户ID {request.UserId}", ex.Message);
            throw;
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(RemoveRoleUserRequest request)
    {
        try
        {
            var result = await RoleManageApiClient.Value.RemoveRoleFromUserAsync(request);
            if (result)
                await AuditLog.LogSuccessAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"从角色移除用户: 角色ID {request.RoleId}, 用户ID {request.UserId}");
            else
                await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"从角色移除用户失败: 角色ID {request.RoleId}, 用户ID {request.UserId}", "移除失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.RoleManage, AuditOperationType.Delete, $"从角色移除用户异常: 角色ID {request.RoleId}, 用户ID {request.UserId}", ex.Message);
            throw;
        }
    }

    // Explicit interface implementations for backward compat
    async Task<List<RoleManageDto>> IRoleManageApiClient.GetAllAsync(CancellationToken cancellationToken) => await GetAllAsync();
    async Task<RoleManageDto?> IRoleManageApiClient.GetByIdAsync(int id, CancellationToken cancellationToken) => await GetByIdAsync(id);
    async Task<RoleManageDto?> IRoleManageApiClient.CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken) => await CreateAsync(request);
    async Task<RoleManageDto?> IRoleManageApiClient.UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken) => await UpdateAsync(request);
    async Task<bool> IRoleManageApiClient.DeleteAsync(int id, CancellationToken cancellationToken) => await DeleteAsync(id);
    async Task<List<RoleUserInfo>> IRoleManageApiClient.GetRoleUsersAsync(int roleId, CancellationToken cancellationToken) => await GetRoleUsersAsync(roleId);
    async Task<bool> IRoleManageApiClient.AddRoleToUserAsync(AddRoleToUserRequest request, CancellationToken cancellationToken) => await AddRoleToUserAsync(request);
    async Task<bool> IRoleManageApiClient.RemoveRoleFromUserAsync(RemoveRoleUserRequest request, CancellationToken cancellationToken) => await RemoveRoleFromUserAsync(request);
}
