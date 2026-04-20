using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.RelationApi.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{

    public class Module : ModuleBase
    {
    }


    public static IServiceCollection AddRelationManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        services.AddHttpClient<IRelationApiClient,RelationApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddRelationManageModuleApiServices(this IServiceCollection services)
    {

        services.AddSingleton<IModuleDbConfig, RelationModuleDbConfig>();


        services.AddHttpClient<IRelationApiClient, RelationApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddScoped<IRelationManageService, RelationManageService>();
        return services;
    }

    public static IApplicationBuilder AddRelationManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapRelationManageApi();
        });
    }

}

