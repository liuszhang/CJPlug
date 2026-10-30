using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.HostWebServer.Shell;
using CJ.Plug.KnowledgeManage;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJCore.Framework.Api;
using CJCore.Modules.Data;
using CJCore.Modules.ModuleAdmin;
using CJCore.Modules.PersonalCenter;
using CJCore.Modules.SystemConfig;
using CJCore.Modules.SystemConfig.UI;
using CJCore.Modules.ThreeRoleAdmin;
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
        //    ⚠ 2026-10-30（P6.3 实测）补入两条前缀，均为「端点声明了 RequireAuthorization 但落到 ② 分支 ⇒ 恒 500」：
        //      · /api/permission —— PermissionAdminEndpoints 的 MapGroup("/api/permission")
        //      · /api/modules    —— ModuleAdminEndpoints 的 MapGroup("/api/modules")（注意不是 /api/module-admin）
        //    此前无人调用这两个 API 故未暴露：权限矩阵页走 @inject PermissionFunctionsService、
        //    模块管理页走 @inject DefaultModuleCatalog，都绕过了 HTTP，所以 UI 正常而上游 API 一直是坏的。
        if (path.StartsWithSegments("/api/modules")
            || path.StartsWithSegments("/api/module-admin")
            || path.StartsWithSegments("/api/system-config")
            || path.StartsWithSegments("/api/permission")
            // P5.5 第三批（2026-10-30）：/api/plug-agent/tools 由匿名改为 RequireModuleAccess("plug-agent:tools")。
            // 声明了授权元数据就必须走授权中间件，否则恒 500（同 /api/permission 的历史坑）。
            || path.StartsWithSegments("/api/plug-agent"))
            return true;

        // ①.5 P5：三员 / 个人中心端点同样带 RequireAuthorization 元数据。
        //    ⚠ 若落到下面的 ② 分支（无授权中间件），EndpointMiddleware 会抛
        //    "contains authorization metadata, but a middleware was not found that supports authorization"
        //    ⇒ 登录后打开三员页面 500（2026-10-29 P5 实测）。
        //    登录端点自身带 AllowAnonymous，经过授权中间件仍可匿名访问。
        if (path.StartsWithSegments("/api/three-role") || path.StartsWithSegments("/api/personal-center"))
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

        // ── P5.8 方案 C2 地基：内部信任密钥（2026-10-29）────────────────────
        // 背景：业务面 /api/* 全匿名 + 监听 http://*:5066 ⇒ 同网段任何主机可读写业务数据。
        //   但不能直接加 RequireAuthorization：内部调用方（MainApiClient、53 个 AddHttpClient、
        //   工位机跨进程、Elsa/MCP）全部零凭证，一加全站 401。
        // 本段只做"出站自动带凭证"这一半：
        //   ① Configure —— 从配置读密钥；为空则整套机制关闭（保留既有全匿名行为，零风险）；
        //   ② Filter —— 让 AddHttpClient 创建的全部客户端统一附加 X-Internal-Key。
        // 入站校验在 ApiServer 的 UseInternalApiKeyGate（默认 Observe 只统计不拦截）。
        // ⚠ 顺序：必须在任何 AddHttpClient 之前执行 Configure，否则首轮创建的客户端读到空密钥。
        //   虽然 Factory 是延迟创建的，但"先配后注册"不依赖这个时序假设。
        CJ.Plug.Models.Shared.InternalApiKey.Configure(configuration["InternalApi:Key"]);
        services.AddSingleton<Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter,
                              CJ.Plug.HostWebServer.Shell.InternalApiKeyHttpFilter>();

        services.AddMudServices();
        services.AddMudExtensions();

        // ── Blazor 三件套 ──────────────────────────────────────────────────
        // ⚠ P5（2026-10-29）：已移除 .AddInteractiveWebAssemblyComponents()。
        //   本仓没有 Blazor WASM 项目，此前唯一用 InteractiveAuto 的 RadzenComponents
        //   已改为 InteractiveServer。保留 WASM 渲染模式会让 Blazor 认为存在多种 rendermode，
        //   导致 CookieAuthStateProviderBase 的 RegisterOnPersisting（未显式指定 rendermode）
        //   无法推断 ⇒ 每个页面请求 500（详见 PlugRuntimeHost.razor 顶部注释）。
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

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
            // ⚠ P5 必须有：框架用它给所有模块 ApiClient 装配默认 BaseAddress
            //   （CJCoreFrameworkExtensions 里 ConfigureHttpClientDefaults → client.BaseAddress）。
            //   不设 ⇒ 模块 ApiClient 的 HttpClient.BaseAddress 为空 ⇒ 登录时直接抛
            //   "An invalid request URI was provided... BaseAddress must be set"，页面只显示红字、请求发不出去。
            //   单进程合并后就是本进程自己的地址，环回已由 LoopbackProxyBypass 排除系统代理。
            o.BaseUrl = GlobalData.MainApiServer;
            // P5 起：真认证由 ThreeRoleAdmin 承担，未登录访问受保护页跳到三员登录页。
            // （P2–P4 期间是 /login + PlugGuardModule 的 localStorage 守卫，两者均已移除）
            o.LoginPath = "/three-role/login";
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

        // ── P4：业务库接入 CJCore Data 底座（2026-10-29） ─────────────────
        // 目标（D5=C-2 的第一步）：18 个业务模块的表由 CJCore 的 DataDbContext + 自动迁移引擎接管，
        //   MainDbContext 收缩为「身份侧专用」（只承载 UserModuleDbConfig 注册的 7 张身份/权限表）。
        //
        // ⚠ 三条不可动的约束：
        //   ① 适配器必须 Singleton：SnapshotModelBuilder 是 Singleton 且注入 IEnumerable<IModuleDbConfig>，
        //      集合里出现任一 Scoped 元素会在容器校验期直接抛。
        //   ② 连接串与 MainDbContext 共用同一个文件（main.db）。不拆两个库，是为了不改变面向现场的路径口径。
        //   ③ 建表顺序：MainDbContext.EnsureCreated（身份表）必须先跑，再由本引擎建业务表 ——
        //      前者在 ApiServer/Program.cs 的 app.Run() 之前显式调用，后者是 HostedService（Run 时才启动）。
        //      反过来则 EnsureCreated 会因"库里已有表"直接 no-op，身份表永久缺失。
        var plugConnStr = CJ.Plug.Models.DbContexts.DbConnectionString.ConnectionString;
        var plugDbType = CJ.Plug.Models.DbContexts.DbConnectionString.DbType;
        // 与 MainDbContext 完全相同的绝对化处理（SQLite 的 Data Source= 相对路径按应用基目录解析）
        var plugAbsoluteConnStr = plugConnStr.Replace("Data Source=",
            $"Data Source={Path.Combine(AppContext.BaseDirectory, string.Empty)}");
        var plugDataProvider = (plugDbType ?? string.Empty)
            .Contains("postgre", StringComparison.OrdinalIgnoreCase)
            ? CJCore.Modules.Data.DataProvider.PostgreSql
            : CJCore.Modules.Data.DataProvider.Sqlite;

        // ── 业务模块 DbConfig 必须与 ApiServer 同集合（2026-10-30 修）─────────────────
        // 症状：本宿主只调了 ConfigModulePageServices()（页面/UI 服务），而 18 个业务
        //   IModuleBusinessDbConfig 的注册全在 ConfigModuleApiServices() 里 ⇒ 本宿主的
        //   DataDbContext 模型只剩 CJCore 框架自带的表（29 张），而 main.db 实际有 80 张
        //   （由 ApiServer 迁移建成，快照 __CjModelSnapshot 也是 80 张）。
        //   后果：自动迁移引擎按「模型 vs 实际库」diff，判定「模型整体回退」要删 51 张表
        //   ⇒ MASS-ARCHIVE-BLOCKED fail-fast ⇒ HostWebServer 直接起不来（Elsa 引擎宿主
        //   在 5066，起不来等于整条流程执行链不可用）。护栏拦住了删除，但服务是死的。
        //
        // 修法：本宿主也要把业务模块 DbConfig 注册进来（只注册服务，**不**调
        //   ConfigModuleApis() 映射端点 ⇒ 5066 不会因此多出业务 API），使模型与库/快照一致。
        //   注意：必须在 AddCJCoreDataApi 之前，迁移引擎建模时要能看到完整集合。
        //
        // ⚠ 2026-10-30 更正：本段**不在这里补**。合并形态（CJ.Plug.ApiServer 引用本类的
        //   AddPlugWebHost）下，业务 DbConfig 已由 ApiServer 侧的 ConfigModuleApiServices() 注册，
        //   这里再补一份会出现**重复注册**（实测 21 条 DbConfig 各打印两次 = 42 行）。
        //   只有「独立启动形态」（本项目的 Program.cs 薄壳）才需要补 —— 已挪到 Program.cs，
        //   而合并形态根本不会执行 Program.cs，两边互不干扰。

        services.AddSingleton<CJCore.Modules.Data.IModuleDbConfig,
                              CJ.Plug.Models.Services.BusinessModuleDbConfigAdapter>();

        services.AddCJCoreDataApi(o =>
        {
            o.ConnectionString = plugAbsoluteConnStr;
            o.Provider = plugDataProvider;
            // EF 传统迁移不启用：表结构由 CJCore 全自动迁移引擎接管。
            o.EnableMigrations = false;
            o.EnableAutoMigration = true;
            o.EnvironmentName = builder.Environment.EnvironmentName;
            // 种子改由引擎在「结构迁移完成之后」触发：原来在 Program.cs 里 app.Run() 前直接跑，
            // 那时业务表还没建（引擎是 HostedService，Run 才启动）⇒ 全新库首次启动必然失败。
            o.InitAction = (sp, ct) =>
            {
                // 传进来的可能是根容器，而种子提供者会解析 Scoped 服务 ⇒ 必须自建 scope
                //   （这正是原 SeedDataRunner 直接吃 app.Services 的既有缺陷）。
                using var seedScope = sp.CreateScope();
                return SeedDataRunner.RunAllAsync(seedScope.ServiceProvider, ct);
            };
        });

        // ── P5：审计独立库（AuditDbContext）──────────────────────────────
        // 三员的 AuditService / 审计页依赖 CJCore.Modules.Data.AuditDbContext，
        // 它与业务 DataDbContext 是**两个物理库**（SQLite 下按主库名派生 main_audit.db）；
        // 不注册 ⇒ 打开 /three-role/audit 直接抛
        // "Unable to resolve service for type 'CJCore.Modules.Data.AuditDbContext'"。
        // 该扩展同时把审计表纳入自动迁移引擎（MigrationTarget.Audit）。
        services.AddAuditDbContext(configuration);

        // ── P5：身份替换 —— 三员管理真认证（2026-10-29）─────────────────────
        // 一行全包：DataProtection（密钥落 DataProtectionKeys 表 ⇒ F5/重启保持登录）
        //   + TRA_* DbConfig（20 张表，走同一个 DataDbContext/迁移引擎）
        //   + 认证基座（PolicyScheme 双通道：Bearer→JWT / 否则 Cookie；真撤销）
        //   + Permission 统一鉴权中心（Profile=ThreeRoleAdmin ⇒ 策略② RoleDataMenu，
        //     默认管理角色 ["system_admin"]，故此处无需传参）+ PersonalCenter + 种子。
        //
        // ⚠ 三个必须一起理解的点：
        //   ① 本调用内部 AddCJCoreAuth 会改写全局 DefaultScheme 为 "ThreeRolePolicy"。
        //      因此原 P2–P4 的临时认证（PlugTempAuthHandler + PlugTempAdminAuthStateProvider）
        //      必须**同一次提交里**移除：只接后端不切组件层，会得到「端点层要三员 Cookie、
        //      组件层恒 Admin」的错乱态（PlugTempAdminAuthStateProvider 类注释记载过同类症状）。
        //   ② 种子 ThreeRoleAdminSeedDataProvider 仅在 TRA_SysUsers 为空时执行，会建
        //      admin / sysadmin / autadmin / audadmin 四个账号，密码取配置：
        //      ThreeRoleAdmin:AdminPassword（admin 用）/ ThreeRoleAdmin:DefaultPassword（其余，缺省 123456）。
        //      ⇒ 本方案（D-P5-1 = A）不迁移旧 Users 表，登录凭据由此处产生。
        //   ③ 默认密码策略 TRA_Pwd_MinLength=8 + 需大小写/数字，与缺省密码 123456 不匹配：
        //      LoginAsync 不校验策略（能登录），但首次改密会被挡 ⇒ 部署时必须显式设置合规密码。
        services.AddCJCoreThreeRoleAdmin();

        // ⚠ 兼容兜底：TRA 注册的 IPasswordHasher 只认 PBKDF2-HMAC-SHA256；
        //   旧 Identity 库里的哈希是 HMACSHA512（实测 PRF=2/100000 迭代）。
        //   本方案不迁移账号 ⇒ 不需要兼容层。若将来改走 D-P5-1 的 B 方案，
        //   在此处 AddScoped<IPasswordHasher, PlugLegacyCompatPasswordHasher>() 覆盖即可
        //   （AddCJCoreAuth 用的是 AddScoped 而非 TryAdd，后注册生效）。

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        // ── P7.1：通用配置页 /system-config ───────────────────────────────
        // ① 宿主持久化 Store 必须先注册 —— AddCJCoreSystemConfig 内部是 TryAdd，不会覆盖。
        //    P7.1a（2026-10-30）：内容已换 — 不再经旧配置模块的 HTTP 客户端，
        //    改为本进程直读写在 CJCore Data 底座（TRA_SysConfigs / Category="Plug"）。
        services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigStore,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigStore>();
        services.AddSingleton<CJCore.Modules.SystemConfig.Abstractions.ISystemConfigFieldProvider,
                              CJ.Plug.HostWebServer.SystemConfig.PlugSystemConfigFieldProvider>();
        services.AddCJCoreSystemConfig(o =>
        {
            // P5：三员体系的系统管理员角色码（原 P2–P4 临时体系的 "Admin" 已退役）
            o.AccessRoles = ["system_admin"];
        });
        // ①' P7.1a：远程查看模式的**读**侧入口（ToolExecuteService 消费）。
        //     旧配置模块退役后，原先打 /api/systemconfig/get/RemoteViewMode 的 HTTP 读法一并消失，
        //     由本实现在同进程读 CJCore 配置快照替代；未注册本服务的宿主按 fullscreen 兜底。
        services.AddSingleton<CJ.Plug.PlugBaseCore.Contracts.IRemoteViewModeProvider,
                              CJ.Plug.HostWebServer.SystemConfig.CJCoreRemoteViewModeProvider>();
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
        // ③ P5.5 第二批：权限键声明 —— 权限矩阵页靠它扫到 plug-agent 键
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration(
                "plug-agent", ["system_admin"], "工作流 Agent（调试）"));

        // ── P2：宿主壳模块（替代原 App_All / Routes / MainLayout / MyNavMenu / EmbeddedLayout）──
        // 注册顺序 = FrameworkApp 中 head 资源的渲染顺序（顺序敏感项见 PlugHeadModule 注释）。
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugHeadModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugShellModule>();
        services.AddSingleton<CJCore.Framework.Abstractions.IModule,
                              CJ.Plug.MainPageContent.Shell.PlugRuntimeModule>();
        // ⚠ P5 已移除 PlugGuardModule：它是 P2–P4 的临时守卫（注入 5 行脚本，靠
        //   localStorage.userid 判断并 location.replace('/login')）。真认证上线后，
        //   未登录由 Cookie 认证的 Challenge 直接导到 /three-role/login，再保留本模块
        //   会在登录页上空转跳转（login 页也被它拦）。

        // 原 MyNavMenu 里硬编码的三条入口（下载包 / VNC×2）改由菜单服务贡献
        services.AddSingleton<CJCore.Framework.Abstractions.IMenuService,
                              CJ.Plug.MainPageContent.Shell.Menus.PlugShellMenu>();

        // 执行日志抽屉：AppBar 按钮与抽屉分处两棵渲染子树，用单例桥接
        services.AddSingleton<CJ.Plug.MainPageContent.Shell.PlugExecLogService>();

        // ── P6：模块目录与启停（/admin/modules） ──────────────────────────
        // P4 之后 Data 底座已就位（见上文 AddCJCoreDataApi），启停状态可直接落 MA_ 表；
        //   注册顺序无所谓（ModuleAdmin 用 TryAdd / 运行时解析），但 Data 在前保证迁移引擎先建表。
        //   注：首次切换到 Data 底座时，旧的中立交换文件 module-enablement.json 不再被读取，
        //   启停状态回落到「模块默认值」——可接受（无历史数据口径）。
        services.AddCJCoreModuleAdmin(o =>
        {
            // P5：与 SystemConfig 同口径（原临时体系的 "Admin" 已退役）
            o.AdminRoles = ["system_admin"];
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

        // ── P5.6 已删除本段「抢回默认认证 scheme」的过渡修法 ──────────────────
        // 原问题：LoginModule 的 AddLoginModuleApiServices() 里的 services.AddIdentity<User, UserRole>()
        //   调 AddAuthentication 把 DefaultScheme 设成 "Identity.Application"，且注册在
        //   AddCJCoreThreeRoleAdmin() 之后 ⇒ 顺序上它赢 ⇒ 所有请求恒「未登录」。
        // 现状：LoginModule 四项目 + UserManageModule + AuthModule 已物理删除，
        //   AddIdentity 调用链整体消失（唯一残留入口在 ApiServer/Program.cs，已同步移除）
        //   ⇒ 默认 scheme 由 AddCJCoreAuth 的 CJCoreAuthOptions 唯一决定，无需再抢。
        // 若将来又出现「恒未登录」，先查是否有模块重新引入 AddIdentity/AddAuthentication。

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

        // ===== Blazor 框架资产显式端点（2026-10-30 故障修复） =====
        // 病因：本宿主的 UseStaticFiles() 注册在 UseRouting() **之后**（Program.cs 的 app.UseRouting()
        //   早于本方法的调用），实测物理 wwwroot 一律取不到——新建一个 wwwroot/__probe.txt 请求回来
        //   也是首页 HTML；而 wwwroot/_framework 里的资产**不在** MapStaticAssets 的清单里
        //   （Debug/Release 两份 staticwebassets.endpoints.json 的 _framework 条目数均为 0）。
        //   两头都不管 ⇒ GET /_framework/blazor.web.js 落到 Blazor fallback 返回首页 HTML
        //   ⇒ 浏览器报 "Unexpected token '<'"、window.Blazor 始终 undefined
        //   ⇒ 整站 **按钮有外观、无事件**（三员登录页点「登录」完全没反应即此故障）。
        // 处置（本次选定：不动既有中间件顺序，零副作用）：把 Blazor 运行时真正用到的几个文件
        //   用**端点**直接吐出去，白名单 + 防目录穿越；文件源按 内容根 wwwroot → 基目录 wwwroot 两级兜底。
        // 技术债：根治应把静态中间件整体挪到 UseRouting() 之前（CJOntology.Web 就是该顺序且实测正常），
        //   但那要回归认证 / 防伪造 / 网关的中间件事前依赖，另行立项。
        app.MapBlazorFrameworkAssets();

        // P5：身份与权限端点 —— MapCJCoreThreeRoleAdmin() 一行映射 10 组：
        //   认证基座（api/three-role/account: login/logout/cookie/jwt/refresh）
        //   + me/会话/改密/重置密码 + 用户/角色/部门/用户组 + 角色权限配置
        //   + 审批/审计/元数据 + Permission 矩阵管理 API。
        // ⚠ 与 AddCJCoreThreeRoleAdmin 成对出现：少一边会让 /three-role/* 页面 404 或 API 401。
        app.MapCJCoreThreeRoleAdmin();
        // P5：个人中心端点（AddCJCoreThreeRoleAdmin 内部已注册 PersonalCenter 服务，
        //   但端点映射要宿主自己调 —— 少了它，AppBar 头像菜单里的个人中心/改密页会 404）。
        app.MapCJCorePersonalCenter();

        // P6：模块管理端点（/admin/modules）+ 管理 API
        app.MapCJCoreModuleAdmin();
        // P7.1：系统配置端点（GET/PUT/POST /api/system-config）
        app.MapCJCoreSystemConfig();

        // P8.3.5：Agent 工具面只读端点 —— 与调试页同源（IToolRegistry.AllEnabledDefinitions），
        // 便于用 curl 验收"工具真的进了注册表"，不必开浏览器。
        // ⚠ 只读：返回名称/描述/参数 schema，不提供执行入口。
        //
        // ⚠ P5.5 第三批（2026-10-30）：本端点此前**完全匿名**。它虽只读，但会吐出全部启用工具的
        //   名称 + 描述 + 完整参数 schema —— 等于把 Agent 的能力面摊开，是枚举攻击的第一步。
        //   收口方式与页面/菜单同键（`plug-agent` 的派生数据权限键 `plug-agent:tools`）：
        //     · pairedWith 声明"本接口是 plug-agent 页面的运行必需依赖"，矩阵里勾了页面没勾接口会提醒；
        //     · AdminRoles 硬保护（ModuleAccessStrategy 0.2）⇒ system_admin 恒定放行。
        //   ⚠ 端点声明了 RequireAuthorization 元数据 ⇒ 必须同时进 NeedsIdentity() 的 ① 分支，
        //     否则落到 ② 分支缺 AuthorizationMiddleware，EndRequest 直接抛 ⇒ 恒 500（P6.3 同类坑）。
        app.MapGet("/api/plug-agent/tools", (CJ.Plug.HostWebServer.Agent.PlugWorkflowAgent agent) =>
            agent.GetVisibleTools()
                 .Select(t => new { t.Name, t.Description, t.ParametersJson })
                 .ToList())
           .WithName("PlugAgentTools")
           .WithTags("P8-Agent")
           .RequireModuleAccess(
               "plug-agent:tools",
               roles: ["system_admin"],
               moduleName: "工作流 Agent（调试）",
               displayName: "读取 Agent 工具清单",
               pairedWith: ["plug-agent"]);

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
        // ⚠ P5：原此处追加 .AddInteractiveWebAssemblyRenderMode()（为 Radzen 的 InteractiveAuto）。
        //   本仓无 WASM 项目，且该追加会让 rendermode 变成多种 ⇒ 认证态持久化无法推断 rendermode
        //   ⇒ 全站 500。已随 PlugRuntimeHost 改用 InteractiveServer 一并移除。
        app.MapCJCoreRazorComponents();

        return app;
    }
}
