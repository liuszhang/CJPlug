using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.SystemConfigApiClient;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 SystemConfig API 客户端服务（前端页面 + 服务端 ToolExecuteService 共用）
    /// </summary>
    public static IServiceCollection AddSystemConfigApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<ISystemConfigApiClient, SystemConfigApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
