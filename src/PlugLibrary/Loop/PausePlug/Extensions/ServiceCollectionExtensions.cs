using CJ.Plug.PlugBaseCore.Contracts;
using PausePlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PausePlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        public static IServiceCollection AddPause(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, PausePlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, PausePlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        public static IServiceCollection AddPauseExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, PausePlugCommonExecuteService>();

            return services;
        }
    }
}
