using CJ.Plug.AuthApi.Contracts;
using CJ.Plug.AuthModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.AuthApi.Apis
{
    public static class AuthApi
    {
        public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/auth").WithTags("授权管理");

            api.MapGet("/getAll", async ([FromQuery] AuthRequestStatus? status, IAuthService service) =>
                await service.GetAllAsync(status));

            api.MapGet("/getById/{id:int}", async (int id, IAuthService service) =>
                await service.GetByIdAsync(id) is { } request ? Results.Ok(request) : Results.NotFound());

            api.MapPost("/create", async ([FromBody] CreateAuthRequestDto request, IAuthService service) =>
            {
                try
                {
                    var result = await service.CreateAsync(request);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/approve", async ([FromBody] ApproveAuthRequestDto request, IAuthService service) =>
            {
                try
                {
                    var result = await service.ApproveAsync(request);
                    return result != null ? Results.Ok(result) : Results.BadRequest("审批失败，请求不存在或已处理");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/cancel/{id:int}", async (int id, [FromBody] CancelRequest request, IAuthService service) =>
            {
                try
                {
                    var result = await service.CancelAsync(id, request.CancelledBy);
                    return result != null ? Results.Ok(result) : Results.BadRequest("撤回失败，请求不存在、已处理或无权限");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapGet("/hasPending", async ([FromQuery] AuthOperationType operationType, [FromQuery] string target, IAuthService service) =>
            {
                var exists = await service.HasPendingRequestAsync(operationType, target);
                return Results.Ok(exists);
            });

            // 解锁系统管理员账号
            api.MapPost("/unlockSystemAdmin", async ([FromBody] UnlockSystemAdminRequest request, IAuthService service) =>
            {
                try
                {
                    var result = await service.UnlockSystemAdminAsync(request);
                    return result ? Results.Ok() : Results.BadRequest("解锁失败，用户不存在、非系统管理员或未锁定");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            return app;
        }
        }
    

    public class CancelRequest
    {
        public string CancelledBy { get; set; } = string.Empty;
    }
}
