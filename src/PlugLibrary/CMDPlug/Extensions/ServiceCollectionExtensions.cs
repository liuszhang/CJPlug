using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CMDPlug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CMDPlug.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCMD(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, CMDPlugCommonSettingContent>()
                .AddScoped<IPlugActionSettingContent, CMDPlugActionSettingContent>();
           

            //services.AddTransient<IMyBusinessToolDialogShow>(sp => new MyPatranDialogShow(), name: "NX");

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCMDExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, CMDPlugCommonExecuteService>()
            //    .AddScoped<IBusinessToolEnumProvider, NXToolEnumProvider>()
            //    .AddScoped<IExtraInputProvider, NXExtraInputProvider>()
            //    .AddScoped<IExtraOutputProvider, NXExtraOutputProvider>()
            //    .AddScoped<INXExecuteAction, NXExecuteAction>()
            //    .AddScoped<IPlugExecute, NXPlugExecute>();
            ;

            return services;
        }

    }
}
