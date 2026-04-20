using CalculatorPlug.Services;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CalculatorPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCalculator(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.CalculatorPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, CalculatorPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCalculatorExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, CalculatorPlugCommonExecuteService>();

            return services;
        }

    }
}
