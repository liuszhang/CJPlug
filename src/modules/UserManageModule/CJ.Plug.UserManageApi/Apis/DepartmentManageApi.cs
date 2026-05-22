using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis
{
    public static class DepartmentManageApi
    {
        public static IEndpointRouteBuilder MapDepartmentManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/department").WithTags("部门管理");

            api.MapGet("/getAll", async (IDepartmentManageService service) =>
                await service.GetAllAsync());

            api.MapGet("/getById/{id:int}", async (int id, IDepartmentManageService service) =>
                await service.GetByIdAsync(id) is { } dept ? Results.Ok(dept) : Results.NotFound());

            api.MapPost("/create", async ([FromBody] CreateDepartmentRequest request, IDepartmentManageService service) =>
            {
                try
                {
                    var dept = await service.CreateAsync(request);
                    return Results.Ok(dept);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPut("/update", async ([FromBody] UpdateDepartmentRequest request, IDepartmentManageService service) =>
            {
                try
                {
                    var dept = await service.UpdateAsync(request);
                    return dept is not null ? Results.Ok(dept) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapDelete("/delete/{id:int}", async (int id, IDepartmentManageService service) =>
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

            // 部门人员管理
            api.MapGet("/getUsers/{departmentId:int}", async (int departmentId, IDepartmentManageService service) =>
                await service.GetDepartmentUsersAsync(departmentId));

            api.MapPost("/addUser", async ([FromBody] AddDepartmentUserRequest request, IDepartmentManageService service) =>
            {
                try
                {
                    var result = await service.AddUserToDepartmentAsync(request.DepartmentId, request.UserId);
                    return result ? Results.Ok() : Results.BadRequest("用户不存在");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/removeUser", async ([FromBody] RemoveDepartmentUserRequest request, IDepartmentManageService service) =>
            {
                try
                {
                    var result = await service.RemoveUserFromDepartmentAsync(request.UserId);
                    return result ? Results.Ok() : Results.BadRequest("用户不存在");
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
