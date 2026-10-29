using CJ.Plug.FileManageApi.Apis;
using CJ.Plug.FileManageApi.Contracts;
using CJ.Plug.FileManageApi.DbContext;
using CJ.Plug.FileManageApi.Services;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

        services.AddHttpClient<IFileManageApiClient, FileManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddFileManageModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleBusinessDbConfig,  FileModuleDbConfig>();

        services.AddScoped<IFileManageService, FileManageService>();

        services.AddHttpClient<IFileManageApiClient, FileManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    public static IApplicationBuilder AddFileManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapFileManageApi();
        });
    }


    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "文件管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-file-manage";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }

}

