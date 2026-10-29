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
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "知识管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-knowledge";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "集成";
    }

    /// <summary>
    /// 前端服务包：注册模块、菜单、HttpClient
    /// </summary>
    public static IServiceCollection AddKnowledgeModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, KnowledgeManageMenu>();

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
