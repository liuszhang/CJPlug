using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.TASApiClient;
using CJ.Plug.ToolActionSettingApi.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;
using ToolActionSettingModule.Permissions;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "工具动作配置";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-tool-action";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }



    public static IServiceCollection AddTASPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, ToolActionSettingMenu>();

        // P5.5：模块权限声明（矩阵页可见性 + 模块内功能键归属锚点）。
        // ⚠ 与本模块的 IFunctionPermissionProvider（功能点：view/create/edit/delete/publish/station-*）**不是同一体系**，
        //   而是两个层次：
        //     ① 本声明（AccessKey = plug-tool-action）→ 模块级角色白名单，喂给鉴权判定链；
        //     ② 功能点 Provider → 权限矩阵里的按钮行，靠 FunctionPermissionItem.ParentKey 挂到本键下。
        //   2026-10-30 更正：旧注释称功能点 Provider「全仓只注册无消费方」，**该结论是错的** ——
        //   它当时注册的是 CJPlug 自有的同名接口（CJ.Plug.Models.Contracts），而权限矩阵注入的是
        //   CJCore.Framework.Abstractions.IFunctionPermissionProvider，DI 按类型解析导致 27 个功能点静默不可见。
        //   接口已在 P5.5 第三批归口到 CJCore 版，功能点现已能进矩阵。
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-tool-action", ["system_admin"], "工具动作配置"));
        services.AddHttpClient<ITASApiClient,TASApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddGuiDesignerService();

        return services;
    }


    


    public static IServiceCollection AddTASModuleApiServices(this IServiceCollection services)
    {
        //services.AddDbContext<TASDbContext>(options =>
        //        options.UseSqlite(DbConnectionString.ConnectionString));

        services.AddSingleton<IModuleBusinessDbConfig, TASModuleDbConfig>();

        services.AddScoped<IPlugManageService, PlugManageService>();

        // 注册功能点权限提供者（P5.5 第三批：全限定为 CJCore 版，自有同名接口已删除）
        services.AddSingleton<CJCore.Framework.Abstractions.IFunctionPermissionProvider, ToolActionSettingPermissionProvider>();

        services.AddHttpClient<ITASApiClient, TASApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });


        return services;
    }

    public static IApplicationBuilder AddTASModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPlugManageApi();
        });
    }
    

}

