using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis;

public static class GroupManageApi
{
    public static IEndpointRouteBuilder MapGroupManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/group").WithTags("用户组管理");

        api.MapGet("/getAllGroups", async (IGroupManageService service) => await service.GetAllAsync());

        api.MapGet("/getGroupById/{id:int}", async (int id, IGroupManageService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        api.MapPost("/createGroup", async ([FromBody] CreateGroupRequest request, IGroupManageService service) =>
        {
            try
            {
                var result = await service.CreateAsync(request);
                return result != null ? Results.Ok(result) : Results.BadRequest("创建用户组失败");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapPut("/updateGroup", async ([FromBody] UpdateGroupRequest request, IGroupManageService service) =>
        {
            try
            {
                var result = await service.UpdateAsync(request);
                return result != null ? Results.Ok(result) : Results.NotFound("用户组不存在");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapDelete("/deleteGroup/{id:int}", async (int id, IGroupManageService service) =>
        {
            try
            {
                var success = await service.DeleteAsync(id);
                return success ? Results.Ok() : Results.NotFound("用户组不存在");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapGet("/getGroupMembers/{groupId:int}", async (int groupId, IGroupManageService service) =>
        {
            try
            {
                var members = await service.GetGroupMembersAsync(groupId);
                return Results.Ok(members);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapPost("/addGroupUser", async ([FromBody] AddGroupUserRequest request, IGroupManageService service) =>
        {
            try
            {
                var success = await service.AddGroupUserAsync(request);
                return success ? Results.Ok() : Results.BadRequest("用户已在用户组中或操作失败");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapPost("/removeGroupUser", async ([FromBody] RemoveGroupUserRequest request, IGroupManageService service) =>
        {
            try
            {
                var success = await service.RemoveGroupUserAsync(request);
                return success ? Results.Ok() : Results.BadRequest("移除用户失败");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        api.MapGet("/getUserGroups/{userId:int}", async (int userId, IGroupManageService service) =>
        {
            try
            {
                var groups = await service.GetUserGroupsAsync(userId);
                return Results.Ok(groups);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        return app;
    }
}
