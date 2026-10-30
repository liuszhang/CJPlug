using CJ.Plug.ApiServer.Services;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToExternalPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

        // P7.1b-B-1：模块权限声明 —— 本模块无菜单（只有 API /api/cjprocess），此前未声明 ⇒
        //   模块管理页开关对它完全无效（B-1 对账时补入：ModuleId 清单 27 个中唯一漏网的业务模块）。
        // ⚠ 无菜单 ⇒ 矩阵分组名回落到 ModuleName，必须传中文显示名。Roles=["*"] = 零可见性回归。
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-to-external", ["*"], "对外接口(ToExternal)"));

        return services;
    }


    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "对外接口(ToExternal)";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-to-external";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "集成";
    }


    public static IServiceCollection AddToExternalModuleApiServices(this IServiceCollection services)
    {
        services.AddScoped<IProcessToExternalService, ProcessToExternalService>();
        return services;
    }

    public static IApplicationBuilder AddToExternalModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapProcessToExternalApi();
        });
    }

}

