using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Shared;
using CJ.Plug.SystemConfigApiClient;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.SystemConfig;

public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "系统配置读侧";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-system-config-read";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "内核";
    }

    /// <summary>
    /// 2026-10-29 P7.1：配置<b>页面</b>已由 CJCore 通用配置页 <c>/system-config</c> 接管
    ///（《（设计中）CJPlug接入CJCore实施计划-20261029》P7.1 / Q10）—— 本项目的
    /// <c>Pages/SystemConfigPage.razor</c> 与 <c>Menus/SystemConfigMenu.cs</c> 已删除，
    /// 菜单改由 <c>CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigMenuService</c> 贡献。
    ///
    /// <para><b>这里刻意保留 ApiClient 注册</b>：写虽然改走 /system-config，<b>读</b>仍在本整套 API 上 ——
    /// 见 <c>CJ.Plug.PlugBaseCore/Services/ToolExecuteService.cs</c>（经 MainApiClient 打 ApiServer 的
    /// <c>/api/systemconfig/*</c>）。ApiClient 一旦不注册，远程查看模式会在工具侧读不到。</para>
    /// </summary>
    public static IServiceCollection AddSystemConfigPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

        services.AddHttpClient("SystemConfig", client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // SystemConfig API 客户端（跨进程读取 RemoteViewMode 用；见上）
        services.AddSystemConfigApiClient();

        return services;
    }
}
