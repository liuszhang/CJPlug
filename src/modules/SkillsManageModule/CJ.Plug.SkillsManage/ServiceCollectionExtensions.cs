using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.SkillApiClient;
using CJ.Plug.SkillsManage.Menus;
using CJ.Plug.SkillsManageApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

        services.AddHttpClient<ISkillApiClient, SkillApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API服务包
    public static IServiceCollection AddSkillsApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, SkillsManageDbConfig>();

        services.AddScoped<ISkillsManageService, SkillsManageService>();

        return services;
    }

    //后端API路由注册
    public static IApplicationBuilder AddSkillsApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapSkillsManageApi();
        });
    }
}
