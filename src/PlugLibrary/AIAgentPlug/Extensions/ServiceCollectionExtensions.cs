using AiAgentPlug.Services;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AiAgentPlug.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAiAgent(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, AiAgentPlugCommonSettingContent>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAiAgentExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, AiAgentPlugCommonExecuteService>();


            return services;
        }

    }
}
