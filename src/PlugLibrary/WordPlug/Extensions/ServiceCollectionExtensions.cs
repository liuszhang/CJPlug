
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WordPlug.Services;

namespace WordPlug.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWord(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, Services.WordPlugCommonSettingContent>()
                .AddScoped<IPlugCommonExecute, WordPlugCommonExecuteService>();

           

            //services.AddTransient<IMyBusinessToolDialogShow>(sp => new MyPatranDialogShow(), name: "NX");

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWordExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, WordPlugCommonExecuteService>();
            //    .AddScoped<IBusinessToolEnumProvider, NXToolEnumProvider>()
            //    .AddScoped<IExtraInputProvider, NXExtraInputProvider>()
            //    .AddScoped<IExtraOutputProvider, NXExtraOutputProvider>()
            //    .AddScoped<INXExecuteAction, NXExecuteAction>()
            //    .AddScoped<IPlugExecute, NXPlugExecute>();


            return services;
        }

    }
}
