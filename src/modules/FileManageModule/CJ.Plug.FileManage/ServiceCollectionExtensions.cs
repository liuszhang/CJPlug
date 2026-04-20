using CJ.Plug.FileManageApi.Apis;
using CJ.Plug.FileManageApi.Contracts;
using CJ.Plug.FileManageApi.DbContext;
using CJ.Plug.FileManageApi.Services;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        services.AddHttpClient<IFileManageApiClient, FileManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddFileManageModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, FileModuleDbConfig>();

        services.AddScoped<IFileManageService, FileManageService>();

        services.AddHttpClient<IFileManageApiClient, FileManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    public static IApplicationBuilder AddFileManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapFileManageApi();
        });
    }


    public class Module : ModuleBase
    {
    }

}

