using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.Login;
using CJCore.Framework.Api;
using CJCore.Modules.ModuleAdmin;
using CJCore.Modules.SystemConfig;
using CJCore.Modules.SystemConfig.UI;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using Elsa.Extensions;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using MudExtensions.Services;
using PlugsBundle;
using Radzen;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


//设置.NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    //.WriteTo.File("FrontLogs/log.txt",
    //    rollingInterval: RollingInterval.Day,
    //    rollOnFileSizeLimit: true)
    //.WriteTo.Sink(new InMemorySink())
    //.WriteTo.Sink(new SignalRLogSink(builder.Services.BuildServiceProvider().GetRequiredService<IHubContext<MainHub>>()))
    .WriteTo.Sink(new SignalRLogSink("Front"))
    //.WriteTo.Sink(builder.Services.BuildServiceProvider().GetService<ILogEventSink>())

    .CreateLogger();
//builder.Services.AddSingleton<IErrorBoundaryLogger, CustomErrorBoundaryLogger>();


var configuration = builder.Configuration;
//builder.WebHost.UseStaticWebAssets();
// 2026-10-07 删除：原先这里把配置键 env 覆盖到 ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT。
//   它发生在 CreateBuilder 之后（host 环境已定型）⇒ 对 host 环境无效，却让"配置里写着 Development"
//   看着能控制环境，并会污染子进程环境。环境名一律交给标准来源（launchSettings / 环境变量；缺省 Production）。
//   背景与修复见《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》§9.7。
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production（默认）"}");

// 显式加载静态 Web 资产清单（Build 模式而非 Publish 时需要）
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();


// Add service defaults & Aspire components.
builder.AddServiceDefaults();


// Add services to the container.
builder.Services.AddRazorComponents()    
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    options.RootComponents.RegisterCustomElsaStudioElements();
});



// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize to handle large workflows.
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

//builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
//builder.Services.AddMediatR(typeof(Program).Assembly);

// 注册 EventAggregator 服务
builder.Services.AddEventAggregator();

//添加ELSA流程引擎相关服务注册
builder.AddElsaServicesForWeb();

// 2026-10-29 P2：CJPlug 自有的 IAppBarService / DefaultAppBarService 已退役 ——
//   AppBar 注册统一走 CJCore 的 Singleton IAppBarService（由 AddCJCoreFramework 注册）。
//   同时注册两套会让模块往错误的集合里注册 AppBar 项（旧的是 Scoped，每个电路一份）。
builder.Services.ConfigModulePageServices();

// VNC/SSH 远程桌面 WebSocket 代理服务
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.VncWebSocketProxy>();
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.SshWebSocketProxy>();
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.CaptureWebSocketProxy>();

//添加插头前端集合包依赖
builder.Services.AddPlugsBundle();
builder.Services.AddXmlConfiguredServices();
builder.Services.AddXmlConfiguredExecuteServices();


//添加Radzen控件服务
builder.Services.AddRadzenComponents();


builder.Services.AddOutputCache();

//其他服务配置
builder.Services.ConfigOtherServices();

// ── CJCore 框架接入（2026-10-29《CJPlug 接入 CJCore》P1 接线 / P2 换壳） ──
builder.Services.AddCJCoreFramework(builder.Configuration, o =>
{
    o.ProductName            = "寸金插座平台";
    o.DefaultIconUrl         = "/plug2.png";
    // P2–P4 期间真拦截由 PlugGuardModule（localStorage 守卫）承担；
    // LoginPath 供框架在未登录访问受保护页时跳转（P5 起改为 /three-role/login）。
    o.LoginPath              = "/login";
    o.EnableLogDrawer        = true;    // 平台日志抽屉（Alt+L）
    o.EnableLogAppBarButton  = true;
    o.EnableThemeSwitcher    = true;
    // 贴近换壳前的观感（原 MainLayout 的 AppBar 紫 + 暗色紫蓝主色）
    o.Theme = new CJCore.Framework.Abstractions.ThemeOptions
    {
        Light = new() { AppbarBackground = "rgba(89, 74, 226, 1)", AppbarText = "#ffffff", Primary = "#594ae2" },
        Dark  = new() { AppbarBackground = "rgba(89, 74, 226, 1)", AppbarText = "#ffffff", Primary = "#7e6fff" },
    };
});

// ── P2 认证 + P5 前临时认证占位 ─────────────────────────────────────────────
// 必要性：CJCore 的端点（P6 /api/module-admin/*、P7 /api/system-config/*）都以
// RequireAuthorization 挂了授权元数据 —— 缺 AuthorizationMiddleware 时请求直接抛异常；
// 有中间件却没 scheme 时一律 401。详见 PlugTempAuthHandler 的类注释。
// ⚠ P5 删除：接入 ThreeRoleAdmin 后连带 PlugAnonymousAuthStateProvider 一起删。
builder.Services.AddAuthentication(CJ.Plug.HostWebServer.Shell.PlugTempAuthHandler.Scheme)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
               CJ.Plug.HostWebServer.Shell.PlugTempAuthHandler>(
        CJ.Plug.HostWebServer.Shell.PlugTempAuthHandler.Scheme, null);

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
                           CJ.Plug.HostWebServer.Shell.PlugTempAdminAuthStateProvider>();

// ── P7.1：通用配置页 /system-config（D8=B / Q10）─────────────────────────────
// ① 宿主持久化 Store 必须先注册 —— AddCJCoreSystemConfig 内部是 TryAdd，不会覆盖。
//    目标表沿用 CJPlug 现有的 SystemConfigItems（理由见 PlugSystemConfigStore 类注释）。
builder.Services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigStore,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigStore>();
builder.Services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigFieldProvider,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigFieldProvider>();
builder.Services.AddCJCoreSystemConfig(o =>
{
    // CJPlug 现行最高权限角色名（P5 换 ThreeRoleAdmin 后改为 system_admin）
    o.AccessRoles = ["Admin"];
});
// ② UI 层（页面 + 模块实例）。它的目的在于把 CJCore.Modules.SystemConfig.UI 程序集带进路由发现；
//    但它自带的菜单把分组写死成“系统”，与 D3=A（菜单归一「通用/管理/调试」）冲突 ⇒ 摘掉后换宿主自己的。
builder.Services.AddCJCoreSystemConfigUI();
foreach (var d in builder.Services
             .Where(d => d.ServiceType == typeof(CJCore.Framework.Abstractions.IMenuService)
                      && d.ImplementationType == typeof(CJCore.Modules.SystemConfig.UI.SystemConfigUiMenuService))
             .ToList())
{
    builder.Services.Remove(d);
}
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigMenuService>();

// ── P8：A2A 工具面（CJPlug 动态工具 → CJCore IToolRegistry）────────────────
// 注册表用框架自带的 DefaultToolRegistry：它天生支持「static 池（进程内注入）+ dynamic 池（运行时增删）」，
// 正好匹配 CJPlug 的工具来自「运行时发布的工作流」这一事实。
// ⚠ 消费方（CJCore.Agent 执行链 / 对外 MCP 面）尚未接入 CJPlug —— 本阶段交付的是**供给侧**，
//   验证口径是 IToolRegistry.AllEnabledDefinitions 能查到 CJPlug 的工具（见文档附录）。
// DefaultToolRegistry 先按具体类型注册：它持有 dynamic 池，工具同步服务要拿到具体类型才有增删改口。
builder.Services.AddSingleton<CJCore.AgentTool.MCPTools.DefaultToolRegistry>();
builder.Services.AddSingleton<CJCore.AgentTool.MCPTools.IToolRegistry>(
    sp => sp.GetRequiredService<CJCore.AgentTool.MCPTools.DefaultToolRegistry>());
builder.Services.AddHttpClient(CJ.Plug.HostWebServer.A2A.PlugMcpBridgeClientOptions.HttpClientName, client =>
{
    // McpServer 实际端口由配置给出（缺省 3001，与 CJ.Plug.AspireHost.AppHost 的 KestrelUrl 一致）
    client.BaseAddress = new Uri(
        builder.Configuration["PlugMcpServer:Url"]
        ?? CJ.Plug.HostWebServer.A2A.PlugMcpBridgeClientOptions.DefaultBaseAddress);
    client.Timeout = TimeSpan.FromSeconds(120);   // 工具执行是长任务（跑真实工位机）
});
builder.Services.AddHostedService<CJ.Plug.HostWebServer.A2A.PlugMcpToolSyncService>();

// ── P2：宿主壳模块（替代原 App_All / Routes / MainLayout / MyNavMenu / EmbeddedLayout）──
// 注册顺序 = FrameworkApp 中 head 资源的渲染顺序（顺序敏感项见 PlugHeadModule 注释）。
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugHeadModule>();
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugShellModule>();
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugRuntimeModule>();
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugGuardModule>();

// 原 MyNavMenu 里硬编码的三条入口（下载包 / VNC×2）改由菜单服务贡献
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.MainPageContent.Shell.Menus.PlugShellMenu>();

// 执行日志抽屉：AppBar 按钮与抽屉分处两棵渲染子树，用单例桥接
builder.Services.AddSingleton<CJ.Plug.MainPageContent.Shell.PlugExecLogService>();

// ── P6：模块目录与启停（/admin/modules）──
// ⚠ 与 P4 的关系：此刻 CJPlug 还没接 CJCore Data 底座，启停状态走「进程内存 + 中立交换文件」
//   （module-enablement.json），菜单/AppBar 的即时消失已可用，但重启后状态靠交换文件恢复；
//   P4 接上 Data 底座后同一行代码即升级为 MA_ 表持久化，无需改动。
// ⚠ 与 P5 的关系：AdminRoles 现在填的是 CJPlug 现有角色名；P5 换 ThreeRoleAdmin 后改为 "system_admin"。
builder.Services.AddCJCoreModuleAdmin(o =>
{
    o.AdminRoles = ["Admin"];
});
// Ⅱ ModuleAdmin 自带的菜单把分组写死成“系统”（同 P7.1 的 SystemConfig 情形）⇒ 摘掉后换宿主自己的。
//   冒烟实测：不摘的话菜单里会多出第 4 个分组「系统」，且该组下只有「模块管理」一条。
foreach (var d in builder.Services
             .Where(d => d.ServiceType == typeof(CJCore.Framework.Abstractions.IMenuService)
                      && d.ImplementationType == typeof(CJCore.Modules.ModuleAdmin.ModuleAdminUiMenuService))
             .ToList())
{
    builder.Services.Remove(d);
}
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.HostWebServer.SystemConfig.PlugModuleAdminMenuService>();



// 开启详细错误信息
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
});



var app = builder.Build();

// ── 根组件与模块路由发现：见下方 app.MapCJCoreRazorComponents()（含完整说明） ──

if (!app.Environment.IsDevelopment())
{
    Console.WriteLine("------------Production mode--------------");
    //app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseRouting();
app.UseWebSockets(); // WebSocket 支持 (VNC/SSH 远程桌面代理) — 必须在 endpoint 之前
// 2026-10-29 P2：CJCore 端点（P6 /api/module-admin/*、P7 /api/system-config/*）带 RequireAuthorization 元数据，
//   缺这两个中间件时请求会直接抛 InvalidOperationException / 一律 401。
//   P5 之前认证由 PlugTempAuthHandler（恒 Admin 的过渡件）提供。
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();    // Add this line
app.MapDefaultEndpoints();
//app.UseHttpsRedirection();

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

// 2026-10-29 P2（冒烟修正）：**必须**走框架提供的 MapCJCoreRazorComponents() ——
//   它内部做的就是 MapRazorComponents<FrameworkApp>() + AddAdditionalAssemblies(IModule 程序集)，
//   但**会排除根组件程序集 CJCore.Framework.UI**（FrameworkUiModule 所在程序集）。
//   ⚠ 手写这段样板（本项目上一版就是手写的）会让 Framework.UI 同时成为 AppAssembly 与
//     AdditionalAssemblies 成员 ⇒ 启动即抛 InvalidOperationException "Assembly already defined"，
//     且此异常发生在 UseAuthorization 构造 AuthorizationPolicyCache 时，编译期完全看不出来。
//   宿主程序集自身无 @page（已核实），MainPageContent 的 4 个 @page 由 PlugShellModule 带进。
//   Radzen 组件用 InteractiveAuto ⇒ 额外追加 WebAssembly 渲染模式（框架默认只加 Server）。
//   框架自带 NotFoundPage 的 catch-all（/{**path}）此时是**期望行为**：未匹配路由 → 404 页。
app.MapCJCoreRazorComponents()
    .AddInteractiveWebAssemblyRenderMode();

// VNC/SSH 远程桌面代理 API 端点
app.MapRemoteDesktopApi();

//app.UseAntiforgery();

app.Run();
