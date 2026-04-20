using CJ.Plug.ApiServer.Services;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToExternalPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        return services;
    }


    public class Module : ModuleBase
    {
    }


    public static IServiceCollection AddToExternalModuleApiServices(this IServiceCollection services)
    {
        services.AddScoped<IProcessToExternalService, ProcessToExternalService>();
        return services;
    }

    public static IApplicationBuilder AddToExternalModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapProcessToExternalApi();
        });
    }

}

