using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis
{
    public static class UserManageApi
    {
        public static IEndpointRouteBuilder MapUserManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/user").WithTags("用户管理");

            api.MapGet("/getAllUsers", async (IUserManageService service) => await service.GetAllUsersAsync());

            api.MapPost("/createUser", async ([FromBody] CreateUserRequest request, IUserManageService service) =>
            {
                var result = await service.CreateUserAsync(request);
                return result != null ? Results.Ok(result) : Results.BadRequest("创建用户失败，用户名或邮箱可能已存在");
            });

            api.MapPut("/updateUser", async ([FromBody] UpdateUserRequest request, IUserManageService service) =>
            {
                try
                {
                    var result = await service.UpdateUserAsync(request);
                    return result != null ? Results.Ok(result) : Results.NotFound("用户不存在");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/assignRoles", async ([FromBody] AssignRolesRequest request, IUserManageService service) =>
            {
                try
                {
                    var result = await service.AssignRolesAsync(request);
                    return result ? Results.Ok() : Results.BadRequest("分配角色失败");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapGet("/getUserRoles/{userId:int}", async (int userId, IUserManageService service) =>
            {
                var roles = await service.GetUserRolesAsync(userId);
                return Results.Ok(roles);
            });

            api.MapPut("/{userId:int}/status", async (int userId, DataStatus status, IUserManageService service) =>
            {
                try
                {
                    var result = await service.SetUserStatusAsync(userId, status);
                    return result ? Results.Ok() : Results.BadRequest("设置用户状态失败");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPut("/{userId:int}/lockout", async (int userId, bool isLocked, IUserManageService service) =>
            {
                try
                {
                    var result = await service.SetUserLockoutAsync(userId, isLocked);
                    return result ? Results.Ok() : Results.BadRequest("设置用户锁定状态失败");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            return app;
        }
    }
}
