using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using PPTPlug.Services;

namespace PPTPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPPT(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, PPTPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, PPTPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPPTExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, PPTPlugCommonExecuteService>();

            return services;
        }
    }
}
