using CJ.Plug.Models.Contracts;
using CJ.Plug.SystemConfigApi.Apis;
using CJ.Plug.SystemConfigApi.Contracts;
using CJ.Plug.SystemConfigApi.Services;
using CJ.Plug.SystemConfigApiClient;
using CJ.Plug.SystemConfigModel.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.SystemConfigApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemConfigModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, SystemConfigDbConfig>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddSingleton<ISeedDataProvider, SystemConfigSeedDataProvider>();
        services.AddHttpClient();

        // API 进程内也注册 ApiClient（ToolExecuteService 等宿主服务经 MainApiClient 读取系统配置）
        services.AddSystemConfigApiClient();

        return services;
    }

    public static IApplicationBuilder AddSystemConfigModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapSystemConfigApi();
        });
    }
}
