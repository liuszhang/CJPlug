using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using LLMPlug.Services;

namespace LLMPlug.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        public static IServiceCollection AddLLMPlug(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.LLMPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, LLMPlugCommonExecuteService>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        public static IServiceCollection AddLLMPlugExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, LLMPlugCommonExecuteService>();

            return services;
        }
    }
}
