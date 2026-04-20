
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;


using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class PlugVariableDataManageApi
{
    public static IEndpointRouteBuilder MapPlugVariableDataManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/pdz").WithTags("PDZ管理-Datas");

        api.MapGet("/getPlugVariableById/{Id}", async (IPlugVariableDataService service, int Id) => await service.GetByIdAsync(Id));
        api.MapGet("/getPlugVariables", async (IPlugVariableDataService service) => await service.GetAllAsync());
        api.MapGet("/getPlugVariableByDefinitionId/{DefinitionId}", async (IPlugVariableDataService service,string DefinitionId) => await service.GetByPlugDefinitionIdAsync(DefinitionId));
        api.MapPost("/createPlugVariable", async (IPlugVariableDataService service, [FromBody] PlugVariableData request) => await service.CreateAsync(request));
        api.MapPut("/updatePlugVariable", async (IPlugVariableDataService service, [FromBody] PlugVariableData request) => await service.UpdateAsync(request));
        api.MapDelete("/deletePlugVariable/{id}", async (IPlugVariableDataService service, int id) => await service.DeleteAsync(id));


        return app;
    }

}

