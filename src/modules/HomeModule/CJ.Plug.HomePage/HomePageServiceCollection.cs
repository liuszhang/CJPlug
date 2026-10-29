using CJ.Plug.HomePage.Menus;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.HomePage
{
    public static class HomePageServiceCollection
    {
        public static IServiceCollection AddHomePageModuleServices(this IServiceCollection services)
        {
            
            services.AddSingleton<CJCore.Framework.Abstractions.IModule, HomePageModule>();
            services.AddScoped<CJCore.Framework.Abstractions.IMenuService, HomePageMenu>();

            return services;
        }

        /// <summary>
        /// 宿主壳模块：仅用于把本程序集纳入 <c>FrameworkRoutes</c> 的路由发现。
        /// P1 换契约后基类改为 CJCore 的 <c>ModuleBase</c>（2026-10-29《CJPlug 接入 CJCore》D2=A1）。
        /// </summary>
        public class HomePageModule : CJCore.Framework.Abstractions.ModuleBase
        {
            /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
            public override string Name => "首页";

            /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
            public override string ModuleId => "plug-home";

            /// <summary>模块管理页分组。</summary>
            public override string Group => "内核";
        }
    }
}