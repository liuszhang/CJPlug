using CJ.Plug.PlugMarketApi.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.PlugMarketApi.Apis
{
    public static class PlugMarketApi
    {
        public static IEndpointRouteBuilder MapPlugMarketApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/plug").WithTags("插头市场");

            api.MapPost("/createMarketPlug", async (IPlugMarketService service, [FromBody] MarketPlug request) => await service.CreateMarketPlugAsync(request));
            api.MapGet("/getMarketPlugs", async (IPlugMarketService service) => await service.GetMarketPlugsAsync());
            api.MapDelete("/deleteMarketPlug/{id}", async (IPlugMarketService service, int id) => await service.DeleteMarketPlugAsync(id));
            api.MapGet("/getSourcePlug/{marketPlugId}", async (IPlugMarketService service, int marketPlugId) =>
            {
                var plug = await service.GetSourcePlugByMarketPlugIdAsync(marketPlugId);
                return plug is not null ? Results.Ok(plug) : Results.NotFound();
            });



            return app;
        }

    }
}
