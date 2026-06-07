using CJ.Plug.ApiServer.Apis;
using CJ.Plug.ApiServer.Services;

namespace CJ.Plug.ApiServer;

public static class PackageServiceCollectionExtensions
{
    public static IServiceCollection AddPackageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<PackageProgressTracker>();
        services.AddScoped<PackageService>();
        return services;
    }

    public static IApplicationBuilder AddPackageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPackageApi();
        });
    }
}
