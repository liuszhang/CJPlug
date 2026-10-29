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
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "作业管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-job-manage";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }

    public static IServiceCollection AddJobManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, JobManageMenu>();

        services.AddHttpClient<IJobManageApiClient, JobManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddJobManageModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleBusinessDbConfig,  JobModuleDbConfig>();

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

