//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.AuditModels;
using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.Models.Relation;
using Microsoft.Extensions.DependencyInjection;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.TASApiClient;
using CJ.Plug.ProcessManageApiClient;
using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;

public partial class MainApiClient : IUserManageApiClient, IDepartmentManageApiClient
{
    // IUserManageApiClient
    public async Task<IEnumerable<User?>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var result = await UserManageApiClient.Value.GetAllUsersAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Other, "查询所有用户");
        return result;
    }

    public async Task<User?> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UserManageApiClient.Value.CreateUserAsync(request, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Create, 
                    $"创建用户: {request.UserName}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Create, 
                    $"创建用户失败: {request.UserName}", "用户名或邮箱已存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Create, 
                $"创建用户异常: {request.UserName}", ex.Message);
            throw;
        }
    }

    public async Task<User?> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UserManageApiClient.Value.UpdateUserAsync(request, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Update, 
                    $"更新用户ID: {request.Id}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update, 
                    $"更新用户失败ID: {request.Id}", "用户不存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update, 
                $"更新用户异常ID: {request.Id}", ex.Message);
            throw;
        }
    }

    public async Task<bool> AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UserManageApiClient.Value.AssignRolesAsync(request, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Update, 
                    $"分配角色给用户ID: {request.UserId}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update, 
                    $"分配角色失败用户ID: {request.UserId}", "分配失败");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update, 
                $"分配角色异常用户ID: {request.UserId}", ex.Message);
            throw;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var result = await UserManageApiClient.Value.GetUserRolesAsync(userId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Other, $"查询用户角色ID: {userId}");
        return result;
    }

    public async Task<bool> SetUserStatusAsync(int userId, DataStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UserManageApiClient.Value.SetUserStatusAsync(userId, status, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Update,
                    $"设置用户ID: {userId} 状态为 {status.GetDisplayName()}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update,
                    $"设置用户状态失败ID: {userId}", "操作失败");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update,
                $"设置用户状态异常ID: {userId}", ex.Message);
            throw;
        }
    }

    public async Task<bool> SetUserLockoutAsync(int userId, bool isLocked, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UserManageApiClient.Value.SetUserLockoutAsync(userId, isLocked, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Update,
                    $"设置用户ID: {userId} 锁定状态为 {(isLocked ? "锁定" : "解锁")}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update,
                    $"设置用户锁定状态失败ID: {userId}", "操作失败");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Update,
                $"设置用户锁定状态异常ID: {userId}", ex.Message);
            throw;
        }
    }

    // IDepartmentManageApiClient
    public async Task<List<DepartmentManageDto>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var result = await DepartmentManageApiClient.Value.GetAllDepartmentsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Other, "查询所有部门");
        return result;
    }

    public async Task<DepartmentManageDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await DepartmentManageApiClient.Value.GetDepartmentByIdAsync(id, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Other, $"查询部门ID: {id}");
        return result;
    }

    public async Task<DepartmentManageDto?> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await DepartmentManageApiClient.Value.CreateDepartmentAsync(request, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Create, 
                    $"创建部门: {request.Name}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Create, 
                    $"创建部门失败: {request.Name}", "创建失败");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Create, 
                $"创建部门异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<DepartmentManageDto?> UpdateDepartmentAsync(UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await DepartmentManageApiClient.Value.UpdateDepartmentAsync(request, cancellationToken);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Update, 
                    $"更新部门ID: {request.Id}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update, 
                    $"更新部门失败ID: {request.Id}", "部门不存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update, 
                $"更新部门异常ID: {request.Id}", ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await DepartmentManageApiClient.Value.DeleteDepartmentAsync(id, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Delete,
                    $"删除部门ID: {id}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Delete,
                    $"删除部门失败ID: {id}", "部门不存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Delete,
                $"删除部门异常ID: {id}", ex.Message);
            throw;
        }
    }

    // 部门人员管理
    public async Task<List<DepartmentUserInfo>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var result = await DepartmentManageApiClient.Value.GetDepartmentUsersAsync(departmentId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Other, $"查询部门人员ID: {departmentId}");
        return result;
    }

    public async Task<bool> AddUserToDepartmentAsync(int departmentId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await DepartmentManageApiClient.Value.AddUserToDepartmentAsync(departmentId, userId, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                    $"将用户ID: {userId} 添加到部门ID: {departmentId}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                    $"添加用户到部门失败", "用户不存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                $"添加用户到部门异常", ex.Message);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromDepartmentAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await DepartmentManageApiClient.Value.RemoveUserFromDepartmentAsync(userId, cancellationToken);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                    $"将用户ID: {userId} 从部门移除");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                    $"从部门移除用户失败", "用户不存在");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.DepartmentManage, AuditOperationType.Update,
                $"从部门移除用户异常", ex.Message);
            throw;
        }
    }    
}
