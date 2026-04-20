
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;


using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class PlugDataManageApi
{
    public static IEndpointRouteBuilder MapPlugDataManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/pdz").WithTags("PDZ管理-Datas");

        api.MapGet("/getPlugDatas", async (IPlugDataService service) => await service.GetAllAsync());
        api.MapGet("/getPlugDataByDefinitionId/{DefinitionId}", async (IPlugDataService service,string DefinitionId) => await service.GetByPlugDefinitionIdAsync(DefinitionId));
        //api.MapPost("/getPdzByFilter", async (IPDZManageService service, [FromBody] PDZFilter filter) => await service.GetByFilter(filter));
        api.MapPost("/createPlugData", async (IPlugDataService service, [FromBody] PlugData request) => await service.CreateAsync(request));
        api.MapPut("/updatePlugData", async (IPlugDataService service, [FromBody] PlugData request) => await service.UpdateAsync(request));
        api.MapDelete("/deletePlugData/{PlugDefinitionId}", async (IPlugDataService service, string PlugDefinitionId) => await service.DeleteByDefinitionId(PlugDefinitionId));
        //api.MapPost("/deleteByFilter", async (IPDZManageService service, [FromBody] PDZFilter filter) => await service.DeleteByFilter(filter));


        return app;
    }

}

