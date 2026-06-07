using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using SignaturePadPlug.Services;

namespace SignaturePadPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTestStandalone(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.SignaturePadPlugSettingContent>()
                .AddScoped<IPlugCommonExecute, SignaturePadPlugExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTestStandaloneExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, SignaturePadPlugExecuteService>();

            return services;
        }

    }
}
