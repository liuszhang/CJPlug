using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.StationAndToolApi.DbContext;
using CJ.Plug.StationAndToolApi.Services;
using CJ.Plug.StationAndToolApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase{}

    public static IServiceCollection AddStationAndToolModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, StationAndToolMenu>();

        services.AddHttpClient<IStationAndToolApiClient, StationAndToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddStationAndToolApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig,StationAndToolDbConfig>();

        // 种子数据：默认工具列表
        services.AddSingleton<ISeedDataProvider, ToolSeedDataProvider>();


        services.AddScoped<IStationConfigService, StationConfigService>();
        services.AddScoped<IToolManageService, ToolManageService>();
        services.AddScoped<IStationManageService, StationManageService>();


        services.AddHttpClient<IStationAndToolApiClient, StationAndToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddStationAndToolApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapStationConfigApi();
            endpoints.MapToolManageApi();
            endpoints.MapStationManageApi();
        });
    }




}

