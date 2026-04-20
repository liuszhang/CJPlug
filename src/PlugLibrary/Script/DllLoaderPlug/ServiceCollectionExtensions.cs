using CJ.Plug.PlugBaseCore.Contracts;
using DllLoaderPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DllLoaderPlug
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDllLoaderPage(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, DllLoaderPlugSettings>()
                .AddScoped<IPlugCommonExecute, DllLoaderPlugExecuteService>()
                .AddScoped<DllLoaderPlug.Services.IDllMethodCacheService, DllLoaderPlug.Services.DllMethodCacheService>()
                .AddScoped<DllLoaderPlug.Services.IDllInvokerService, DllLoaderPlug.Services.DllInvokerService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDllLoaderExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, DllLoaderPlugExecuteService>();

            return services;
        }

    }
}
