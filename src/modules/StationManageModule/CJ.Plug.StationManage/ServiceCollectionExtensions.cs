using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.StationManage.Services;
using CJ.Plug.StationManageApi.DbContext;
using CJ.Plug.StationManageApiClient;
using CJ.Plug.StationManage.Menus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase{}

    public static IServiceCollection AddStationManageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, StationManageMenu>();
        services.AddScoped<StationSelectionService>();

        services.AddHttpClient<IStationManageApiClient, StationManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddStationManageApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, StationManageDbConfig>();

        services.AddScoped<IStationConfigService, StationConfigService>();
        services.AddScoped<IStationManageService, StationManageService>();

        services.AddHttpClient<IStationManageApiClient, StationManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddStationManageApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapStationConfigApi();
            endpoints.MapStationManageApi();
        });
    }




}
