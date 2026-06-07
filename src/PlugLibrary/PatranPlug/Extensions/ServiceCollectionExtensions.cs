using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using PatranPlug.Services;

namespace PatranPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        public static IServiceCollection AddPatran(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, PatranPlugCommonSettingContent>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        public static IServiceCollection AddPatranExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, PatranPlugCommonExecuteService>();

            return services;
        }
    }
}
