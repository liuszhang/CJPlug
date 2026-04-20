

using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class PDZManageApi
{
    public static IEndpointRouteBuilder MapPDZManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/pdz").WithTags("PDZ管理");

        api.MapGet("/getPDZs", async (IPDZManageService service) => await service.GetAllPdz());
        api.MapGet("/getByPDZId/{PDZId}", async (IPDZManageService service,string PDZId) => await service.GetByPDZId(PDZId));
        api.MapGet("/getById/{Id}", async (IPDZManageService service,int Id) => await service.GetPdzById(Id));
        api.MapPost("/getPdzByFilter", async (IPDZManageService service, [FromBody] PDZFilter filter) => await service.GetByFilter(filter));
        api.MapPost("/createPDZ", async (IPDZManageService service, [FromBody] PlugDataZone pdz) => await service.CreatePDZ(pdz));
        api.MapPut("/updatePDZ", async (IPDZManageService service, [FromBody] PlugDataZone pdz) => await service.UpdatePDZ(pdz));
        api.MapDelete("/deletePDZ/{PDZId}", async (IPDZManageService service, string PDZId) => await service.DeletePDZ(PDZId));
        api.MapPost("/deleteByFilter", async (IPDZManageService service, [FromBody] PDZFilter filter) => await service.DeleteByFilter(filter));


        return app;
    }

}

