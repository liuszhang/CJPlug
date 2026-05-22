using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis
{
    public static class RolePermissionApi
    {
        public static IEndpointRouteBuilder MapRolePermissionApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/role-permission").WithTags("角色权限管理");

            // 获取所有功能权限定义（从各模块收集）
            api.MapGet("/permission-definitions", GetAllPermissionDefinitions);

            // 获取角色完整配置
            api.MapGet("/config/{roleId:int}", GetRoleConfig);

            // 获取角色功能权限
            api.MapGet("/function-permissions/{roleId:int}", GetRoleFunctionPermissions);

            // 保存角色功能权限
            api.MapPost("/function-permissions", SaveRoleFunctionPermissions);

            // 获取角色数据权限
            api.MapGet("/data-permissions/{roleId:int}", GetRoleDataPermissions);

            // 保存角色数据权限
            api.MapPost("/data-permissions", SaveRoleDataPermissions);

            // 获取角色成员
            api.MapGet("/members/{roleId:int}", GetRoleMembers);

            // 保存角色成员
            api.MapPost("/members", SaveRoleMembers);

            return app;
        }

        private static async Task<IResult> GetAllPermissionDefinitions(IRolePermissionService service, CancellationToken cancellationToken)
        {
            var result = await service.GetAllPermissionDefinitionsAsync(cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> GetRoleConfig(int roleId, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var result = await service.GetRoleConfigAsync(roleId, cancellationToken);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }

        private static async Task<IResult> GetRoleFunctionPermissions(int roleId, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var result = await service.GetRoleFunctionPermissionsAsync(roleId, cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> SaveRoleFunctionPermissions(SaveRoleFunctionPermissionsRequest request, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var success = await service.SaveRoleFunctionPermissionsAsync(request, cancellationToken);
            return success ? Results.Ok() : Results.Problem("保存功能权限失败");
        }

        private static async Task<IResult> GetRoleDataPermissions(int roleId, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var result = await service.GetRoleDataPermissionsAsync(roleId, cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> SaveRoleDataPermissions(SaveRoleDataPermissionsRequest request, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var success = await service.SaveRoleDataPermissionsAsync(request, cancellationToken);
            return success ? Results.Ok() : Results.Problem("保存数据权限失败");
        }

        private static async Task<IResult> GetRoleMembers(int roleId, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var result = await service.GetRoleMemberIdsAsync(roleId, cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> SaveRoleMembers(SaveRoleMembersRequest request, IRolePermissionService service, CancellationToken cancellationToken)
        {
            var success = await service.SaveRoleMembersAsync(request, cancellationToken);
            return success ? Results.Ok() : Results.Problem("保存角色成员失败");
        }
    }
}
