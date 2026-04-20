
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using PythonPlug.Services;

namespace PythonPlug.Extensions
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“ServiceCollectionExtensions”。
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPython(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonSettingContent, PythonPlugCommonSettingContent>();
                //.AddScoped<IPlugCommonExecute, PythonPlugCommonExecuteService>();
            //.AddSingleton<IPlugDisplaySettingsProvider, PythonPlugDisplaySettingProvider>();

            //前端也需要添加执行服务，以适配执行测试场景
            services.AddPythonExecute();

            return services;
        }

        /// <summary>
        /// 添加后端API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPythonExecute(this IServiceCollection services)
        {
            services
                .AddScoped<IPlugCommonExecute, PythonPlugCommonExecuteService>()
                ;


            return services;
        }

    }
}
