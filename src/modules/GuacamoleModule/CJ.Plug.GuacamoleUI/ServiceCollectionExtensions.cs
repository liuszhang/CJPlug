using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.GuacamoleUI
{
    public static class ServiceCollectionExtensions
    {

        public class Module : ModuleBase
        {
        }
        /// <summary>
        /// 注册 Guacamole UI 模块服务
        /// </summary>
        public static IServiceCollection AddGuacamoleUIPageServices(this IServiceCollection services)
        {
            services.AddScoped<IModule, Module>();

            // 注册 API 客户端
            services.AddGuacamoleModulePageServices();

            return services;
        }
    }
}
