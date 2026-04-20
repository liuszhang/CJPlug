using CJ.Plug.PlugBaseCore.Contracts;
using ForPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ForPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFor(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.ForPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, ForPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddForExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, ForPlugCommonExecuteService>();

            return services;
        }

    }
}
