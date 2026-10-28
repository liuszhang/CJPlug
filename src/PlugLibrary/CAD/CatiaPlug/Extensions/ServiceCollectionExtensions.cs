using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using CatiaPlug.Services;

namespace CatiaPlug.Extensions
{
    /// <summary>
    /// Catia 插头依赖注入扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入（设置页面、动作页面）
        /// </summary>
        public static IServiceCollection AddCatia(this IServiceCollection services)
        {
            services
                // CatiaPlug services (主插头)
                .AddScoped<IPlugCommonSettingContent, CatiaPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, CatiaPlugCommonExecuteService>()
                // CatiaGetParameters services (获取参数)
                .AddScoped<IPlugCommonSettingContent, CatiaGetParametersPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, CatiaGetParametersPlugActionSettingContent>()
                // CatiaSetParameters services (设置参数)
                .AddScoped<IPlugCommonSettingContent, CatiaSetParametersPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, CatiaSetParametersPlugActionSettingContent>()
                // CatiaExportStp services (导出STP)
                .AddScoped<IPlugCommonSettingContent, CatiaExportStpPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, CatiaExportStpPlugActionSettingContent>()
                // CatiaExportStl services (导出STL)
                .AddScoped<IPlugCommonSettingContent, CatiaExportStlPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, CatiaExportStlPlugActionSettingContent>();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入（执行服务）
        /// </summary>
        public static IServiceCollection AddCatiaExecute(this IServiceCollection services)
        {
            services
                // CatiaPlug execute service (主插头执行)
                .AddScoped<IPlugCommonExecute, CatiaPlugCommonExecuteService>()
                // CatiaGetParameters execute service (获取参数)
                .AddScoped<IPlugCommonExecute, CatiaGetParametersCommonExecuteService>()
                // CatiaSetParameters execute service (设置参数)
                .AddScoped<IPlugCommonExecute, CatiaSetParametersCommonExecuteService>()
                // CatiaExportStp execute service (导出STP)
                .AddScoped<IPlugCommonExecute, CatiaExportStpCommonExecuteService>()
                // CatiaExportStl execute service (导出STL)
                .AddScoped<IPlugCommonExecute, CatiaExportStlCommonExecuteService>();

            return services;
        }
    }
}
