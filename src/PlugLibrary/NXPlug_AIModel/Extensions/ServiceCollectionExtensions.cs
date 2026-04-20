
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NXPlug_AIModel.Services;

namespace NXPlug_AIModel.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddNXAIModel(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, NXAIModelPlugCommonSettingContent>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddNXAIModelExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, NXAIModelPlugCommonExecuteService>();


            return services;
        }

    }
}
