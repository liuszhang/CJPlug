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
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "技能执行";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-skills";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "集成";
    }


    //前端服务包
    public static IServiceCollection AddSkillsModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, SkillsManageMenu>();

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
        //services.AddSingleton<IModuleBusinessDbConfig,  MCPToolsManageDbConfig>();

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

