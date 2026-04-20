using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugMarketApi.Apis;
using CJ.Plug.PlugMarketApi.Contracts;
using CJ.Plug.PlugMarketApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }


    public static IServiceCollection AddPlugMarketPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, PlugMarketMenu>();

        services.AddHttpClient<IPlugMarketApiClient, PlugMarketApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }


    public static IServiceCollection AddPlugMarketModuleApiServices(this IServiceCollection services)
    {
        //services.AddDbContext<PlugMarketDbContext>(options => options.UseSqlite(DbConnectionString.ConnectionString));
        services.AddSingleton<IModuleDbConfig,PlugMarketModuleDbConfig>();

        services.AddScoped<IPlugMarketService, PlugMarketService>();

        services.AddHttpClient<IPlugMarketApiClient, PlugMarketApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddPlugMarketModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPlugMarketApi();
        });
    }




}

