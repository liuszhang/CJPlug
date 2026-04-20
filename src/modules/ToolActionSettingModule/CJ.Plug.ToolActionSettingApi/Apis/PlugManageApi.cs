
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class PlugManageApi
{
    public static IEndpointRouteBuilder MapPlugManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/plug").WithTags("插头管理");

        api.MapGet("/getPlugs", async (IPlugManageService service) => await service.GetAllPlugsAsync());
        //api.MapGet("/getPlugsInProcess/{ProcessDefinitionId}", async (IPlugManageService service, string ProcessDefinitionId) => await service.GetChildPlugsAsync(ProcessDefinitionId));
        api.MapGet("/getChildPlugs/{DefinitionId}", async (IPlugManageService service, string DefinitionId) => await service.GetChildPlugsAsync(DefinitionId));
        api.MapGet("/getByDefinitionId/{definitionId}", async (IPlugManageService service, string definitionId) => await service.GetPlugByDefinitionId(definitionId));
        api.MapGet("/getById/{Id}", async (IPlugManageService service, int Id) => await service.GetPlugById(Id));
        api.MapGet("/GetParentPlugById/{Id}", async (IPlugManageService service, int Id) => await service.GetParentPlugById(Id));
        api.MapGet("/getByType/{typeName}", async (IPlugManageService service, string typeName) => await service.GetPlugByTypeName(typeName));

        api.MapGet("/getVariablesByDefinitionId/{DefinitionId}", async (IPlugManageService service, string DefinitionId) => await service.GetVariablesByDefinitionId(DefinitionId));
        api.MapPut("/updatePlug/{id}", async (IPlugManageService service, int id, [FromBody] Plug request) => await service.UpdatePlugAsync(id, request)); // 编辑 API
        api.MapPost("/createPlug", async (IPlugManageService service, [FromBody] Plug request) => await service.CreatePlugAsync(request));
        api.MapDelete("/deletePlug/{id}", async (IPlugManageService service, int id) => await service.DeletePlugAsync(id));


        api.MapGet("/getPlugActions", async (IPlugManageService service) => await service.GetAllPlugActionsAsync());
        api.MapPost("/createPlugAction", async (IPlugManageService service, [FromBody] PlugAction request) => await service.CreatePlugActionAsync(request));
        api.MapPut("/updatePlugAction", async (IPlugManageService service, [FromBody] PlugAction request) => await service.UpdatePlugActionAsync(request)); // 编辑 API
        api.MapGet("/getPlugActionById/{Id}", async (IPlugManageService service, int Id) => await service.GetPlugActionByIdAsync(Id));
        api.MapGet("/getPlugActionsByPlugId/{Id}", async (IPlugManageService service, int Id) => await service.GetPlugActionsByPlugIdAsync(Id));
        api.MapGet("/getPlugActionsByPlugDefinitionId/{DefinitionId}", async (IPlugManageService service, string DefinitionId) => await service.GetPlugActionsByPlugDefinitionIdAsync(DefinitionId));
        api.MapDelete("/deletePlugAction/{id}", async (IPlugManageService service, int id) => await service.DeletePlugActionAsync(id));


        api.MapGet("/getRealations", async (IPlugManageService service) => await service.GetAllRealationssAsync());
        api.MapPost("/createRealation", async (IPlugManageService service, [FromBody] PlugToPlugAction request) => await service.CreateRealationAsync(request));
        api.MapPost("/deleteRealation", async (IPlugManageService service, [FromBody] PlugToPlugAction request) => await service.DeleteRealationsAsync(request));
        //api.MapDelete("/deleteRealations/{plugId?}/{plugActionDefinitionId?}", async (IPlugManageService service, int? plugId, string? plugActionDefinitionId) => await service.DeleteRealationsAsync(plugId, plugActionDefinitionId));

        



        return app;
    }

}

