using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.PlugProcess;
using Microsoft.AspNetCore.Mvc;

public static class StationManageApi
{
    public static IEndpointRouteBuilder MapStationManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/Station").WithTags("图站管理");

        //路由定义
        api.MapGet("/GetAllStations", async (IStationManageService service) => await service.GetAllStationsAsync());
        api.MapGet("/GetStationById/{id}", async (IStationManageService service, int id) => await service.GetByIdAsync(id));
        api.MapGet("/GetStationByIp/{stationIp}", async (IStationManageService service, string stationIp) => await service.GetByStationIpAsync(stationIp));
        api.MapPost("/CreateStation", async (IStationManageService service, [FromBody] Station newStation) => await service.CreateStationAsync(newStation));
        api.MapDelete("/DeleteStation/{stationId}", async (IStationManageService service, int stationId) => await service.DeleteStationAsync(stationId));
        api.MapPut("/UpdateStation", async (IStationManageService service, [FromBody] Station updatedStation) => await service.UpdateStationAsync(updatedStation));
        //api.MapGet("/GetToolPathOnIp/{ip}/{toolName}/{version}", async (IStationManageService service, string ip, string toolName, string? version) => await service.GetToolPathOnIp(ip, toolName, version));




        return app;
    }

}

