
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Mvc;

public static class RelationManageApi
{
    public static IEndpointRouteBuilder MapRelationManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/relation").WithTags("关系管理");

        api.MapGet("/getRealations", async (IRelationManageService service) => await service.GetAllRelationsAsync());
        api.MapGet("/getRealationsByCategory/{Category}", async (IRelationManageService service,string Category) => await service.GetRelationsByCategoryAsync(Category));
        api.MapPost("/createRelation", async (IRelationManageService service, [FromBody] CommonRelation request) => await service.CreateRealationAsync(request));
        api.MapPost("/deleteRelation", async (IRelationManageService service, [FromBody] CommonRelation request) => await service.DeleteRealationAsync(request));
        api.MapPost("/updateRelation", async (IRelationManageService service, [FromBody] CommonRelation request) => await service.UpdateRealationAsync(request));
        api.MapPost("/getByFilter", async (IRelationManageService service, [FromBody] RelationFilter filter) => await service.GetRealationByFilterAsync(filter));


        return app;
    }

}

