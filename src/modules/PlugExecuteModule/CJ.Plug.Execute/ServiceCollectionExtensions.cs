using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PlugExecuteModule.Permissions;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugExecutePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        services.AddHttpClient<IExecuteApiClient,ExecuteApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }


    public class Module : ModuleBase
    {
    }

    public static IServiceCollection AddPlugExecuteModuleApiServices(this IServiceCollection services)
    {
        //添加插头执行服务
        services.AddScoped<IPlugCommonExecute>(sp => new DefaultPlugExecuteService(sp, sp.GetRequiredService<IToolExecuteService>()));
        // 接口类 Category 回退处理器
        services.AddScoped<IPlugCommonExecute, ApiPlugCategoryFallbackHandler>();
        // 脚本类 Category 回退处理器
        services.AddScoped<IPlugCommonExecute, ScriptPlugCategoryFallbackHandler>();

        services.AddScoped<IPlugExecuteService, PlugExecuteService>();

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, PlugExecutePermissionProvider>();

        services.AddHttpClient<IExecuteApiClient,ExecuteApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddPlugExecuteModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPlugExecuteApi();
        });
    }

}

