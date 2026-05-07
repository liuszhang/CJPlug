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

            return app;
        }
    }
}
