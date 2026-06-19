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

public partial class MainApiClient : IGroupManageApiClient
{    
    public async Task<List<GroupManageDto>> GetAllUserGroupAsync()
    {
        var result = await GroupManageApiClient.Value.GetAllUserGroupAsync();
        await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other, "查询所有用户组");
        return result;
    }

    public async Task<GroupManageDto?> GetUserGroupByIdAsync(int id)
    {
        var result = await GroupManageApiClient.Value.GetUserGroupByIdAsync(id);
        await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other, $"查询用户组ID: {id}");
        return result;
    }

    public async Task<GroupManageDto?> CreateUserGroupAsync(CreateGroupRequest request)
    {
        try
        {
            var result = await GroupManageApiClient.Value.CreateUserGroupAsync(request);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Create,
                    $"创建用户组: {request.Name}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Create,
                    $"创建用户组失败: {request.Name}", "创建失败");
            }
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Create,
                $"创建用户组异常: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<GroupManageDto?> UpdateUserGroupAsync(UpdateGroupRequest request)
    {
        try
        {
            var result = await GroupManageApiClient.Value.UpdateUserGroupAsync(request);
            if (result != null)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Update,
                    $"更新用户组ID: {request.Id}", JsonSerializer.Serialize(request));
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Update,
                    $"更新用户组失败ID: {request.Id}", "更新失败");
            }
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Update,
                $"更新用户组异常ID: {request.Id}", ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteUserGroupAsync(int id)
    {
        try
        {
            var result = await GroupManageApiClient.Value.DeleteUserGroupAsync(id);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Delete,
                    $"删除用户组ID: {id}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Delete,
                    $"删除用户组失败ID: {id}", "删除失败");
            }
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Delete,
                $"删除用户组异常ID: {id}", ex.Message);
            throw;
        }
    }

    public async Task<List<GroupUserInfo>> GetGroupMembersAsync(int groupId)
    {
        try
        {
            var result = await GroupManageApiClient.Value.GetGroupMembersAsync(groupId);
            await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"查询用户组成员ID: {groupId}");
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"查询用户组成员异常ID: {groupId}", ex.Message);
            throw;
        }
    }

    public async Task<bool> AddGroupUserAsync(AddGroupUserRequest request)
    {
        try
        {
            var result = await GroupManageApiClient.Value.AddGroupUserAsync(request);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                    $"添加用户到用户组ID: {request.GroupId}, 用户ID: {request.UserId}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                    $"添加用户到用户组失败ID: {request.GroupId}, 用户ID: {request.UserId}", "添加失败");
            }
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"添加用户到用户组异常ID: {request.GroupId}, 用户ID: {request.UserId}", ex.Message);
            throw;
        }
    }


    public async Task<bool> RemoveGroupUserAsync(RemoveGroupUserRequest request)
    {
        try
        {
            var result = await GroupManageApiClient.Value.RemoveGroupUserAsync(request);
            if (result)
            {
                await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                    $"从用户组移除用户ID: {request.GroupId}, 用户ID: {request.UserId}");
            }
            else
            {
                await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                    $"从用户组移除用户失败ID: {request.GroupId}, 用户ID: {request.UserId}", "移除失败");
            }
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"从用户组移除用户异常ID: {request.GroupId}, 用户ID: {request.UserId}", ex.Message);
            throw;
        }
    }


    public async Task<List<UserGroupInfo>> GetUserGroupsAsync(int userId)
    {
        try
        {
            var result = await GroupManageApiClient.Value.GetUserGroupsAsync(userId);
            await AuditLog.LogSuccessAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"查询用户所属用户组ID: {userId}");
            return result;

        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserGroupManage, AuditOperationType.Other,
                $"查询用户所属用户组异常ID: {userId}", ex.Message);
            throw;
        }
    }
}
