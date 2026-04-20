using CJ.Plug.PlugBaseCore.Contracts;
using IfPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IfPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddIf(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.IfPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, IfPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddIfExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, IfPlugCommonExecuteService>();

            return services;
        }

    }
}
