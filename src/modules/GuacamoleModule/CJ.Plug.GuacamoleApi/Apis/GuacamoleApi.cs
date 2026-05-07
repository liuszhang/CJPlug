using CJ.Plug.GuacamoleApi.Contracts;
using CJ.Plug.GuacamoleModels;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.GuacamoleApi.Apis
{
    /// <summary>
    /// Guacamole API 端点
    /// 提供远程桌面连接管理功能
    /// </summary>
    public static class GuacamoleApi
    {
        public static IEndpointRouteBuilder MapGuacamoleApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/guacamole").WithTags("Guacamole远程桌面");

            // 获取认证 Token
            api.MapGet("/token", async (IGuacamoleService service, CancellationToken ct) =>
            {
                var token = await service.GetAuthTokenAsync(ct);
                return token != null ? Results.Ok(token) : Results.StatusCode(503);
            })
            .WithName("GetGuacamoleToken")
            .WithDescription("获取 Guacamole 认证 Token");

            // 获取所有连接
            api.MapGet("/connections", async (IGuacamoleService service, CancellationToken ct) =>
            {
                var connections = await service.GetAllConnectionsAsync(ct);
                return Results.Ok(connections ?? new List<GuacamoleConnectionDto>());
            })
            .WithName("GetAllGuacamoleConnections")
            .WithDescription("获取所有 Guacamole 连接");

            // 根据 Station IP 获取连接
            api.MapGet("/connections/station/{stationIp}", async (IGuacamoleService service, string stationIp, CancellationToken ct) =>
            {
                var connection = await service.GetConnectionByStationIpAsync(stationIp, ct);
                return connection != null ? Results.Ok(connection) : Results.NotFound();
            })
            .WithName("GetGuacamoleConnectionByStationIp")
            .WithDescription("根据 Station IP 获取 Guacamole 连接");

            // 获取连接嵌入 Token (用于 iframe)
            api.MapGet("/embed/{stationIp}", async (IGuacamoleService service, string stationIp, CancellationToken ct) =>
            {
                var token = await service.GetConnectionTokenAsync(stationIp, ct);
                return token != null ? Results.Ok(token) : Results.NotFound(new { error = "未找到该 Station 的 Guacamole 连接配置" });
            })
            .WithName("GetGuacamoleEmbedToken")
            .WithDescription("获取 Guacamole 连接嵌入 Token");

            // 创建连接
            api.MapPost("/connections", async (IGuacamoleService service, [FromBody] GuacamoleConnectionDto connection, CancellationToken ct) =>
            {
                var result = await service.CreateConnectionAsync(connection, ct);
                return result != null ? Results.Ok(result) : Results.StatusCode(500);
            })
            .WithName("CreateGuacamoleConnection")
            .WithDescription("创建 Guacamole 连接");

            // 更新连接
            api.MapPut("/connections/{connectionId}", async (IGuacamoleService service, string connectionId, [FromBody] GuacamoleConnectionDto connection, CancellationToken ct) =>
            {
                var result = await service.UpdateConnectionAsync(connectionId, connection, ct);
                return result != null ? Results.Ok(result) : Results.StatusCode(500);
            })
            .WithName("UpdateGuacamoleConnection")
            .WithDescription("更新 Guacamole 连接");

            // 删除连接
            api.MapDelete("/connections/{connectionId}", async (IGuacamoleService service, string connectionId, CancellationToken ct) =>
            {
                var success = await service.DeleteConnectionAsync(connectionId, ct);
                return success ? Results.Ok() : Results.StatusCode(500);
            })
            .WithName("DeleteGuacamoleConnection")
            .WithDescription("删除 Guacamole 连接");

            return app;
        }
    }
}
