
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;


using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class DataFlowDataManageApi
{
    public static IEndpointRouteBuilder MapDataFlowDataManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/pdz").WithTags("PDZ管理-Datas");

        api.MapGet("/getDataFlows", async (IDataFlowDataService service) => await service.GetAllAsync());
        api.MapGet("/getDataFlowByDefinitionId/{DefinitionId}", async (IDataFlowDataService service,string DefinitionId) => await service.GetByPlugDefinitionIdAsync(DefinitionId));
        api.MapPost("/createDataFlow", async (IDataFlowDataService service, [FromBody] DataFlowData request) => await service.CreateAsync(request));
        api.MapPut("/updateDataFlow", async (IDataFlowDataService service, [FromBody] DataFlowData request) => await service.UpdateAsync(request));
        api.MapDelete("/deleteDataFlow/{id}", async (IDataFlowDataService service, int id) => await service.DeleteAsync(id));


        return app;
    }

}

