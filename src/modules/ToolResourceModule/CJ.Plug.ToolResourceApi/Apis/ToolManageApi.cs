using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.PlugProcess;
using Microsoft.AspNetCore.Mvc;

public static class ToolManageApi
{
    public static IEndpointRouteBuilder MapToolManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/Tool").WithTags("工具管理");

        //路由定义
        api.MapGet("/GetAllTools", async (IToolManageService service) => await service.GetAllToolsAsync());
        api.MapGet("/GetToolById/{id}", async (IToolManageService service, int id) => await service.GetByIdAsync(id));
        api.MapGet("/GetToolByDisplayName/{ToolDisplayName}", async (IToolManageService service, string ToolDisplayName) => await service.GetByDisplayNameAsync(ToolDisplayName));
        api.MapPost("/CreateTool", async (IToolManageService service, [FromBody] Tool newTool) => await service.CreateToolAsync(newTool));
        api.MapDelete("/DeleteTool/{ToolId}", async (IToolManageService service, int ToolId) => await service.DeleteToolAsync(ToolId));
        api.MapPut("/UpdateTool", async (IToolManageService service, Tool updatedTool) => await service.UpdateToolAsync(updatedTool));
        api.MapPost("/MoveToolFilesFromTmp", async (IToolManageService service, [FromBody] ToolFileMoveRequest req) =>
            Results.Ok(await service.MoveToolFilesFromTmpAsync(req.ToolName, req.IsSystemTool, req.UserName)));
        api.MapPost("/DeleteToolTmpFiles", async (IToolManageService service) =>
            Results.Ok(await service.DeleteToolTmpFilesAsync()));
        api.MapPost("/ImportDefaultTools", async (IToolManageService service) =>
            Results.Ok(await service.ImportDefaultToolsAsync()));
        //api.MapGet("/GetToolPathOnIp/{ip}/{toolName}/{version}", async (IToolManageService service, string ip, string toolName, string? version) => await service.GetToolPathOnIp(ip, toolName, version));




        return app;
    }

}

public record ToolFileMoveRequest(string ToolName, bool IsSystemTool, string UserName);

