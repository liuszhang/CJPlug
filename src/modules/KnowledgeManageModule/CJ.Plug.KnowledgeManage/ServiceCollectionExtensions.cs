using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.KnowledgeApiClient;
using CJ.Plug.KnowledgeManage.Menus;
using CJ.Plug.KnowledgeManageApi;
using CJ.Plug.KnowledgeManageApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.KnowledgeManage;

public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }

    /// <summary>
    /// 前端服务包：注册模块、菜单、HttpClient
    /// </summary>
    public static IServiceCollection AddKnowledgeModuleServices(this IServiceCollection services)
    {
        services.AddScoped<IModule, Module>();
        services.AddScoped<IMenuService, KnowledgeManageMenu>();

        services.AddHttpClient<IKnowledgeApiClient, KnowledgeApiClient.KnowledgeApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    /// <summary>
    /// 后端API服务包：注册 DbConfig、Service 和种子数据
    /// </summary>
    public static IServiceCollection AddKnowledgeApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, KnowledgeManageDbConfig>();

        services.AddScoped<IKnowledgeManageService, KnowledgeManageService>();

        services.AddSingleton<ISeedDataProvider, KnowledgeManageSeedDataProvider>();

        return services;
    }

    /// <summary>
    /// 后端API路由注册
    /// </summary>
    public static IApplicationBuilder AddKnowledgeApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapKnowledgeManageApi();
        });
    }
}
