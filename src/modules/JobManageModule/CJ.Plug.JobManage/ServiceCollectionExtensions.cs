using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.JobManageApi.Apis;
using CJ.Plug.JobManageApi.Contracts;
using CJ.Plug.JobManageApi.DbContext;
using CJ.Plug.JobManageApi.Services;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;
using JobManageModule.Permissions;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }

    public static IServiceCollection AddJobManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, JobManageMenu>();

        services.AddHttpClient<IJobManageApiClient, JobManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddJobManageModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, JobModuleDbConfig>();

        services.AddScoped<IJobManageService, JobManageService>();

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, JobManagePermissionProvider>();

        services.AddHttpClient<IJobManageApiClient, JobManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    public static IApplicationBuilder AddJobManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapJobManageApi();
        });
    }




}

