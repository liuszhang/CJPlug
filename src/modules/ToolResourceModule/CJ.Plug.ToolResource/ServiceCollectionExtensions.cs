using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.ToolResource.Menus;
using CJ.Plug.ToolResourceApi.DbContext;
using CJ.Plug.ToolResourceApi.Services;
using CJ.Plug.ToolResourceApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase{}

    /// <summary>
    /// 注册前端页面服务：菜单 + ApiClient（UI 端调用后端 API 使用）
    /// </summary>
    public static IServiceCollection AddToolResourceModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, ToolResourceMenu>();

        services.AddHttpClient<IToolResourceApiClient, ToolResourceApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    /// <summary>
    /// 注册后端服务：DB 配置 + Service + 种子数据（服务端执行）
    /// </summary>
    public static IServiceCollection AddToolResourceApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, ToolResourceDbConfig>();

        // 种子数据：默认工具列表
        services.AddSingleton<ISeedDataProvider, ToolSeedDataProvider>();

        services.AddScoped<IToolManageService, ToolManageService>();

        services.AddHttpClient<IToolResourceApiClient, ToolResourceApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    /// <summary>
    /// 注册端点路由
    /// </summary>
    public static IApplicationBuilder AddToolResourceApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapToolManageApi();
        });
    }




}
