using CJ.Plug.PlugBaseCore.Contracts;
using AndPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAnd(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.AndPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, AndPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAndExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, AndPlugCommonExecuteService>();

            return services;
        }

    }
}
