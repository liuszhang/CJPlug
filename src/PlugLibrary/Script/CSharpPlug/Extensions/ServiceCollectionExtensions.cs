using CJ.Plug.PlugBaseCore.Contracts;
using CSharpPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCSharp(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.CSharpPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, CSharpPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCSharpExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, CSharpPlugCommonExecuteService>();

            return services;
        }

    }
}
