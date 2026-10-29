using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.HostWebServer.Shell;
using CJ.Plug.Login;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJCore.Framework.Api;
using CJCore.Modules.ModuleAdmin;
using CJCore.Modules.SystemConfig;
using CJCore.Modules.SystemConfig.UI;
using Elsa.Extensions;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using MudExtensions.Services;
using PlugsBundle;
using Radzen;
using System.Runtime.InteropServices;

namespace CJ.Plug.HostWebServer;

/// <summary>
/// CJPlug Web 宿主（Blazor Server）的服务注册与请求管道。
///
/// <para><b>为什么抽成扩展方法</b>：2026-10-29《CJPlug 服务架构简化》第二轮（C 档 + 单端口）
/// 把这个 Blazor 宿主并入 <c>CJ.Plug.ApiServer</c> 进程。两个宿主各自带顶层语句
/// (<c>Program.cs</c>)，而 .NET 不允许被引用的库项目包含顶层语句（CS8805），
/// 因此必须把注册与管道抽成可复用扩展：
/// <list type="bullet">
/// <item><c>CJ.Plug.ApiServer</c> —— 合并后的唯一宿主，调用下面三个方法</item>
/// <item><c>CJ.Plug.HostWebServer/Program.cs</c> —— 退化为薄壳（仍可独立启动 = 回退路径）</item>
/// </list>
/// </para>
///
/// <para><b>责任边界</b>：本文件<b>不</b>负责 host 级基础设施 —— <c>Console</c> 编码、
/// <c>builder.AddServiceDefaults()</c>、<c>builder.WebHost.UseUrls()</c>、
/// <c>Log.Logger</c>、<c>builder.Build()</c>、<c>app.Run()</c> 一律由主程序负责。
/// 因为合并后这些只能执行一次（重复调用会重复注册 OpenTelemetry exporter / 覆盖日志 sink）。</para>
///
/// <para><b>⚠ 注册顺序敏感项</b>：<c>IModule</c> 与 <c>IMenuService</c> 的注册顺序决定
/// 菜单分组与 head 资源的渲染顺序，改动前先看各 Module 的类注释。</para>
/// </summary>
public static class WebHostStartup
{
    /// <summary>Web 入口端口（历史契约）。单端口收敛后即为整个主服务端的唯一端口。</summary>
    public const int WebPort = 5066;

    /// <summary>
    /// 判断某个请求路径是否需要「认证身份」与「授权中间件」。
    ///
    /// <para><b>为什么按路径而不按端口</b>：2026-10-29 第二轮把全部功能收敛到单端口（5066）后，
    /// 端口不再是区分手段；而这两件事的诉求本来是相反的 ——</para>
    /// <list type="bullet">
    /// <item>CJCore 管理端点（<c>/api/module-admin/*</c>、<c>/api/system-config/*</c>）挂了
    ///   <c>RequireAuthorization</c> 元数据，缺 <c>AuthorizationMiddleware</c> 直接抛异常、缺 scheme 一律 401；</item>
    /// <item>主 API 侧（27 个模块端点）与调度 Hub 则**依赖「无认证身份」的既有行为** ——
    ///   一旦全局启用授权，按 Roles 过滤的菜单会被筛成空
    ///   （2026-10-29 Phase 3 实测：首页 mud-nav 容器在、菜单项全丢）。</item>
    /// </list>
    ///
    /// <para>因此只对「页面 + CJCore 管理端点 + Elsa」放行身份，其余保持无身份。</para>
    /// </summary>
    public static bool NeedsIdentity(PathString path)
    {
        // ① CJCore 管理端点：明确需要（它们带 RequireAuthorization 元数据）。
        //    必须排在 ② 之前 —— 它们也以 /api 开头。
        if (path.StartsWithSegments("/api/module-admin") || path.StartsWithSegments("/api/system-config"))
            return true;

        // ② 业务 API / 调度 Hub / MCP 面：保持「无认证身份」。
        if (path.StartsWithSegments("/api")
            || path.StartsWithSegments("/mainHub")
            || path.StartsWithSegments("/mcp"))
            return false;

        // ③ 其余：Blazor 页面（含 _blazor circuit、_framework 静态资源）与 Elsa 引擎端点。
        return true;
    }

    /// <summary>
    /// 注册 Blazor 宿主及其依赖的全部服务（MudBlazor / CJCore 框架 / Elsa Studio / 模块壳 / A2A / Agent）。
    /// <para>幂等性：<b>不</b>幂等 —— 每个宿主进程只能调用一次。</para>
    /// </summary>
    public static WebApplicationBuilder AddPlugWebHost(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // 显式加载静态 Web 资产清单（Build 模式而非 Publish 时需要）。
        // ⚠ 合并后必须保留：ApiServer 自己是 Web SDK 项目，但 MudBlazor / Radzen 等 RCL 的
        //   _content/ 资产只有在 Web 宿主侧才会被 StaticWebAssets 清单收录。
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, configuration);

        services.AddMudServices();
        services.AddMudExtensions();

        // ── Blazor 三件套 ──────────────────────────────────────────────────
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddRazorPages();
        services.AddServerSideBlazor(options =>
        {
            // Register the root components.
            options.RootComponents.RegisterCustomElsaStudioElements();
        });

        // ── SignalR ───────────────────────────────────────────────────────
        // ⚠ MaximumReceiveMessageSize 必须保留 5MB：流程编辑器保存大工作流时会超过默认 32KB。
        //   合并到 ApiServer 后，那里原有的裸 AddSignalR() 不会覆盖本配置（Configure 是追加语义）。
        services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
        });

        // 注册 EventAggregator 服务
        services.AddEventAggregator();

        // 添加 ELSA 流程引擎相关服务注册（Studio 前端侧，走 remote backend 连引擎）
        builder.AddElsaServicesForWeb();

        // CJPlug 自有的 IAppBarService / DefaultAppBarService 已退役 ——
        // AppBar 注册统一走 CJCore 的 Singleton IAppBarService（由 AddCJCoreFramework 注册）。
        services.ConfigModulePageServices();

        // VNC/SSH 远程桌面 WebSocket 代理服务
        services.AddSingleton<CJ.Plug.GuacamoleApi.Services.VncWebSocketProxy>();
        services.AddSingleton<CJ.Plug.GuacamoleApi.Services.SshWebSocketProxy>();
        services.AddSingleton<CJ.Plug.GuacamoleApi.Services.CaptureWebSocketProxy>();

        // 添加插头前端集合包依赖
        services.AddPlugsBundle();
        services.AddXmlConfiguredServices();
        services.AddXmlConfiguredExecuteServices();

        // 添加 Radzen 控件服务
        services.AddRadzenComponents();

        services.AddOutputCache();

        // 其他服务配置
        services.ConfigOtherServices();

        // ── CJCore 框架接入（P1 接线 / P2 换壳） ──────────────────────────
        services.AddCJCoreFramework(configuration, o =>
        {
            o.ProductName = "寸金插座平台";
            o.DefaultIconUrl = "/plug2.png";
            // P2–P4 期间真拦截由 PlugGuardModule（localStorage 守卫）承担；
            // LoginPath 供框架在未登录访问受保护页时跳转（P5 起改为 /three-role/login）。
            o.LoginPath = "/login";
            o.EnableLogDrawer = true;    // 平台日志抽屉（Alt+L）
            o.EnableLogAppBarButton = true;
            o.EnableThemeSwitcher = true;
            // 贴近换壳前的观感（原 MainLayout 的 AppBar 紫 + 暗色紫蓝主色）
            o.Theme = new CJCore.Framework.Abstractions.ThemeOptions
            {
                Light = new() { AppbarBackground = "rgba(89, 74, 226, 1)", AppbarText = "#ffffff", Primary = "#594ae2" },
                Dark = new() { AppbarBackground = "rgba(89, 74, 226, 1)", AppbarText = "#ffffff", Primary = "#7e6fff" },
            };
        });

        // ── P2 认证 + P5 前临时认证占位 ────────────────────────────────────
        // 必要性：CJCore 的端点（P6 /api/module-admin/*、P7 /api/system-config/*）都以
        // RequireAuthorization 挂了授权元数据 —— 缺 AuthorizationMiddleware 时请求直接抛异常；
        // 有中间件却没 scheme 时一律 401。详见 PlugTempAuthHandler 的类注释。
        // ⚠ P5 删除：接入 ThreeRoleAdmin 后连带 PlugAnonymousAuthStateProvider 一起删。
        services.AddAuthentication(PlugTempAuthHandler.Scheme)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                       PlugTempAuthHandler>(PlugTempAuthHandler.Scheme, null);

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
                           PlugTempAdminAuthStateProvider>();

        // ── P7.1：通用配置页 /system-config ───────────────────────────────
        // ① 宿主持久化 Store 必须先注册 —— AddCJCoreSystemConfig 内部是 TryAdd，不会覆盖。
        services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigStore,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigStore>();
        services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigFieldProvider,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigFieldProvider>();
        services.AddCJCoreSystemConfig(o =>
        {
            // CJPlug 现行最高权限角色名（P5 换 ThreeRoleAdmin 后改为 system_admin）
            o.AccessRoles = ["Admin"];
        });
        // ② UI 层（页面 + 模块实例）。它自带的菜单把分组写死成"系统"，
        //    与 D3=A（菜单归一「通用/管理/调试」）冲突 ⇒ 摘掉后换宿主自己的。
        services.AddCJCoreSystemConfigUI();
        foreach (var d in services
                     .Where(d => d.ServiceType == typeof(CJCore.Framework.Abstractions.IMenuService)
                              && d.ImplementationType == typeof(CJCore.Modules.SystemConfig.UI.SystemConfigUiMenuService))
                     .ToList())
        {
            services.Remove(d);
        }
        services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigMenuService>();

        // ── P8：A2A 工具面（CJPlug 动态工具 → CJCore IToolRegistry） ──────
        // 注册表用框架自带的 DefaultToolRegistry：它天生支持「static 池（进程内注入）+ dynamic 池」，
        // 正好匹配 CJPlug 的工具来自「运行时发布的工作流」这一事实。
        services.AddSingleton<CJCore.AgentTool.MCPTools.DefaultToolRegistry>();
        services.AddSingleton<CJCore.AgentTool.MCPTools.IToolRegistry>(
            sp => sp.GetRequiredService<CJCore.AgentTool.MCPTools.DefaultToolRegistry>());
        services.AddHttpClient(CJ.Plug.HostWebServer.A2A.PlugMcpBridgeClientOptions.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(
                configuration["PlugMcpServer:Url"]
                ?? CJ.Plug.HostWebServer.A2A.PlugMcpBridgeClientOptions.DefaultBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(120);   // 工具执行是长任务（跑真实工位机）
        });
        services.AddHostedService<CJ.Plug.HostWebServer.A2A.PlugMcpToolSyncService>();

        // ── P8.3：Agent 消费链 ────────────────────────────────────────────
        // 用户提问 → LLMAgentBase（多轮循环）→ IToolRegistry.Resolve → PlugMcpToolExecutor → MCP → 工作流。
        services.AddHttpClient<CJ.Plug.LlmConfigApiClient.ILlmConfigApiClient,
                               CJ.Plug.LlmConfigApiClient.LlmConfigApiClient>(client =>
        {
            client.BaseAddress = new Uri(GlobalData.MainApiServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        // 泛型版 = 用我们自己的配置读取器替换框架的 DbLLMConfigReader，并顺带注册 transient HttpClient
        // （⚠ LLMChatService 内部会写 HttpClient 的 BaseAddress/Timeout，必须 transient，否则第二次调用即抛
        //   InvalidOperationException「This instance has already started one or more requests」）。
        CJCore.LLM.LLMClient.ServiceCollectionExtensions
            .AddLLMChatService<CJ.Plug.HostWebServer.Agent.PlugLlmConfigReader>(services);
        // Agent 与其配置：基类注释写明"生命周期无关"（每次执行内按调用建 scope 解析 LLM）⇒ Singleton 常驻安全。
        services.AddSingleton<CJ.Plug.HostWebServer.Agent.PlugAgentConfig>();
        services.AddSingleton<CJ.Plug.HostWebServer.Agent.PlugWorkflowAgent>();
        // ① IModule：宿主程序集的 @page（/debug/plug-agent）要进路由发现，必须有一个宿主内的 IModule。
        // ② IMenuService：菜单项挂「调试」组。
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.HostWebServer.Agent.PlugAgentModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.HostWebServer.Agent.PlugAgentMenuService>();

        // ── P2：宿主壳模块（替代原 App_All / Routes / MainLayout / MyNavMenu / EmbeddedLayout）──
        // 注册顺序 = FrameworkApp 中 head 资源的渲染顺序（顺序敏感项见 PlugHeadModule 注释）。
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugHeadModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugShellModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugRuntimeModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugGuardModule>();

        // 原 MyNavMenu 里硬编码的三条入口（下载包 / VNC×2）改由菜单服务贡献
        services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.MainPageContent.Shell.Menus.PlugShellMenu>();

        // 执行日志抽屉：AppBar 按钮与抽屉分处两棵渲染子树，用单例桥接
        services.AddSingleton<CJ.Plug.MainPageContent.Shell.PlugExecLogService>();

        // ── P6：模块目录与启停（/admin/modules） ──────────────────────────
        // ⚠ 与 P4 的关系：此刻 CJPlug 还没接 CJCore Data 底座，启停状态走「进程内存 + 中立交换文件」
        //   （module-enablement.json）；P4 接上 Data 底座后同一行代码即升级为 MA_ 表持久化。
        services.AddCJCoreModuleAdmin(o =>
        {
            o.AdminRoles = ["Admin"];
        });
        // Ⅱ ModuleAdmin 自带的菜单把分组写死成"系统" ⇒ 摘掉后换宿主自己的。
        foreach (var d in services
                     .Where(d => d.ServiceType == typeof(CJCore.Framework.Abstractions.IMenuService)
                              && d.ImplementationType == typeof(CJCore.Modules.ModuleAdmin.ModuleAdminUiMenuService))
                     .ToList())
        {
            services.Remove(d);
        }
        services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.HostWebServer.SystemConfig.PlugModuleAdminMenuService>();

        // 开启详细错误信息
        services.Configure<CircuitOptions>(options =>
        {
            options.DetailedErrors = true;
        });

        return builder;
    }

    /// <summary>
    /// 注册 Web 宿主的中间件与除 Razor 根组件之外的端点。
    /// <para>
    /// ⚠ <b>调用位置有约束</b>：必须在 <c>UseRouting()</c> / <c>UseAuthentication()</c> /
    /// <c>UseAntiforgery()</c> 之后、<c>MapPlugWebRazorComponents()</c> 之前调用。
    /// 内部会依次执行 <c>UseStaticFiles</c> → <c>UseOutputCache</c> → <c>MapStaticAssets</c> → 各类端点。
    /// </para>
    /// </summary>
    public static WebApplication MapPlugWebHost(this WebApplication app)
    {
        // 1. 判断当前操作系统并定义基础路径
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var basePlugPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../PlugConfig/Plugs");

        var baseWebFilePath = isWindows
            ? GlobalData.MainWebFileServer // Windows使用现有配置
            : "/cj/apps"; // Linux对应的web文件路径

        // 2. 确保目录存在（关键步骤：自动创建文件夹）
        Directory.CreateDirectory(basePlugPath);
        Directory.CreateDirectory(baseWebFilePath);

        Console.WriteLine($"Base Web Path: {baseWebFilePath}");

        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            FileProvider = new PhysicalFileProvider(baseWebFilePath),
            RequestPath = "/webFiles"
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            FileProvider = new PhysicalFileProvider(basePlugPath),
            RequestPath = "/plugconfig"
        });

        app.UseOutputCache();

        app.MapStaticAssets();

        // P6：模块管理端点（/admin/modules）+ 管理 API
        app.MapCJCoreModuleAdmin();
        // P7.1：系统配置端点（GET/PUT/POST /api/system-config）
        app.MapCJCoreSystemConfig();

        // P8.3.5：Agent 工具面只读端点 —— 与调试页同源（IToolRegistry.AllEnabledDefinitions），
        // 便于用 curl 验收"工具真的进了注册表"，不必开浏览器。
        // ⚠ 只读：返回名称/描述/参数 schema，不提供执行入口。
        app.MapGet("/api/plug-agent/tools", (CJ.Plug.HostWebServer.Agent.PlugWorkflowAgent agent) =>
            agent.GetVisibleTools()
                 .Select(t => new { t.Name, t.Description, t.ParametersJson })
                 .ToList())
           .WithName("PlugAgentTools")
           .WithTags("P8-Agent");

        // ⚠ 刻意**不**在这里调 app.MapRemoteDesktopApi()：
        //   该端点（/api/remote/vnc 等，端点名 VncWebSocketProxy）由 Guacamole 模块的 API 扩展映射，
        //   合并形态下 ApiServer 的 ConfigModuleApis() 已经扫到它 —— 两处都调会抛
        //   「Duplicate endpoint name 'VncWebSocketProxy' found on 'api/remote/vnc' and 'api/remote/vnc'」，
        //   而且这个异常发生在 EndpointRoutingMiddleware 初始化阶段 ⇒ **本进程所有请求一律 500**
        //   （含 /mainHub 协商、/api/plugbridge/*），极易误判成 Hub 故障。
        //   独立启动形态（Program.cs 薄壳）没有 ConfigModuleApis()，由它自己补一次。

        return app;
    }

    /// <summary>
    /// 映射框架统一根组件（Razor 路由发现）。
    /// <para>
    /// ⚠ <b>必须是最后一个端点映射</b>：框架自带 NotFoundPage 的 catch-all（<c>/{**path}</c>）
    /// 会吃掉所有未匹配路由，放在业务端点之前会把它们全部遮住。
    /// </para>
    /// <para>
    /// ⚠ 必须走框架提供的 <c>MapCJCoreRazorComponents()</c>：它内部做的
    /// <c>MapRazorComponents&lt;FrameworkApp&gt;()</c> + <c>AddAdditionalAssemblies(IModule 程序集)</c>
    /// 会排除根组件程序集 CJCore.Framework.UI。手写这段样板会让 Framework.UI 同时成为
    /// AppAssembly 与 AdditionalAssemblies 成员 ⇒ 启动即抛 InvalidOperationException
    /// "Assembly already defined"，且异常发生在 UseAuthorization 构造 AuthorizationPolicyCache 时，
    /// 编译期完全看不出来。
    /// </para>
    /// </summary>
    public static WebApplication MapPlugWebRazorComponents(this WebApplication app)
    {
        // Radzen 组件用 InteractiveAuto ⇒ 额外追加 WebAssembly 渲染模式（框架默认只加 Server）。
        app.MapCJCoreRazorComponents()
            .AddInteractiveWebAssemblyRenderMode();

        return app;
    }
}
