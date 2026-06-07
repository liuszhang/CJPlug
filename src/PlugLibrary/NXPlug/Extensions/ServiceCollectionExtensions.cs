
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NXPlug.Services;

namespace NXPlug.Extensions
{
    /// <summary>
    /// NX 插头依赖注入扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入（设置页面、动作页面）
        /// </summary>
        public static IServiceCollection AddNX(this IServiceCollection services)
        {
            services
                // NXPlug services (主插头)
                .AddScoped<IPlugCommonSettingContent, NXPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, NXPlugCommonExecuteService>()
                // NXSetParameters services (设置参数)
                .AddScoped<IPlugCommonSettingContent, NXSetParametersPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, NXSetParametersPlugActionSettingContent>()
                // NXToStl services (模型转STL)
                .AddScoped<IPlugCommonSettingContent, NXToStlCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, NXToStlActionSettingContent>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入（执行服务）
        /// </summary>
        public static IServiceCollection AddNXExecute(this IServiceCollection services)
        {
            services
                // NXPlug execute service (主插头执行)
                .AddScoped<IPlugCommonExecute, NXPlugCommonExecuteService>()
                // NXGetParameters execute service (获取参数)
                .AddScoped<IPlugCommonExecute, NXGetParametersPlugCommonExecuteService>()
                // NXSetParameters execute service (设置参数)
                .AddScoped<IPlugCommonExecute, NXSetParametersPlugCommonExecuteService>()
                // NXToStl execute service (模型转STL)
                .AddScoped<IPlugCommonExecute, NXToStlCommonExecuteService>();

            return services;
        }
    }
}
