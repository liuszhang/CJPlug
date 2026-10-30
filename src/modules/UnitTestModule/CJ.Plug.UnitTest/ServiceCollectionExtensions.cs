using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnitTestPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, UnitTestMenu>();

        // P6.4：模块权限声明 —— 供权限矩阵扫描本模块键，同时作为「功能键归属锚点」：
        //   DefaultModuleCatalog.BuildFeatures 用它反查 AccessKey 属于哪个模块，
        //   未声明的键不参与归组（漏注册 = 模块管理页无功能键、关模块对入口无效）。
        // ⚠ AccessKey 必须与 Module.ModuleId 逐字一致；ModuleName 必须与 Module.Name 逐字一致。
        //   Roles=["*"] = 公共项（所有已登录用户命中），保持本菜单原有「仅登录可见」口径不变（D-P6-4=A）。
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-unit-test", ["*"], "单元测试"));

        return services;
    }


    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "单元测试";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-unit-test";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "调试";
    }

}

