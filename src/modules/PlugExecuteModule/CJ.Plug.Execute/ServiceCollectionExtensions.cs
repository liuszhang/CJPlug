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
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

        services.AddHttpClient<IExecuteApiClient,ExecuteApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }


    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "插头执行";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-execute";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
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

