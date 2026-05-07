using CJ.Plug.AuditApi.Contracts;
using CJ.Plug.AuditModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.AuditApi.Apis
{
    public static class AuditApi
    {
        public static IEndpointRouteBuilder MapAuditApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/audit").WithTags("审计管理");

            api.MapPost("/log", async ([FromBody] CreateAuditLogRequest request, IAuditService service) =>
            {
                try
                {
                    var result = await service.LogAsync(request);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/query", async ([FromBody] AuditLogQueryRequest query, IAuditService service) =>
            {
                try
                {
                    var result = await service.QueryAsync(query);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapGet("/getById/{id:long}", async (long id, IAuditService service) =>
            {
                var result = await service.GetByIdAsync(id);
                return result != null ? Results.Ok(result) : Results.NotFound();
            });

            api.MapDelete("/cleanup/{days:int}", async (int days, IAuditService service) =>
            {
                try
                {
                    var deletedCount = await service.CleanupAsync(days);
                    return Results.Ok(new { DeletedCount = deletedCount });
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
