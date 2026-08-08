using CJ.Plug.SystemConfigApi.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.SystemConfigApi.Apis;

public static class SystemConfigApi
{
    public static IEndpointRouteBuilder MapSystemConfigApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/systemconfig").WithTags("系统配置");

        api.MapGet("/getAll", async (
            ISystemConfigService service,
            CancellationToken ct) =>
            await service.GetAllAsync(ct))
        .WithName("GetAllSystemConfig")
        .WithDescription("获取全部系统配置项");

        api.MapGet("/get/{key}", async (
            string key,
            ISystemConfigService service,
            CancellationToken ct) =>
        {
            var value = await service.GetValueAsync(key, ct);
            return value == null ? Results.NotFound() : Results.Ok(value);
        })
        .WithName("GetSystemConfigValue")
        .WithDescription("按键获取系统配置值");

        api.MapPut("/set", async (
            [FromBody] SetSystemConfigRequest request,
            ISystemConfigService service,
            CancellationToken ct) =>
            await service.SetValueAsync(request.Key, request.Value, request.Description, ct))
        .WithName("SetSystemConfigValue")
        .WithDescription("设置系统配置值（不存在则新增）");

        return app;
    }

    public record SetSystemConfigRequest(string Key, string Value, string? Description = null);
}
