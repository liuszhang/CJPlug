using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.MCPToolApiClient;
using CJ.Plug.MCPToolsManage.Menus;
using CJ.Plug.MCPToolsManageApi.Apis;
using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.MCPToolsManageApi.DbContext;
using CJ.Plug.MCPToolsManageApi.Services;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }


    //前端服务包
    public static IServiceCollection AddMCPToolsModuleServices(this IServiceCollection services)
    {
        services.AddScoped<IModule, Module>();
        services.AddScoped<IMenuService, MCPToolsManageMenu>();

        services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API服务包
    public static IServiceCollection AddMCPToolsApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, MCPToolsManageDbConfig>();

        services.AddScoped<IMCPToolsManageService, MCPToolsManageService>();


        services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API路由注册
    public static IApplicationBuilder AddMCPToolsApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMCPToolsManageApi();
        });
    }




}

