using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis
{
    public static class RoleManageApi
    {
        public static IEndpointRouteBuilder MapRoleManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/role").WithTags("角色管理");

            api.MapGet("/getAll", async (IRoleManageService service) =>
                await service.GetAllAsync());

            api.MapGet("/getById/{id:int}", async (int id, IRoleManageService service) =>
                await service.GetByIdAsync(id) is { } role ? Results.Ok(role) : Results.NotFound());

            api.MapPost("/create", async (CreateRoleRequest request, IRoleManageService service) =>
            {
                try
                {
                    var role = await service.CreateAsync(request);
                    return Results.Ok(role);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPut("/update", async (UpdateRoleRequest request, IRoleManageService service) =>
            {
                try
                {
                    var role = await service.UpdateAsync(request);
                    return role is not null ? Results.Ok(role) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapDelete("/delete/{id:int}", async (int id, IRoleManageService service) =>
            {
                try
                {
                    var result = await service.DeleteAsync(id);
                    return result ? Results.Ok() : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            return app;
        }
    }
}
