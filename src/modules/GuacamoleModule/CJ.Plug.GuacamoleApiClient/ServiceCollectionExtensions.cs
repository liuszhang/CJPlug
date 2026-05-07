using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.GuacamoleApiClient
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 Guacamole API 客户端服务 (前端页面)
        /// </summary>
        public static IServiceCollection AddGuacamoleModulePageServices(this IServiceCollection services)
        {
            services.AddHttpClient<IGuacamoleApiClient, GuacamoleApiClient>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}
