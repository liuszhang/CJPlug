using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Shared;
using CJ.Plug.ProcessManageApi.Apis;
using CJ.Plug.ProcessManageApi.DbContext;
using CJ.Plug.ProcessManageApi.Services;
using CJ.Plug.ProcessManageApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;
using ProcessManageModule.Permissions;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcessManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService,ProcessManageMenu>();

        services.AddHttpClient<IProcessManageApiClient, ProcessManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    public static IServiceCollection AddProcessManageModuleApiServices(this IServiceCollection services)
    {
        //services.AddDbContext<ProcessDbContext>(options => options.UseSqlite(DbConnectionString.ConnectionString));
        services.AddSingleton<IModuleDbConfig, ProcessManageModuleDbConfig>();

        services.AddScoped<IProcessManageService, ProcessManageService>();

        // ★ AI Workflow Builder 服务
        services.AddSingleton<CapabilityRegistry>();
        services.AddSingleton<IDeepSeekService, DeepSeekService>();
        services.AddSingleton<AiWorkflowBuilderService>();
        services.AddSingleton<WorkflowTranslator>();

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, ProcessManagePermissionProvider>();

        services.AddHttpClient<IProcessManageApiClient, ProcessManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    public static IApplicationBuilder AddProcessManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapProcessManageApi();
            endpoints.MapAiWorkflowApi();
        });
    }


    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "流程管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-process-manage";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }

}

