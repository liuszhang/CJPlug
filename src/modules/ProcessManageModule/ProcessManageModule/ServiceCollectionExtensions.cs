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
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService,ProcessManageMenu>();

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
    }

}

