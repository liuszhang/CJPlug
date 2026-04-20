using CJ.Plug.DispatchServer.Apis;
using CJ.Plug.DispatchServer.Contracts;
using CJ.Plug.DispatchServer.Services;

public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDispatchApiService(this IServiceCollection services)
        {
        services.AddSignalR();
        services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());

        services.AddScoped<IStationService, StationService>();

        return services;
        }

    public static IApplicationBuilder UseDispatchServiceEndpoints(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<MainHub>("/mainHub");
            endpoints.MapDispatcherApi();
        });
    }

}

