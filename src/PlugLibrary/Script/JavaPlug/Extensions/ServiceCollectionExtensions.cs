using CJ.Plug.PlugBaseCore.Contracts;
using JavaPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JavaPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        public static IServiceCollection AddJava(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, JavaPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, JavaPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        public static IServiceCollection AddJavaExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, JavaPlugCommonExecuteService>();

            return services;
        }
    }
}
