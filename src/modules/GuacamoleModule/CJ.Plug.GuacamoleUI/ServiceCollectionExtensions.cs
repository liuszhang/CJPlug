using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.GuacamoleUI
{
    public static class ServiceCollectionExtensions
    {

        public class Module : ModuleBase
        {
            /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
            public override string Name => "远程桌面(Guacamole)";

            /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
            public override string ModuleId => "plug-guacamole";

            /// <summary>模块管理页分组。</summary>
            public override string Group => "集成";
        }
        /// <summary>
        /// 注册 Guacamole UI 模块服务
        /// </summary>
        public static IServiceCollection AddGuacamoleUIPageServices(this IServiceCollection services)
        {
            services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

            // 注册 API 客户端
            services.AddGuacamoleModulePageServices();

            return services;
        }
    }
}
