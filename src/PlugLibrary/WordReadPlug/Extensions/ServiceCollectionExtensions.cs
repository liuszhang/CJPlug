using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WordReadPlug.Services;

namespace WordReadPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        public static IServiceCollection AddWordRead(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.WordReadPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, WordReadPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        public static IServiceCollection AddWordReadExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, WordReadPlugCommonExecuteService>();

            return services;
        }
    }
}
