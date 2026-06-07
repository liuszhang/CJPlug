using CJ.Plug.DispatchServer.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace CJ.Plug.DispatchServer.Apis;

public static class DispatcherApi
{
    public static IEndpointRouteBuilder MapDispatcherApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/dispatch");

        //路由定义
        api.MapGet("/GetStationToExecute", async (IStationService service) => await service.GetStationToExecute());
        api.MapGet("/GetAllOnlineStation", async (IStationService service) => await service.GetAllOnlineStation());
        api.MapGet("/GetApiServer", async (IStationService service) => await service.GetApiServer());
        api.MapGet("/GetElsaEngineServer", async (IStationService service) => await service.GetElsaEngineServer());
        api.MapGet("/GetElsaEngineApiKey", async (IStationService service) => await service.GetElsaEngineApiKey());

        // MCP Tool 变更通知端点（供 ApiServer 调用，触发 SignalR 广播）
        api.MapPost("/notifyMcpToolUpdated", async (string toolId, string action, IHubContext<MainHub> hubContext) =>
        {
            await hubContext.Clients.All.SendAsync("MCPToolUpdated", toolId, action);
            return Results.Ok();
        });

        return app;
    }
}
