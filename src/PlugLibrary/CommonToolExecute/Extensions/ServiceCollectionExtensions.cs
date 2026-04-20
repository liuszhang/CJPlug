
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CommonToolExecute.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCommonToolExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.CommonToolExecutePlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, CommonToolExecutePlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCommonToolExecuteExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, CommonToolExecutePlugCommonExecuteService>();

            return services;
        }

    }
}
