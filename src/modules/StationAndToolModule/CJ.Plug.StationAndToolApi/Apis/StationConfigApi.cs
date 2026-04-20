using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.PlugProcess;
using Microsoft.AspNetCore.Mvc;

public static class StationConfigApi
{
    public static IEndpointRouteBuilder MapStationConfigApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/stationConfig").WithTags("图站配置");

        //路由定义
        api.MapGet("/GetAllConfigs", async (IStationConfigService service) => await service.GetAllStationConfigsAsync());
        api.MapGet("/GetConfigById/{id}", async (IStationConfigService service, int id) => await service.GetByIdAsync(id));
        api.MapGet("/GetConfigByStationIp/{stationIp}", async (IStationConfigService service, string stationIp) => await service.GetByStationIpAsync(stationIp));
        api.MapPost("/CreateConfig", async (IStationConfigService service, [FromBody] StationConfigTable newStationToolConfig) => await service.CreateStationToolConfigAsync(newStationToolConfig));
        api.MapDelete("/DeleteConfig/{stationToolConfigId}", async (IStationConfigService service, int stationToolConfigId) => await service.DeleteStationToolConfigAsync(stationToolConfigId));
        api.MapPut("/UpdateConfig", async (IStationConfigService service, [FromBody] StationConfigTable updatedStationToolConfig) => await service.UpdateStationToolConfigAsync(updatedStationToolConfig));
        api.MapGet("/GetToolPathOnIp/{ip}/{toolName}/{version}", async (IStationConfigService service, string ip, string toolName, string? version) => await service.GetToolPathOnIp(ip, toolName, version));
        api.MapPost("/GetToolPathByFilter", async (IStationConfigService service, ToolConfigFilter filter) => await service.GetToolPathByFilter(filter));




        return app;
    }

}

