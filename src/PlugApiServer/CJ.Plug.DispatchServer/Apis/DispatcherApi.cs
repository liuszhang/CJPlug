using CJ.Plug.DispatchServer.Contracts;

namespace CJ.Plug.DispatchServer.Apis
{
    public static class DispatcherApi
    {
        public static IEndpointRouteBuilder MapDispatcherApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/dispatch");

            //路由定义
            api.MapGet("/GetStationToExecute", async (IStationService service) => await service.GetStationToExecute());
            api.MapGet("/GetAllOnlineStation", async (IStationService service) => await service.GetAllOnlineStation());
            api.MapGet("/GetApiServer", async (IStationService service) => await service.GetApiServer());
            api.MapGet("/GetElsaEngineServer", async (IStationService service) => await service.GetElsaEngineServer());
            api.MapGet("/GetElsaEngineApiKey", async (IStationService service) => await service.GetElsaEngineApiKey());
            

            return app;
        }
    }
}
