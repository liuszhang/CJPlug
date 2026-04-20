
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;


using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class FlowchartDataManageApi
{
    public static IEndpointRouteBuilder MapFlowchartDataManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/pdz").WithTags("PDZ管理-Datas");

        api.MapGet("/getFlowcharts", async (IFlowchartDataService service) => await service.GetAllAsync());
        api.MapGet("/getFlowchartByDefinitionId/{DefinitionId}", async (IFlowchartDataService service,string DefinitionId) => await service.GetByPlugDefinitionIdAsync(DefinitionId));
        api.MapPost("/createFlowchart", async (IFlowchartDataService service, [FromBody] FlowchartData request) => await service.CreateAsync(request));
        api.MapPut("/updateFlowchart", async (IFlowchartDataService service, [FromBody] FlowchartData request) => await service.UpdateAsync(request));
        api.MapDelete("/deleteFlowchart/{PlugDefinitionId}", async (IFlowchartDataService service, string PlugDefinitionId) => await service.DeleteByPlugDefinitionIdAsync(PlugDefinitionId));


        return app;
    }

}

