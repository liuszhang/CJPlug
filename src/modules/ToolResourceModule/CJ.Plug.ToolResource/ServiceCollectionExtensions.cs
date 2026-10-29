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
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "工具资源";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-tool-resource";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }

    /// <summary>
    /// 注册前端页面服务：菜单 + ApiClient（UI 端调用后端 API 使用）
    /// </summary>
    public static IServiceCollection AddToolResourceModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, ToolResourceMenu>();

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
