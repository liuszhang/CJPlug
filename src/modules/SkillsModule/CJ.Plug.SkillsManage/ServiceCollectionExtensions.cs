using CJ.Plug.MCPToolApiClient;
using CJ.Plug.MCPToolsManage.Menus;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }


    //前端服务包
    public static IServiceCollection AddSkillsModuleServices(this IServiceCollection services)
    {
        services.AddScoped<IModule, Module>();
        services.AddScoped<IMenuService, SkillsManageMenu>();

        services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API服务包
    public static IServiceCollection AddSkillsApiServices(this IServiceCollection services)
    {
        //services.AddSingleton<IModuleDbConfig, MCPToolsManageDbConfig>();

        //services.AddScoped<IMCPToolsManageService, MCPToolsManageService>();


        //services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        //{
        //    client.BaseAddress = new(GlobalData.MainDispatcherServer);
        //    client.Timeout = TimeSpan.FromSeconds(60);
        //});
        return services;
    }

    //后端API路由注册
    public static IApplicationBuilder AddSkillsApi(this IApplicationBuilder app)
    {
        return app;
        //return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        //{
        //    endpoints.MapMCPToolsManageApi();
        //});
    }




}

