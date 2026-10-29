# （设计中）CJPlug 接入 CJCore 实施计划

> 状态：设计中（待评审）　|　日期：2026-10-29　|　作者：旺财　|　位置：`CJPlug/docs/`
> 替代文档：`CJCore/docs/CJPlug-接入CJCore框架迁移计划.md`（v2.0 / 2026-07-07，已过期，见 §1.6）
> 关联文档（均在 `CJCore/docs/`）：`CJOntology-接入方案-v2.md`、`（已完成）模块配置管理界面方案.md`、`（已完成）CJCore-统一根组件FrameworkApp与模块资源贡献方案.md`、`CJCore-模块角色管理配置ModuleAccess实施方案.md`、`CJCore-登录认证统一架构方案.md`
> 本文档全部路径以**仓库根** `D:\Pro\CJ.Plug.Github` 为基准。

---

## 0. 结论先行（TL;DR）

1. **接入度当前≈0**。CJPlug 165 个项目里只有 **7 个模块**引用了 CJCore，且**只用了一个东西**——`DisplayTime` 统一时刻门面。没有任何项目引用 `CJCore.Framework.Api` / `CJCore.Framework.UI`，没有一处 `AddCJCoreFramework()` 调用。两侧目前是"两个独立产品"，不是"内核 + 产品"。
2. **最短可上线路径 = P0–P2（7.5–10.5 人日）**：只统一"宿主壳 + 模块契约 + 菜单/AppBar"，不碰数据与身份。走通即得：统一布局/主题/日志抽屉/快捷键、菜单 3 组、嵌入形态。**但评审认为这不是终态**——CJPlug 的 `EnsureCreated` 不会演进 schema，越晚迁移越贵，因此决定继续做到 M5（身份/数据/权限归一）。
3. **三个硬阻断（已逐一拍板，见 §4）**：
   - **认证**：CJPlug 是 ASP.NET Identity + **localStorage 里的 userid**（`DomInteropService`），UI 侧 **0 个 `[Authorize]`**，API 侧 `AddAuthentication()` **空注册**、无 `UseAuthorization()`；CJCore 是 Cookie/JWT 统一基座 + `ModuleAccess` 策略路由。
   - **数据**：CJPlug 用 `EnsureCreated` + 代码里手写 `ALTER TABLE`（`PlugManageService.cs:289`），2026-01 的 3 个 EF 迁移已废弃；CJCore 是自动迁移引擎 + `IModuleDbConfig` 并入共享 `DataDbContext`。**两边 `IModuleDbConfig` 同名不同契约**（CJPlug 41 个实现 / CJCore 1 个）。
   - **嵌入布局**：桌面端 `CJ.Plug.Desktop` 依赖 `?hideMenu=true` 复用 9+ 条路由，框架当前**无此能力**（`EmbedLayout` 是 iframe 专用无壳布局，不是同一件事）。
4. **已定路线（评审进行中）**：`D1=壳内嵌(A)` / `D2=A1 直接替换` / `D3=CoreMenu + 3 组` / **`D4=C` 换 ThreeRoleAdmin** / **`D5=C-2` 单库单上下文** / **`D9=A` 权限一并换 `Permission` 矩阵**。
   ⚠ **与原建议的偏离**：初稿建议"先表现层、后身份与数据"（D4=B 认证桥 + D5=A 双底座，14–20 人日）；评审在 D4 处改为"身份、数据、权限一并归一"，战线抬到 **43.5–67.5 人日**，且 P4/P7 必须重排到数据归一之后。反转点与理由是：CJPlug 的 `EnsureCreated` 无法演进 schema，拖得越久迁移成本越高。
5. **工作量**：按已定路线 **43.5–67.5 人日**；若退回"认证桥 + 双底座 + 保留旧权限"（B/B/B）则为 13.5–19.5 人日。**不含** Elsa、插件执行链、Aspire 编排、StationAgent、PlugToolIntegrations——本计划刻意不碰。
6. **本计划只写方案，不动代码**。每个阶段的出口判据由你执行构建与冒烟确认（遵循"只编码、不构建不运行"铁律）。

---

## 1. 调研结论（证据）

### 1.1 两侧定位与规模

| 维度 | CJPlug | CJCore |
|---|---|---|
| 定位 | 插座平台：任意工具 → 插件 → MCP Tool（执行层） | 共享内核：框架/模块/鉴权/LLM/DSL（基础设施层） |
| 解决方案 | `CJ.Plug-Aspire.sln` | `CJCore.sln` |
| 项目数 | 165 个 csproj | 149 个 csproj |
| 页面 | 515 个 `.razor` | — |
| 模块目录 | `src/modules/` 32 个 | `src/Modules/` 30 个 |
| 宿主形态 | Aspire 编排 8 个进程 + 1 个 WebView2 桌面端 | 单进程 Web（CJOntology/CJOEM 等产品） |
| 目标框架 | net10.0 / net10.0-windows | net10.0 |
| 包版本 | **工作区根 `Directory.Packages.props` 统一**（MudBlazor 9.11.0 / EF 10.0.12 / Radzen 12.0.6） | 同上 |

> **利好**：包版本是**工作区单文件中央管理**，不存在 MudBlazor / EF 版本战。这是本次接入最大的先天减风险项。

### 1.2 接入现状（全量证据）

| # | 事实 | 证据 |
|---|---|---|
| 1 | 仅 7 个 CJPlug 项目引用 CJCore，且只引用 `CJCore.Framework.Abstractions` | `modules/{AuthModule/CJ.Plug.AuthUI, JobManageModule/CJ.Plug.JobManage, KnowledgeManageModule/CJ.Plug.KnowledgeManage, LicenseModule/CJ.Plug.LicenseUI, MCPToolsModule/CJ.Plug.MCPToolsManage, SkillsModule/CJ.Plug.SkillsManage, UserManageModule/CJ.Plug.UserManage}/*.csproj` |
| 2 | 该引用的唯一用途是 `DisplayTime`（展示时区门面） | 各 csproj 上一行注释：`<!-- 展示时刻门面 DisplayTime（2026-10-05《CJCore 统一时刻口径与展示时区方案》D1/D2） -->` |
| 3 | 无任何项目引用 `CJCore.Framework.Api` / `CJCore.Framework.UI` | `grep CJCore --include=*.csproj` 全量 14 处命中，全是 `Framework.Abstractions` |
| 4 | 无 `AddCJCoreFramework()` / `MapCJCoreRazorComponents()` 调用 | 全仓 grep 0 命中 |
| 5 | CJPlug 自有同名契约 6 个 | `CJ.Plug.Models/Contracts/{IModule,IMenuService,IAppBarService,IModuleDbConfig,ISeedDataProvider,IFunctionPermissionProvider}.cs`、`Abstractions/ModuleBase.cs` |
| 6 | 模块实现类 **29 个**（26 继承 `ModuleBase` + 3 直接实现 `IModule`） | 例：`AuditModule : IModule`（`AuditUI/Menus/AuditManageMenu.cs:26`）、`AIModule(IAppBarService) : ModuleBase`（主构造函数形态） |
| 7 | `IModule` 注册 49 处，其中大量是 **Scoped** | `AddScoped<IModule, HomePageModule>()`（`HomePageServiceCollection.cs:16`） |
| 8 | 只有 2 个模块重写 `InitializeAsync`，且都只注册 AppBar 项 | `AIModule/ServiceCollectionExtensions.cs:22`（AskAI）、`LoginModule/LoginPages/LoginModule.cs:15`（LoginState） |
| 9 | AppBar 服务注册为 **Scoped** | `WebHost/CJ.Plug.HostWebServer/Program.cs:91` |
| 10 | 菜单实现 20 个，`MenuItem` 类型仅 24 个文件触碰 | `modules/**/Menus/*.cs` + `MyNavMenu.razor` |
| 11 | 宿主启动期**二次建容器**做模块初始化 | `HostWebServer/Program.cs:126-134`（`builder.Services.BuildServiceProvider()`） |
| 12 | 路由映射手写样板 | `HostWebServer/Program.cs:185-188`（`MapRazorComponents<App_All>()` + Server + WASM + `AddAdditionalAssemblies`） |
| 13 | UI 宿主无任何服务端鉴权 | 515 个 razor 中 `[Authorize]` 命中 **0**；`HostWebServer/Program.cs:147-148` 的 `UseAuthentication`/`UseAuthorization` **两行都被注释** |
| 14 | API 侧认证为空壳 | `PlugApiServer/CJ.Plug.ApiServer/Program.cs:147` `AddAuthentication()` 无 scheme；`:213` `UseAuthentication()` 但**无 `UseAuthorization()`** |
| 15 | 登录态存 localStorage | `Core/CJ.Plug.SharedPages/Services/DomInteropService.cs:20`（读 `userid`） |
| 16 | 数据库用 `EnsureCreated`，无迁移演进 | `ApiServer/Program.cs:182`；`Migrations/MainDb` 仅 3 个 2026-01 迁移；`PlugManageService.cs:289` 手写 `ALTER TABLE Plugs ADD COLUMN SortOrder` |
| 17 | 桌面端强依赖 `?hideMenu=true` | `WebHost/CJ.Plug.Desktop/ViewModels/MainViewModel.cs` 9 处 + `MainWindow.xaml.cs:56-69` 路由补参 |
| 18 | Radzen 仍在使用，但面很窄 | 22 个 razor 含 `<Radzen` |
| 19 | 2 个布局内渲染 `RadzenComponents @rendermode="InteractiveAuto"` | `MainLayout.razor:52`、`EmbeddedLayout.razor:36` |

### 1.3 CJCore 侧能力清单（本计划会用到的）

| 能力 | 入口 / 类型 | 说明 |
|---|---|---|
| 框架总装配 | `AddCJCoreFramework(IConfiguration, Action<FrameworkOptions>)`（`CJCore.Framework.Api/CJCoreFrameworkExtensions.cs:26`） | 日志 + AppBar + 模块发现 + ApiClient 默认地址 + 审计 + 权限默认引擎 + 静态资源自检 |
| 根组件 | `FrameworkApp.razor` | 收编产品 `App.razor`；模块经 `IAppHeadService` 贡献 CSS/JS |
| 路由 | `FrameworkRoutes.razor` + `MapCJCoreRazorComponents()` | 自动收集 `IEnumerable<IModule>` 程序集做路由发现 |
| 路由授权 | `ModuleAccessRouteView.cs` | `[ModuleAccess]` + `[Authorize]`；**无认证体系时 `[ModuleAccess]` 页面匿名放行**（`IAuthPresenceDetector`） |
| 布局 | `FrameworkLayout.razor` | CoreMenu / AppBar（带 AccessKey 门控）/ 主题切换 / LogDrawer / Alt+M、Alt+L |
| 菜单 | `IMenuService` + `MenuItem` + `CoreMenu.razor` | 按 `GroupName` 分组、按 `Roles`/`AccessKey` 过滤 |
| AppBar | `IAppBarService`（**Singleton**） | `AddAppBarItem<T>(accessKey)` + `AddAppBarDrawer<T>()` |
| 模块元数据 | `ModuleBase` 实现 `IModuleDescriptor` | ModuleId / Version / DependsOn / IsMandatory / EnabledByDefault / Order / Group |
| 模块启停 | `CJCore.Modules.ModuleAdmin` | `IModuleCatalog` / `IModuleStore` / `/admin/modules`；未启用 Data 时降级进程内存储 |
| 权限键 | `ModuleAccessAttribute` / `IModuleAccessDeclaration` / `ModuleAccessOptions` | 菜单/页面/API 三层按同一个 AccessKey 统一授权 |
| 统一认证 | `AddCJCoreAuth(Action<CJCoreAuthOptions>)` | PolicyScheme（Bearer→JWT / 否则 Cookie）+ 6 个扩展点：`ITokenKeyProvider` / `IAuthClaimsBuilder` / `IAuthSessionStore` / `IAuthUserResolver` / `IAuthLoginHandler` / `IAuthCookieClaimsFactory` |
| A2A 工具面 | `CJCore.AgentTool.MCPTools`（`IToolRegistry` / `IToolExecutor` / `McpSurface`） | 内部面 / 对外面分离，可让 MCP Tool 进入 A2A 中枢 |

### 1.4 CJPlug 宿主拓扑（现状，本计划只改标 ★ 的两处）

```
CJ.Plug.AspireHost.AppHost（用 AddExecutable 拉起 8 个进程，AddProject 全被注释）
├── HostWebServer:5066        ★ Blazor UI 宿主（本次接入主战场）
├── ApiServer:8687            ● 业务 API（P3 起可选挂鉴权，其余不动）
├── DispatchServer:8686       — SignalR 中枢，不动
├── ElsaApiServer:5001        — 工作流引擎，不动
├── ElsaStudio:5010           — 流程设计器，不动
├── McpServer:3001            ● MCP 协议服务（P8 对接 A2A 工具面）
├── StationApiServer:7660     — 工作站，不动
└── mcpInspector:6274         — 调试工具，不动

CJ.Plug.Desktop（WPF + WebView2）  ★ 依赖 ?hideMenu=true，受 P2 布局改造影响
CJ.Plug.MainPageContent           ★ App_All/Routes/MainLayout/MyNavMenu 全部在本项目，P2 删除
```

### 1.5 三个硬阻断的证据与含义

| 阻断 | CJPlug 现状 | CJCore 期望 | 直接替换的后果 |
|---|---|---|---|
| **认证** | Identity 用户表（`IdentityDbContext<User,UserRole,int>`）+ 登录页写 localStorage + 无服务端校验 | Cookie/JWT + `ClaimsPrincipal` + `[ModuleAccess]` | 一换就动用户表与会话；且当前**没有可用的 Principal**，`ModuleAccessRouteView` 会走"无认证体系匿名放行"分支，等于换了壳但没换守卫 |
| **数据** | `EnsureCreated` + 手写 ALTER；41 个 `IModuleDbConfig` 实现注册 DbSet | 自动迁移引擎 + `DataDbContext` | 两套建表引擎指向同一 SQLite 文件 → `EnsureCreated` 建的库没有迁移历史，自动迁移引擎会重新建表/误判 |
| **嵌入布局** | `?hideMenu=true` → 切 `EmbeddedLayout`（去侧栏、留 AppBar） | 无此开关 | 桌面端 9+ 入口的"无侧栏"形态丢失，且 `EmbedLayout` 是 iframe 无壳布局，不能顶替 |

### 1.6 既有 v2 计划文档的失效点（逐条）

| v2 断言（2026-07-07） | 现状 | 结论 |
|---|---|---|
| 「阶段五：LLM 模块接入 0.5 天」 | `AddCJCoreLLM` 内部会调 `AddCJCoreData()`，**拖入整套 CJCore 数据底座** | 已失效，降级为 P7 独立评估 |
| 「扩展 FrameworkRoutes 支持 `CascadingAuthenticationState`」 | 框架已内置（`FrameworkRoutes.razor:17`） | 已不需要 |
| 「扩展 FrameworkLayout 支持动态布局」 | 未实现，且不是"扩展 FrameworkLayout"，正确做法是布局内加 query 开关 | 需重写（见 D7） |
| 「删除 CJPlug 的 IModule / IMenuService / ModuleBase」 | 方向对，但未评估 49 处注册 / 29 个模块类 / Scoped→Singleton 的生命周期影响 | 需补齐（见 D2） |
| 「时间估算 5–6 天」 | 未计入认证、数据、hideMenu、模块开关、权限门 | 严重低估，本计划重估为 13.5–19.5 人日（最小闭环） |
| 未提及 | `IModuleDbConfig` / `ISeedDataProvider` / `IFunctionPermissionProvider` / `MainApiClient` **四组同名类型** | 新增附录 B（命名冲突登记） |

---

## 2. 目标与非目标

### 2.1 三层目标（分层交付，不追求一次到位）

| 层 | 目标 | 交付判据 | 阶段 |
|---|---|---|---|
| **L1 表现层统一** | CJPlug 用 CJCore 的根组件/路由/布局/菜单/AppBar/主题/日志抽屉 | 视觉与交互与 CJOntology 同构；桌面端 `?hideMenu=true&title=…` 仍可用 | P1–P2 |
| **L2 模块治理统一** | 模块元数据、启停开关、功能开关（AccessKey）三层打通 | `/admin/modules` 能列出 CJPlug 全部模块（约 29 个，P6 开工时点准）并实时启停菜单/AppBar | P6 |
| **L3 身份与数据统一** | 真 Principal、服务端鉴权、数据底座收敛、能力模块替换 | 未登录跳转、页面/API 拒绝越权、系统配置/许可证单源 | P3–P5、P7 |

### 2.2 明确的非目标（本计划不承诺）

1. **Elsa 工作流引擎**、`ElsaStudio`、`ElsaApiServer` 一律不动。
2. **插件执行链**（`PlugExecutionEngine` / `PlugExecuteHandlerService` / `BasePlugExecuteService` / `PlugLibrary` 20+ 插件）不动。
3. **Aspire 编排**（8 服务拓扑、`AddExecutable` 启动方式、端口规划）不动。
4. **StationAgent / StationApiServer / VNC / Guacamole** 不动。
5. **PlugToolIntegrations**（NX/Word/PythonSDK/ToolMng 等独立可执行）不动。
6. **CJPlug 的 MCP 服务实现**不动（P8 只做"注册进 A2A 工具面"，不改协议栈）。
7. 不做"两套壳并行 + 灰度"（见 D1 否决 B/C）。

---

## 3. 目标架构

### 3.1 接入后的装配视图

```
CJ.Plug.HostWebServer（唯一 UI 进程）
│
├─ Program.cs（装配）
│   ├─ builder.AddCJCoreStartupLogging()                  ← P2（替代裸 Serilog 配置）
│   ├─ builder.Services.AddCJCoreFramework(config, o => {…})   ← P1/P2 核心一行
│   ├─ builder.Services.AddCJCoreData(o => {…})                ← P4（唯一上下文）
│   ├─ builder.Services.AddCJCoreThreeRoleAdmin()              ← P5（认证+用户/角色/部门/组）
│   ├─ builder.Services.AddCJCorePermission()                  ← P5（权限矩阵）
│   ├─ builder.Services.AddCJCoreModuleAdmin()                 ← P6
│   ├─ builder.Services.AddPlugsBundle()                       ← 原样保留
│   ├─ builder.Services.ConfigModulePageServices()             ← 原样保留
│   └─ builder.Services.AddSingleton<IModule, PlugShellModule>()  ← 新增（宿主程序集路由兜底）
│
├─ FrameworkApp（CJCore）
│   ├─ <head> MudBlazor + ImportMap + IAppHeadService 贡献（CJPlug 的 Monaco/Amis/Z.Blazor/xterm/noVNC）
│   └─ FrameworkRoutes（AppAssembly=入口程序集，AdditionalAssemblies=全部模块程序集）
│        └─ ModuleAccessRouteView（认证 + [ModuleAccess] 拦截）
│             └─ FrameworkLayout
│                  ├─ CoreMenu（20 个 IMenuService 聚合，按 GroupName 分组）
│                  ├─ AppBar（AskAI / LoginState + ThemeSwitcher + LogDrawer 按钮）
│                  ├─ 模块注册的 MudDrawer（CJPlug 的执行日志抽屉）
│                  └─ LogDrawer（平台日志，Serilog 内存 Sink）
│
└─ 业务页面（各 modules/*/Pages/*.razor，注册方式不变，仅菜单/AppBar 契约变）

依赖方向铁律：
    CJCore ──► 无反向依赖 ──  CJPlug
    （CJCore 任何项目都不得引用 CJPlug；需要"产品特异性"时一律走框架扩展点）
```

### 3.2 能力归属矩阵（谁保留、谁替换）

| 能力 | CJPlug 现状 | CJCore 对应 | 处置 | 阶段 |
|---|---|---|---|---|
| 根组件 / 路由 / 布局 | `App_All` + `Routes` + `MainLayout` + `MyNavMenu` | `FrameworkApp` / `FrameworkRoutes` / `FrameworkLayout` / `CoreMenu` | **替换** | P2 |
| 模块契约 | `CJ.Plug.Models.Contracts.IModule` | `CJCore.Framework.Abstractions.IModule` | **替换** | P1 |
| 模块元数据 / 启停 | 无 | `IModuleDescriptor` + `ModuleAdmin` | **新增** | P6 |
| 功能权限声明 | `IFunctionPermissionProvider`（自建） | 同名接口，字段为超集（`ParentKey`/`Kind`/`Category`） | **合并** | P5 |
| 角色权限矩阵 | `RolePermissionService`（自建） | `CJCore.Modules.Permission` 矩阵 | **替换** | P5 |
| 认证 | Identity + localStorage | `AddCJCoreAuth` + `ThreeRoleAdmin` | **替换（弃 Identity）** | P5 |
| 系统配置 | `CJ.Plug.SystemConfig` | `CJCore.Modules.SystemConfig` | **替换** | P7 |
| LLM 配置 / 客户端 | `CJ.Plug.LlmConfig` + `DeepSeekService`（含 MCP 工具调用） | `CJCore.Modules.LLM`（无 tools 字段） | **本期不动**（内核缺 tools，见 D8） | — |
| 许可证 | `CJ.Plug.License` | `CJCore.Modules.License` | **替换**（先出差异对照表） | P7 |
| 日志抽屉 | `LogsWrapper`（SignalR 执行日志） | `LogDrawer`（Serilog 内存） | **并存**（语义不同） | P2 |
| 数据访问 | `MainDbContext`（Identity）+ `EnsureCreated` | `DataDbContext` + 自动迁移 | **归一（删 MainDbContext）** | P3–P5 |
| MCP 工具面 | `McpServer` + `DynamicToolRegistry` | `AgentTool.MCPTools.IToolRegistry` | **对接** | P8 |
| 工作流 / 插件执行 / 工作站 | Elsa / PlugExecutionEngine / Station | 无 | **不动** | — |

---

## 4. 决策点（D1–D9）

> 评审纪律：**一次只定一个**，D1 拍板后才动 P1 的代码。每个决策点给出建议值与"如果不选推荐值"的代价。

### D1 接入形态：壳内嵌 / 进程外插件 / 双壳并行

| 选项 | 内容 | 优点 | 代价 |
|---|---|---|---|
| **A（推荐）** | CJPlug 的 `HostWebServer` 直接引用 `CJCore.Framework.Api` + `.UI`，替换 `App_All`/`Routes`/`MainLayout` | 与 CJOntology 同构；单进程零 IPC；可回滚（删引用即退回） | 需处理 §1.5 三个硬阻断 |
| B | 用 `CJCore.ModulePlugin.Host` 把 CJPlug 模块打成插件包，按目录装载 | 版本隔离、可外部分发 | 该宿主面向"外部分发插件包"（`IModulePlugin` + 契约版本 + 六阶段装配），不解决 Aspire 多进程/Elsa/Identity；改造量远大于收益 |
| C | 保留旧壳，新增 `/v2` 路由并行 | 最安全 | 双源（违反"DSL 单源"同类铁律）；菜单/权限要维护两份；长期技术债 |

**建议：A。** 判据：CJPlug 的"插件"是**数据**（`PlugConfig/UserPlugs.xml` + 插件库），不是"代码包"；`ModulePlugin.Host` 的契约假设（每包一个 `IModulePlugin`、包目录、`ContractVersion`）与 CJPlug 的插件语义不同，套用会造成概念混淆。

> **评审结论（2026-10-29）：A 已定。** 本次只动 `HostWebServer` 一个进程；`CJ.Plug.Desktop` 不改代码、只做 `?hideMenu=true` 回归；其余 7 个进程零改动。

**验收判据**：`HostWebServer` 启动后 `/` 渲染出 `FrameworkLayout`；旧 `MainLayout` 不再被任何页面引用。

---

### D2 模块契约归属：直接替换 / 适配桥 / 双契约

| 选项 | 内容 | 代价 |
|---|---|---|
| **A（推荐）** | 删除 CJPlug 的 `IModule`/`ModuleBase`，29 个模块类改用 `CJCore.Framework.Abstractions.ModuleBase`（补 `Name`）；**P1 一律不改 DI 生命周期**，改 Singleton 推迟到 P2 | 改 29 个类 + 约 30 个 `using`；`Name` 要统一（建议中文名，P4 直接复用为显示名） |
| B | 写一个 `PlugModuleBridge : CJCore.IModule` 包装 `IEnumerable<CJPlug.IModule>` | 只改 1 个文件，但桥必须暴露 `Name`（拿不到）→ 只能按程序集名编造；且桥是临时层，后续仍要拆 |
| C | 双契约并存（新模块用 CJCore，旧模块留 CJPlug） | 路由发现要合并两条链；`ModuleAdmin` 只认识 CJCore 那半，模块清单永久缺一半 |

**建议：A1 直接替换（✅ 评审已定，2026-10-29）**，同一阶段内分两个 commit，不引入投影层：

1. **commit 1（纯机械，编译期可验证）**：29 个模块类换基类 + 补 `Name`；约 30 个 `ServiceCollectionExtensions.cs` 换 `using`；删本地 `IModule` / `ModuleBase`；菜单类型跟着换（见 P1 表 1.5）。
2. **commit 2（接线，运行期可验证）**：`Routes.razor` / `Program.cs` 的类型来源换成 CJCore；`AddCJCoreFramework` 接线；拆掉二次建容器。

理由：29 个类里 27 个是空壳，投影层唯一作用是"把编译错误推迟到运行时"，同时把 `Name` 这个**会长期存在的元数据**做成临时值（程序集短名），P4 补元数据时必然重写。拆两个 commit 已经拿到了"回滚粒度"，不需要再加一层脚手架。

> **评审补充（2026-10-29）**：A2（先投影后替换）已否决。否决理由同上：投影层的 `Name` 是注定被推翻的临时设计，且它把编译期风险转成了运行期风险。

**生命周期：P1 一律不动，P2 再动（顺序错了会当场抛异常）**

- `AIModule(IAppBarService appBarService)` 与 `LoginModule(IAppBarService appBarService)` 注入的是 **CJPlug 自己的 AppBar 服务（Scoped）**。如果在 P1 就把这两个模块注册成 Singleton，MS DI 会直接抛 *"Cannot consume scoped service 'IAppBarService' from singleton 'IModule'"*——**P1 必须保持 Scoped 不动**。
- P2 的 2.7/2.8 把 AppBar 换成框架的 **Singleton** 实现之后，这两个模块的依赖才变成 Singleton，此时（新增步骤 2.13）再把 29 个模块统一改 Singleton 才是安全的。
- 改 Singleton 的目的不是"性能"，而是：`FrameworkApp` 用**进程级静态标志**只跑一次 `InitializeAsync`，若模块仍是 Scoped，那唯一一次初始化用的是**首个电路 scope** 的实例，后续所有电路看到的都是"别人 scope 里的对象"——空壳模块无害，但这是定时炸弹。
- 菜单**保持 Scoped**：`StationManageMenu(ILicenseApiClient)` 依赖 Scoped 客户端，且 `CoreMenu` 在电路内解析，Scoped 是正确解。

**验收判据**：`IEnumerable<CJCore.Framework.Abstractions.IModule>` 能枚举出 29 个以上模块；`MapCJCoreRazorComponents()` 的 `AdditionalAssemblies` 覆盖全部含页面的程序集（无 404）。

---

### D3 菜单与布局：全量替换 CoreMenu / 只换数据源

| 选项 | 内容 |
|---|---|
| **A（推荐）** | 20 个 `IMenuService` 改为返回 `CJCore.Framework.Abstractions.MenuItem`；`MyNavMenu.razor` 删除，统一走 `CoreMenu`；3 条硬编码链接（本地部署包 / VNC 测试 ×2）迁到一个 `PlugShellMenuService`，分组落 `管理` / `调试`（“作业列表2”按 Q9 下线） |
| B | 保留 `MyNavMenu` 渲染，只把数据源换成 CJCore `IMenuService` | 分组/过滤/权限逻辑要在 CJPlug 侧再写一遍 → 双源 |

**建议：A（✅ 评审已定，2026-10-29，分组归一到 3 组）。** 差异面很小：仅 24 个文件触碰 `MenuItem`，20 个菜单类是纯声明式（唯一有依赖的是 `StationManageMenu(ILicenseApiClient)`，保持 Scoped 即可，`CoreMenu` 在电路内解析）。

**字段映射（必须逐项确认）**：

| CJPlug `MenuItem` | CJCore `MenuItem` | 处理 |
|---|---|---|
| `Icon` (string?) | `Icon` (string?) | 直通 |
| `Href` (string) | `Href` (string) | 直通 |
| `Match` (**`NavLinkMatch` 枚举**) | `Match` (**string: "Prefix"/"Exact"**) | 枚举转字符串；`NavLinkMatch.All` → `"Exact"` |
| `Text` / `Order` | `Text` / `Order` | 直通 |
| `GroupName`（默认 `"General"`，实际用 `MenuGroupEnum`） | `GroupName`（默认 `"通用"`） | **归一（见下）** |
| `SubMenuItems` | `SubMenuItems` | 直通 |
| — | `Roles` / `AccessKey` | **新增**：P5 补 |

### D3-1 分组归一（评审已定：3 组）

现状实际只有 5 个 GroupName 值（`MyNavMenu` 只特判了 `管理`/`调试`，其余平铺）：

| GroupName | 出现次数 | 旧壳表现 | 归一到 |
|---|---|---|---|
| `通用`（未赋值时的 CJCore 默认） | 多数 | 平铺 | **通用** |
| `管理` | 6 | 折叠组 | **管理** |
| `调试` | 2 | 折叠组 | **调试** |
| `插头管理` | 1 | 平铺 | **管理** |
| `Skills管理` | 1 | 平铺 | **管理** |

改动量：4 处字面量（`MenuGroupEnum.插头管理` 一处、`"Skills管理"` 一处，以及两处引用点）——纯数据，不动渲染。

> **折叠能力说明（必须知情）**：`CoreMenu` 不支持"分组折叠"，它是 `GroupName` → 标题 + 平铺。旧壳的 `管理`/`调试` 是可折叠 `MudNavGroup`，换壳后会变成平铺。若要恢复折叠，唯一正解是**把分组建模成父菜单项**（`MenuItem.SubMenuItems`，`CoreMenu` 会渲染成 `MudNavGroup`）——用一个 `IMenuService` 装饰器聚合即可，不动 20 个菜单文件。**本计划不把它放进 P2**，列为 P2 之后的可选增强（见 §11-Q10）。

**验收判据**：侧边栏 3 个分组标题（通用/管理/调试），菜单项集合与迁移前逐项一致；分组内无可见项时标题不渲染；`Order` 生效（旧壳不排序）。

---

### D4 身份体系：localStorage 守卫 / 最小认证桥 / 替换为内核身份模块

| 选项 | 内容 | 代价 | 风险 |
|---|---|---|---|
| A | 保留 localStorage 判定，自建 `PlugLayout` 复制 `FrameworkLayout` 再加守卫 | 290 行布局双源 | 低（技术债最高） |
| B | **最小认证桥**：只注册 Cookie scheme，登录/登出各补一处 `SignInAsync` | 1–1.5 人日 | 中（`[ModuleAccess]` 生效后 302） |
| **C（✅ 已定）** | **弃 ASP.NET Identity，换 `CJCore.Modules.ThreeRoleAdmin`** | 8–14 人日（含数据迁移与密码兼容） | 高：不可回滚；该模块**零产品验证** |

> **评审结论（2026-10-29）：C 已定，落点 = ThreeRoleAdmin（不是 AccountAuth）。**
> 用户口径：C-2 连 Identity 一起归一，D4 从 B 改为 C。

#### D4-1 为什么落点是 ThreeRoleAdmin 而不是 AccountAuth（证据）

| 维度 | CJPlug `User : IdentityUser<int>` | ThreeRoleAdmin `SysUser` | AccountAuth `AccountAuthUser` |
|---|---|---|---|
| 主键 | **int** | **int** ✅ | **Guid** ❌（41 个业务表 `UserId` 全要改型） |
| 部门 | `DepartmentId` + `Department` | `DepartmentId` + `Department`（ParentId/Manager）✅ | 无 ❌ |
| 用户组 | `UserGroup` + `UserGroupMember` | 同名同义 ✅ | 无 ❌ |
| 角色 | `UserRole` 表 + 权限矩阵 | `Role` + `UserRole`（RoleCode/RoleType/IsSystem）✅ | 用户表一个逗号串 ❌ |
| 锁定/状态 | `Status` / `IsLocked` | `Status` / `IsLocked` / `LockoutEnd` ✅ | 枚举 `Status` |
| 会话 | 无（localStorage） | `SysLoginSession`（可真撤销）✅ | `Auth_Sessions` |
| 密码 | Identity PBKDF2 v3 | 自有 PBKDF2（`PasswordHash`/`PasswordSalt`/`SaltIterations`） | 同型 |
| 认证栈 | — | `ThreeRoleAuthAdapters.cs` 实现全部 6 个扩展点 + `Login.razor` / `ResetPassword.razor` ✅ | 同型 |
| 产品验证 | 生产在用 | **零产品使用** ⚠️ | CJOEM 生产在用 |

**净结论**：ThreeRoleAdmin 是"结构超集且主键同型"——选它，41 个业务表的 `UserId` **一列都不用改类型**；选 AccountAuth 则要把它们全部改成 Guid，那是数据迁移里最贵的一类改动。代价是 ThreeRoleAdmin 没有产品验证过，本次即为首个。

#### D4-2 密码迁移：惰性重算（唯一低风险路径）

Identity v3 哈希格式与 CJCore 的 `salt + iterations` 双列**不兼容**。两条路：

| 方案 | 做法 | 代价 | 用户体验 |
|---|---|---|---|
| **惰性重算（推荐）** | 首登时用 Identity 哈希器校验旧哈希 → 校验通过后按 PBKDF2 重算并覆盖 | 保留一段 Identity 哈希校验代码（约 100 行）+ 1 人日 | 无感 |
| 强制改密 | 全部用户重置密码 | 0.5 人日 | 全员被打断 |

选惰性重算：CJPlug 是已交付产品，强制改密对现场是事故级体验。

#### D4-3 认证栈接入方式

ThreeRoleAdmin 自带完整栈，CJPlug **不需要写任何适配器**：

| 组件 | 来源 | CJPlug 侧动作 |
|---|---|---|
| 登录 / 登出 / 刷新端点 | `ThreeRoleAdmin.Api`（`CJCoreAuthEndpoints`） | `MapCJCoreThreeRoleAdmin()` 一行 |
| Cookie/JWT 双通道 | `AddCJCoreAuth`（由模块内部调用） | 无需单独调用 |
| 登录页 / 重置密码页 | `ThreeRoleAdmin.UI/Pages/{Login,ResetPassword}.razor` | 用内核页面，下线 CJPlug `LoginPage.razor` |
| 6 个认证扩展点 | `ThreeRoleAuthAdapters.cs` | 无需实现 |
| 真撤销 | `SysLoginSession` + `EnableRevocationCheck` | **可开启**（不再需要"无状态 Cookie"妥协） |

**验收判据**：未登录访问受保护页面 → 跳内核登录页；登录后回跳；登出后旧会话不可用（真撤销）；CJPlug 现有用户能用**原密码**登录成功（惰性重算生效）。

---

### D5 数据底座：双底座 / 单库双上下文 / 全量归一

| 选项 | 内容 | 结论 |
|---|---|---|
| A | 双底座（业务留 MainDbContext，CJCore 模块独立库） | 已否决（用户选 C-2） |
| B | 同库双上下文 | 已否决（`EnsureCreated` 的 `HasTables` 语义 + 跨进程顺序） |
| **C-2（✅ 已定）** | **单库单上下文**：删 `MainDbContext`，41 个业务模块全部并入 `DataDbContext`，身份换 ThreeRoleAdmin（其实体本就在 `DataDbContext`） | 本期执行 |

> **评审结论（2026-10-29）：C-2 已定。**

#### D5-1 为什么 C-2 在 ThreeRoleAdmin 下才自洽

关键事实：`ThreeRoleAdminModuleDbConfig` 本身就是一个 `IModuleDbConfig`，它的 `SysUser`/`Department`/`UserGroup`/`Role`/`SysLoginSession` **全部注册进 `DataDbContext`**。也就是说：

- 换 ThreeRoleAdmin ⇒ 身份表天然落在 Data 底座；
- 业务表再归一到 Data ⇒ 真的只剩**一个上下文、一个库、一套迁移引擎**；
- 若当初选 AccountAuth，其 `Auth_*` 表同样在 Data 底座，也自洽——但 Guid 主键的代价更高。

#### D5-2 前置：迁移基线（P0.5，不依赖任何决策）

`EnsureCreated` 必须换成 EF 迁移基线，否则：
- 41 张表纳管到 Data 引擎时，现有库没有版本历史，无法判断"已建/待建"；
- 后续任何加列（会话表、模块开关表）都无法演进。

做法：以**当前模型**生成 `InitialCreate` 基线 → 现有库执行 `Migrate()`（EF 检测到库已存在且无历史表时会走"基线空迁移"路径，需要人工确认脚本）→ 切到 Data 引擎接管。

#### D5-3 迁移执行顺序（不可颠倒）

```
1. P0.5 迁移基线（MainDbContext 先能迁移，不动模型）
2. 41 个 DbConfig 改写成 CJCore 契约（AddDbSets/ConfigEntities/StructureRevision）
3. 41 个消费者从 MainDbContext 改注入 DataDbContext
4. 引 ThreeRoleAdmin + 数据迁移脚本（AspNetUsers → TRA_SysUser 等）
5. 删 MainDbContext + Identity 包引用
6. 全量回归
```

**为什么不能先删 MainDbContext**：步骤 2–4 期间两套表必须同时在线，删了就没有回退路径。

**验收判据**：`DataDbContext` 一个上下文能建出全部业务表 + 身份表；`SchemaMigrationHistory` 有完整版本记录；重启后二次启动不重复建表；旧库升级后数据行数与迁移前一致。

---

### D6 权限落地深度 —— 已并入 D9（保留分档说明供实施参考）

> **评审结论（2026-10-29）**：D6 不再是独立决策点。D9=A 已确定"全量矩阵"，因此 L3 直接进本期；下面的分档只用于**实施排期**（先 L0 再 L1，最后 L2）。

| 档 | 范围 | 前置 | 落点 |
|---|---|---|---|
| L0 | 菜单 + AppBar 的 `AccessKey` 门控（只看"模块/功能是否启用"，与角色无关） | 无（不依赖 Principal） | P2 埋键、P6 接管（`/admin/modules`） |
| L1 | 页面 `[ModuleAccess(key, roles)]` 角色门 | P5 身份替换完成 | P5 |
| L2 | API `RequireModuleAccess(key)` | P5 + `ApiServer` 打开 `UseAuthorization()` + 共享 DataProtection | P5 末 |
| L3 | `CJCore.Modules.Permission` 全量矩阵（替代 `RolePermissionService`） | 角色体系统一 | P5 |

**实施提醒（L2 的真实成本在标注量，不在机制）**：CJPlug 当前 API 侧零鉴权，一开 `UseAuthorization()` 会导致"未标注端点 = 匿名可访问"，必须逐端点标注，工作量按端点数计（`ConfigModuleApis` 里 20+ 个 `AddXxxModuleApi()`）。建议 L2 单独一批、按模块推进。

**验收判据**：L0——`/admin/modules` 关掉某模块，其菜单项与 AppBar 项立即消失；L1——无权限角色访问该模块页面被拒；L2——未带凭据调用受保护 API 返回 401。

---

### D7 嵌入布局（`?hideMenu=true`）

**现状核对**：`EmbeddedLayout.razor`（205 行）不是"正常壳去掉侧栏"，而是**专用紧凑顶栏**——硬编码标题"工具流程编排"、**无菜单按钮**、无 AppBar 项、无主题切换、无品牌 logo。只做"隐藏抽屉"会让桌面嵌页面突然多出菜单死按钮与整套顶栏控件。

| 选项 | 内容 | 框架改动 | 与桌面端现状差异 |
|---|---|---|---|
| A | 只隐藏 `MudDrawer` | ~15 行 | 顶栏为完整版（菜单按钮成死按钮） |
| **B（✅ 已定）** | `hideMenu=true` → 隐藏 `MudDrawer` **与菜单按钮**；新增 `title=xxx` → 覆盖 AppBar 标题 | ~25 行 | 基本等价（桌面端传 `title=工具流程编排`） |
| C | 保留 CJPlug `EmbeddedLayout`（复制精简壳） | 0 | 视觉零变化，但双源，框架每次改布局都要跟 |

> **评审结论（2026-10-29）：B 已定。** A 会留一个"点了没反应的菜单按钮"，是明显体验缺陷；C 的双源会在框架每次改布局时都要返工。B 只比 A 多 10 行，且 `title` 是通用能力。

**实现形态**（`FrameworkLayout.razor`）：

```razor
@if (!_hideMenu)
{
    <MudDrawer ...><CoreMenu /></MudDrawer>
}
@if (!_hideMenu)
{
    <MudIconButton Icon="@Icons.Material.Filled.Menu" ... OnClick="@ToggleMenu" />
}
<MudText Typo="Typo.h6" Class="ml-2">@(_title ?? FrameworkOptions.ProductName)</MudText>
```
`_hideMenu` / `_title` 在 `OnInitialized` 解析 `NavigationManager.Uri` 的 `hideMenu` / `title`，并订阅 `LocationChanged` 重算。**不改 `FrameworkRoutes`、不改 `FrameworkOptions`**（避免污染产品配置面）。

**连带改动**：`CJ.Plug.Desktop/ViewModels/MainViewModel.cs` 9 条 URL → `?hideMenu=true&title=工具流程编排`（不传 title 则显示产品名）。

**验收判据**：`/TAS?hideMenu=true&title=工具流程编排` 无侧栏、无菜单按钮、标题正确；去掉参数后侧栏与按钮回来；桌面端 9 个入口逐一验证；CJOntology/CJEM 回归（不带参数 → 行为不变）。

---

### D8 能力模块替换批次

| 批次 | 模块 | 理由 | 前置 |
|---|---|---|---|
| **B1（P7-a）** | 系统配置 `CJ.Plug.SystemConfig` → `CJCore.Modules.SystemConfig` | 框架布局**已经**可选依赖 `ISystemConfigService`（读名称/图标/主题色），不接就白丢能力；且 `SystemConfig` 是唯一"接了就立刻看得见"的模块 | D5 数据底座就位 |
| B2（P7-b） | LLM：`CJ.Plug.LlmConfig` + `DeepSeekService` → `CJCore.Modules.LLM` | 收益大（统一 Provider、结构化输出、TokenJuice），但 `AddCJCoreLLM` 会拉入 Data 底座，且要改 `AiWorkflowBuilderService` / `AIChat` / `AIAgentPlug` 的注入 | B1 |
| B3（P7-c） | 许可证 `CJ.Plug.License` → `CJCore.Modules.License` | 两边都有，语义需逐条对齐（授权项、机器码、离线包） | 单独评估 |
| B4 | 用户/角色/审计 | **已由 D4=C 覆盖**：用户/部门/组/角色 → ThreeRoleAdmin `SystemManage`；审计 → `AuditManage`。不再作为可选替换项 | D4 |

**建议**：本期只承诺 **B1**；B2 单独立项（因为它牵动 AI 三条业务链）；B3 先做差异对照表再定。

> **评审结论（2026-10-29）：B 已定 = B1 + B3。** B2（LLM）不做，理由见下。

**B2 的新增阻断（2026-10-29 核实）**：CJCore 的 `ChatRequest` **没有 tools/functions 字段**（`CJCore.LLM.Abstractions/ChatRequest.cs`），即 `ILLMClient` 不支持工具调用；而 CJPlug 的 `DeepSeekService` 有完整的 MCP 工具链路（`DeepSeekService.McpTools.cs` + `OllamaSharp.ModelContextProtocol` 桥接）。直接换 LLM 会**打断 AskAI 的工具调用**。要做 B2 必须先给 CJCore 的 LLM 抽象加 tools 支持（内核级变更，影响全体产品），因此**不属本期**。

> 注：C-2 完成后，B2 原来的另一个障碍（`AddCJCoreLLM` 会 `AddCJCoreData`）已自动消失——数据底座已经统一。剩下的唯一阻断是 tools。

---

### D9 C-2 的连带下线范围（评审已定：A）

D4=C 换 ThreeRoleAdmin 后，CJPlug 的 4 个管理模块与之重叠。逐一对照实际页面后：

| CJPlug 模块 | 功能 | ThreeRoleAdmin 对应 | 是否可接管 |
|---|---|---|---|
| `LoginModule` | 登录页 + `LoginState` AppBar | `Login.razor` + 认证栈 | ✅ 可接管 |
| `UserManageModule` | 用户 / 角色 / 部门 / 用户组 | `SystemManage.razor` 四个页签（用户管理/角色管理/部门管理/用户组管理） | ✅ 可接管 |
| `AuditModule` | 审计日志 | `AuditManage.razor` | ✅ 可接管（数据模型需迁移） |
| `AuthModule` | **角色权限矩阵**（`RolePermissionService` + 6 个 `IFunctionPermissionProvider`） | ❌ **无对应**：`AuthManage.razor` 是"待审批/已处理/账号解锁"的审批流，不是权限矩阵 | ❌ **不可接管** |

**关键发现**：ThreeRoleAdmin 没有"角色 × 功能权限"的矩阵。CJCore 里的对应物是 **`CJCore.Modules.Permission`**（权限键 + 三层矩阵，CJOEM/CJOntology 已在用）。所以"授权管理"不是换页面，是**换权限模型**——从 CJPlug 的 `RolePermission` 表换成 `AccessKey` 三层矩阵。

这就是 D6（权限深度）与 D9 合并的原因：两者是同一个决策。

| 选项 | 内容 | 人日 | 后果 |
|---|---|---|---|
| **A 一并换矩阵（✅ 已定）** | 身份 + 用户 + 审计 + 授权全部由内核接管（`Permission` 矩阵替代 `RolePermissionService`） | +5–8（主要成本是 515 个 razor 逐页标 `AccessKey`，可分批） | 一处权限模型；授权管理页操作方式改变（角色勾选 → 权限键矩阵） |
| B 身份换、权限留 | `RolePermission` 表迁到 Data 底座，CJPlug 授权页 UI 不动 | +5–7（要把 `RolePermissionService` 从 Identity 解耦再迁表） | 双权限模型长期并存 |
| C 身份换、权限换矩阵但保留 CJPlug 菜单入口 | 用 `Permission` 矩阵，但入口仍在 CJPlug 菜单 | +4–6 | 中等 |

**待拍板**。

> **评审结论（2026-10-29）：A 已定。** 理由：C-2 已付出"删 MainDbContext + 迁 41 张表 + 换身份"的代价，若权限留在旧模型，等于单库上跑两套鉴权；B 的 +5–7 并不比 A 便宜多少，却留下永久分叉。
>
> **A 的下线清单（功能形态替换，非纯技术重构）**：`RolePermissionService` + `RolePermission` 表、6 个 `IFunctionPermissionProvider` 实现（改标 `[ModuleAccess]`）、CJPlug 授权管理页、`UserManage` 的 4 个页面（改跳 `/three-role/system`）。现场管理员的操作习惯会变，需在交付说明中标注。

---

### 决策汇总表

| ID | 决策 | 建议值 | 状态 | 阻塞的阶段 |
|---|---|---|---|---|
| D1 | 接入形态 | **A 壳内嵌** | ✅ **已定 A（2026-10-29）** | P1 起全部 |
| D2 | 模块契约 | **A1 直接替换（不设投影层）** | ✅ **已定 A1（2026-10-29）** | P1、P4 |
| D3 | 菜单与布局 | **A 全量替换 CoreMenu + 分组归一 3 组** | ✅ **已定 A（2026-10-29）** | P2 |
| D4 | 身份体系 | **C 替换为 ThreeRoleAdmin（弃 Identity）** | ✅ **已定 C（2026-10-29）** | P3（重排后） |
| D5 | 数据底座 | **C-2 单库单上下文（删 MainDbContext）** | ✅ **已定 C-2（2026-10-29）** | P2 起全部 |
| D6 | 权限深度 | ~~L0+L1；L2 单独评估~~ | **已并入 D9（A：全量矩阵）** | P5 |
| D7 | 嵌入布局 | **B hideMenu + 隐藏菜单按钮 + title 参数** | ✅ **已定 B（2026-10-29）** | P2（桌面端验收） |
| D8 | 模块替换批次 | **B = B1 系统配置 + B3 许可证**（LLM 不做） | ✅ **已定 B（2026-10-29）** | P7 |
| **D9** | C-2 连带范围：授权模型与模块下线 | **A 一并换矩阵（Permission）** | ✅ **已定 A（2026-10-29）** | P3–P5 |

---

## 5. 分阶段实施（C-2 路线 / 评审已定）

> **排序原则**：① 先统一契约（UI 一行不变，可独立验证）；② 壳替换尽早交付可见价值，用**临时守卫**过渡，不让安全窗口拖到数据阶段；③ 数据归一**逐模块推进**，不搞大爆炸；④ 身份替换在数据底座就位后一次做对；⑤ 模块治理与能力替换收尾。
>
> 每阶段 = 一个可独立回滚的提交批次。出口判据由你执行（构建/冒烟），本计划不代跑。

```
P0 基线 ──► P1 契约统一 ──► P2 壳替换(临时守卫) ──► P3 迁移基线
                                                          │
    ┌─────────────────────────────────────────────────────┘
    ▼
P4 业务表归一(逐模块) ──► P5 身份替换+权限矩阵+4模块下线 ──► P6 模块元数据/启停
                                                                    │
                                          P7 系统配置+许可证 ◄──────┘
                                                    │
                                                    ▼
                                              P8 A2A 工具面
```

| 阶段 | 内容 | 人日 | 可上线 |
|---|---|---|---|
| P0 | 基线护栏 | 0.5 | — |
| P1 | 契约统一（UI 不变） | 3–4 | 内部 |
| **P2** | **壳替换 + 嵌入布局 + 临时守卫** | 4–6 | **是（L1）** |
| P3 | 迁移基线（`EnsureCreated` → EF Migrations） | 1–2 | 否 |
| P4 | 业务表归一（41 模块，逐批） | 8–12 | 否（可分批灰度） |
| **P5** | **身份替换 + 权限矩阵 + 4 模块下线** | 13–22 | **是（L2/L3）** |
| P6 | 模块元数据 + 启停 + 功能键 | 3–4 | 是 |
| P7 | 系统配置 + 许可证替换 | 6–9 | 是 |
| P8 | A2A 工具面 | 5–8 | 是 |
| | **合计** | **43.5–67.5** | |

---

### P0 基线护栏（0.5 人日）

| 项 | 动作 |
|---|---|
| 分支 | `feat/cjplug-on-cjcore`，全程不合并主干 |
| 构建基线 | 记录 `dotnet build CJPlug/CJ.Plug-Aspire.sln` 的警告/错误数 |
| 快照 | `CJPlug/src/WebHost/CJ.Plug.MainPageContent/`、`CJPlug/src/Core/CJ.Plug.Models/{Contracts,Abstractions,Shared,DbContexts}/` |
| **数据快照** | **导出 `main333.db` 全量备份**（P3 之后任何一步失败都要能回到这里） |
| 冒烟脚本 | 9 条桌面端 URL + 8 服务健康检查 + 关键业务路径清单（§8.2） |
| 回滚点 | `git tag pre-cjcore-integration` + db 备份文件时间戳 |

---

### P1 契约统一（3–4 人日 | D2=A1）

**目标**：`IModule` / `IMenuService` / `MenuItem` 换成 CJCore 契约，**UI 完全不变**（仍用旧 `Routes`/`MainLayout`）。两个 commit。

| # | 位置 | 操作 |
|---|---|---|
| 1.1 | `HostWebServer.csproj` | 加 `CJCore.Framework.Abstractions` / `.Api` / `.UI` 三个引用 |
| 1.2 | 29 个模块类 | 换基类 + 补 `Name`（中文名）；**生命周期保持 Scoped 不动**（见 D2 生命周期段） |
| 1.3 | 约 30 个 `ServiceCollectionExtensions.cs` | `using` 换命名空间（只改 `IModule`/`ModuleBase` 相关行） |
| 1.4 | `Contracts/IModule.cs`、`Abstractions/ModuleBase.cs` | 删除 |
| 1.5 | 20 个 `Menus/*.cs` + `MyNavMenu.razor` | `MenuItem` 换类型；`Match` 枚举 → 字符串 |
| 1.6 | `Program.cs` | 加 `AddCJCoreFramework(...)`（先关日志抽屉）；拆掉 `BuildServiceProvider()` 二次建容器、保留程序集收集 |
| 1.7 | `Contracts/IMenuService.cs` | 删除 |

**出口**：构建通过；旧侧栏菜单**逐项**与 P0 一致；无新增运行时异常。
**回滚**：两个 commit 分别 revert；删除文件从 P0 快照恢复。

---

### P2 壳替换 + 嵌入布局 + 临时守卫（4–6 人日 | D1=A / D3=A / D7=B）

| # | 位置 | 操作 |
|---|---|---|
| 2.1 | `App_All.razor`、`Routes.razor`、`Layout/{MainLayout,MyNavMenu,EmbeddedLayout}.razor` | 删除 |
| 2.2 | 新增 `PlugHeadModule.cs` | 用 `IAppHeadService` 贡献原 `App_All` 全部 css/js（顺序敏感项见 §8.3） |
| 2.3 | 新增 `PlugShellModule.cs` | 宿主程序集 4 个 `@page` 的路由兜底 |
| 2.4 | 新增 `Menus/PlugShellMenu.cs` | 承接 **3 条**原硬编码链接（下载包 / VNC×2）；**“作业列表2”随 Q9 下线，不承接** |
| 2.5 | 20 个 `Menus/*.cs` | `GroupName` 归一 3 组（插头管理、Skills管理 → 管理） |
| 2.6 | 新增 `PlugLogDrawerModule.cs` | `LogsWrapper` 注册为 `AddAppBarDrawer`（执行日志，与平台日志并存） |
| 2.7 | `AIModule` / `LoginModule` | AppBar 改 `AddAppBarItem<T>(accessKey)`；模块改 Singleton |
| 2.8 | 29 个模块注册 | `AddScoped<IModule>` → `AddSingleton<IModule>`（**必须在 2.7 之后**） |
| 2.9 | `FrameworkLayout.razor`（改框架） | `hideMenu` 隐藏抽屉+菜单按钮；`title` 覆盖标题（D7=B，约 25 行） |
| 2.10 | `CJ.Plug.Desktop/ViewModels/MainViewModel.cs` | 9 条 URL 追加 `&title=工具流程编排` |
| 2.11 | 新增 `PlugTempGuardModule.cs` | **临时守卫**：经 `IAppHeadService.AddBodyHtml` 注入 5 行脚本——`localStorage.userid` 为空且当前非 `/login` 时 `location.replace('/login?returnUrl=...')`。语义与现状完全一致，P5 删除 |
| 2.12 | `Program.cs` 路由映射 | `MapRazorComponents<FrameworkApp>()` + Server + **WASM**（Radzen 用 InteractiveAuto）+ `AddAdditionalAssemblies` |

**出口**：首页/菜单（3 组）/AppBar/主题/Alt+M/Alt+L 正常；4 个宿主 `@page` 可达；桌面端 9 条 `?hideMenu=true&title=...` 无侧栏、无菜单按钮、标题正确；执行日志抽屉可用；抽 20 个跨模块页面无 404。
**回滚**：`git revert` 批次 + 从 P0 快照恢复 6 个删除文件 + 回滚框架 25 行。
**注**：临时守卫是**明确标注的过渡件**（代码注释写明"P5 删除"），不是长期方案。

---

### P3 迁移基线（1–2 人日 | D5 前置）

**为什么必须先做**：`EnsureCreated` 的 `HasTables` 语义是 C-2 的**头号拦路虎**——只要库里有任意表，它就不再建缺失的表。而 P4 会往同一个库里加 Data 引擎的表。**不做基线，P4 一定会随机缺表。**

| # | 动作 |
|---|---|
| 3.1 | `dotnet ef migrations add InitialCreate`（以**当前模型**生成基线，`--project CJ.Plug.Models --startup-project CJ.Plug.ApiServer`） |
| 3.2 | 现有库：手工插入 `__EFMigrationsHistory` 的 `InitialCreate` 记录（等价"基线空迁移"），避免重复建表 |
| 3.3 | `ApiServer` 启动改 `db.Database.MigrateAsync()` 替代 `EnsureCreatedAsync()` |
| 3.4 | 冒烟：全新空库能建全表；已有库升级后**数据行数不变**、无重复建表报错 |

**出口**：空库 / 旧库两条路径都能启动；`__EFMigrationsHistory` 有 `InitialCreate`。
**回滚**：还原 `Program.cs` + 删除 Migrations 目录（库未变）。

---

### P4 业务表归一（8–12 人日 | D5=C-2）

**目标**：41 个业务模块表从 `MainDbContext` 迁到 `DataDbContext`；Identity 表**暂留** `MainDbContext`（两者此时都用迁移记账，同库安全）。

| # | 动作 | 说明 |
|---|---|---|
| 4.1 | `HostWebServer` + `ApiServer` 加 `AddCJCoreData(o => o.ConnectionString = <同一个 main333.db>)` | 引擎开始接管 Data 侧 |
| 4.2 | 41 个 `*ModuleDbConfig` 改实现 CJCore 的 `IModuleDbConfig` | 契约差别只有 `StructureRevision`（可留 null） |
| 4.3 | 41 个消费者改注入 `DataDbContext` | `BaseRepositoryService` 先改（一处改动覆盖大部分仓储） |
| 4.4 | 分批顺序 | 先低风险：UnitTest → FMD → ToolResource → ToolActionSetting → Relation → PlugMarket → PDZ → File → Job → Station → Process → Execute → MCPTools → Skills → Knowledge → LLM 配置 → 其余 |
| 4.5 | 每批：建表 → 数据搬迁（`INSERT INTO ... SELECT`）→ 读写切换 → 回归 | 每批一个提交，可单独回滚 |
| 4.6 | 种子数据 | `ISeedDataProvider` 的 6 个实现迁到 Data 侧后重跑一次 |

**出口**：`DataDbContext` 能建出全部业务表；`SchemaMigrationHistory` 记录完整；每批业务回归通过；重启不重复建表。
**回滚**：按批 revert + 把该批表从 Data 侧摘除（数据仍在原表，未删）。

> ⚠ **本阶段不做**：不删 `MainDbContext`、不动 Identity 表。删除留到 P5 末尾。

---

### P5 身份替换 + 权限矩阵 + 4 模块下线（13–22 人日 | D4=C / D9=A）

| # | 动作 | 说明 |
|---|---|---|
| 5.1 | 引 `CJCore.Modules.ThreeRoleAdmin`（Api + UI） | `AddCJCoreThreeRoleAdmin()` + `MapCJCoreThreeRoleAdmin()` |
| 5.2 | 数据迁移脚本 | `AspNetUsers` → `TRA_SysUser`、`AspNetRoles` → `TRA_Role`、`AspNetUserRoles` → `TRA_UserRole`、`Department`/`UserGroup`/`UserGroupMember` 同名同义合并；`UserId` 全为 int，**不改型** |
| 5.3 | **密码：不做兼容层（Q12=直接下线）** | 迁移**不写** `PasswordHash`；所有账号首登被判失败。**硬前置**：迁移脚本必须为管理员账号写入初始密码（随机生成 → 打印到迁移报告），否则迁移后无人能登录（鸡生蛋）。管理员登录后经 `/three-role/reset-password` 或 `POST /api/three-role/account/reset-password` 逐个重置 |
| 5.4 | 登录入口 | 菜单/跳转指向 `/three-role/login`；`LoginPath` 同步；删除 `PlugTempGuardModule`（2.11） |
| 5.5 | 权限矩阵 | 引 `CJCore.Modules.Permission`；`RolePermissionService` + 6 个 `IFunctionPermissionProvider` 下线，改标 `[ModuleAccess(key, roles)]`；515 个 razor **分批**标注（先 6 个核心模块） |
| 5.6 | 4 模块下线 | `LoginModule`（登录页）、`UserManageModule`（用户/角色/部门/组页）、`AuthModule`（授权页）、`AuditModule`（审计页）→ 菜单指向 `/three-role/{system,auth,audit}` |
| 5.7 | 删除 `MainDbContext` + Identity 包引用 | **仅在 5.2–5.6 全部验收通过后**；同时删 `IdentityDbContext` 相关注册与 `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 引用 |
| 5.8 | 管 API 侧 | `ApiServer` 接入同一套认证（共享 DataProtection key ring），`UseAuthorization()` 打开，端点逐批标 `RequireModuleAccess` |

**出口**：旧用户用**原密码**能登录（惰性重算生效）；未登录访问受保护页跳 `/three-role/login`；登出后旧会话不可用（真撤销）；`/three-role/{system,auth,audit}` 四个页签功能可用；`MainDbContext` 已删除且全量构建通过；8 服务冒烟全绿。
**回滚**：**此阶段不可整体回滚**（数据已迁移）。保护措施：P0 的 db 备份 + 5.1–5.6 期间双表并存（迁移脚本只写不删源表，保留一个版本周期）。

---

### P6 模块元数据 + 启停 + 功能键（3–4 人日）

| # | 动作 |
|---|---|
| 6.1 | 29 个模块补元数据（`ModuleId` / `Group` / `Order` / `DependsOn`） |
| 6.2 | `AddCJCoreModuleAdmin()` + `MapCJCoreModuleAdmin()`——此时 Data 底座已就位，**启停状态可持久化**（`MA_` 表） |
| 6.3 | 菜单/AppBar 的 `AccessKey` 与模块 `ModuleId` 对齐；模块内功能键（每模块 1–3 个） |
| 6.4 | 验收：关掉某模块 → 菜单项与 AppBar 项即时消失；重启后状态保留 |

---

### P7 系统配置 + 许可证（6–9 人日 | D8=B）

| # | 动作 | 风险 |
|---|---|---|
| 7.1 | 引 `CJCore.Modules.SystemConfig`；实现 `PlugSystemConfigFieldProvider : ISystemConfigFieldProvider`，用 `Group: "远程查看"` + `Select`（两档）把现有唯一业务开关贡献成 `/system-config` 的一个页签（Q10） | 内核零改动；`FrameworkLayout` 自动读取名称/图标/主题色 |
| 7.1a | 退役 `CJ.Plug.SystemConfig`（页 + Api + ApiClient + Model 及其表） | 唯一消费方 `ToolExecuteService.cs:345` 需从 `MainApiClient.SystemConfigApiClient.GetValueAsync("RemoteViewMode")` 改为 `ISystemConfigService` 读取（ApiServer 进程内直读库） |
| 7.2 | **先出许可证差异对照表**（授权项 / 机器码 / 离线包 / 校验点 / 到期行为），对齐后再替换 `CJ.Plug.License` | 授权语义不一致会导致现场激活失败——**未对齐不得替换** |

---

### P8 A2A 工具面（5–8 人日）

| # | 动作 |
|---|---|
| 8.1 | `McpServer` 侧实现 `IToolExecutor`，把 `DynamicToolRegistry` 的 Tool 投影为执行器 |
| 8.2 | 注册 `CJCore.AgentTool.MCPTools`，`McpSurface` 一律 `External` |
| 8.3 | 验收：A2A 侧 `tools/list` 可见、`tools/call` 走通执行链 |

---

## 6. 关键实现骨架

### 6.1 `HostWebServer/Program.cs` 目标形态（差异视图）

```csharp
// ── 新增：框架装配（替代裸 Serilog 配置，接管日志/审计/权限默认引擎）──
builder.AddCJCoreStartupLogging();          // CJCore.Framework.Api.Logging

builder.Services.AddCJCoreFramework(builder.Configuration, o =>
{
    o.ProductName       = "寸金插座平台";
    o.DefaultIconUrl    = "/plug2.png";
    o.EnableLogDrawer   = true;             // 平台日志抽屉
    o.EnableLogAppBarButton = true;
    o.EnableThemeSwitcher   = true;
    o.LoginPath         = "/three-role/login";   // P5 起生效（P2–P4 用临时守卫）
    o.HeadOutletPrerender   = true;
});

// ── P4：数据底座（唯一上下文 DataDbContext；与 CJPlug 用同一个库）──
builder.Services.AddCJCoreData(o =>
{
    // CJPlug 已有 DbConnectionString（支持 SQLite / PostgreSQL / SqlServer）
    o.ConnectionString = DbConnectionString.ConnectionString;
    o.Provider = DbConnectionString.DbType switch
    {
        "PostgreSQL" => DataProvider.PostgreSql,
        "SqlServer"  => DataProvider.SqlServer,
        _            => DataProvider.Sqlite,
    };
    o.EnableAutoMigration = true;
});

// ── P5：身份与权限（ThreeRoleAdmin 自带认证栈，无需自写适配器）──
builder.Services.AddCJCoreThreeRoleAdmin();
builder.Services.AddCJCorePermission();            // Permission 矩阵（CJOEM/CJOntology 同款）

// ── P6：模块目录与启停（此时 Data 已就位 ⇒ MA_ 表可持久化）──
builder.Services.AddCJCoreModuleAdmin();

// ── 原有装配保持不动 ──
builder.Services.AddMudServices();
builder.Services.AddMudExtensions();
builder.Services.AddEventAggregator();
builder.AddElsaServicesForWeb();
builder.Services.ConfigModulePageServices();
builder.Services.AddPlugsBundle();
builder.Services.AddXmlConfiguredServices();
builder.Services.AddXmlConfiguredExecuteServices();
builder.Services.AddRadzenComponents();
builder.Services.ConfigOtherServices();

// ── 新增：宿主壳模块（CJ.Plug.MainPageContent 程序集的 4 个 @page 路由兜底）──
builder.Services.AddSingleton<CJCore.Framework.Abstractions.IModule, PlugShellModule>();

var app = builder.Build();

// ── 管道（P5 起；**替换**原 Program.cs:145-150 那一段，不是在它之后追加）──
// 顺序与 CJOntology 的 HostPipelineComposition.cs:130/140 一致
app.UseRouting();
app.UseWebSockets();          // 原有：VNC/SSH 远程桌面代理
app.UseAuthentication();      // ← CJPlug 当前这两行都不存在（147-148 行被注释）
app.UseAuthorization();
app.UseAntiforgery();

// ── 端点（按阶段补齐）──
app.MapCJCoreThreeRoleAdmin();   // P5：/three-role/{login,system,auth,audit}
app.MapCJCorePermission();       // P5：权限矩阵
app.MapCJCoreModuleAdmin();      // P6：/admin/modules

// ── 关键：不要在这里再跑 InitializeAsync ──
// 框架的 FrameworkApp.OnInitializedAsync 已用「进程级静态标志 + 信号量」保证只执行一次
// （CJCore.Framework.UI/FrameworkApp.razor:48-77）。此处再跑一次虽不会造成 AppBar 重复
// （CJCore 的 DefaultAppBarService 用 HashSet<Type> 按组件类型去重），但会让任何未来
// 非幂等的 InitializeAsync 被执行两遍，且初始化职责分裂在两处。
// 这里只需要「枚举程序集」用于路由发现，不需要初始化。

// ── 路由：暂不用 MapCJCoreRazorComponents()（见 §5-P2/2.6）──
var moduleAssemblies = app.Services.GetRequiredService<IEnumerable<CJCore.Framework.Abstractions.IModule>>()
    .Select(m => m.GetType().Assembly)
    .Distinct()
    .Where(a => a != typeof(CJCore.Framework.UI.FrameworkApp).Assembly);

app.MapRazorComponents<CJCore.Framework.UI.FrameworkApp>()
   .AddInteractiveServerRenderMode()
   .AddInteractiveWebAssemblyRenderMode()   // 保留：RadzenComponents 用 InteractiveAuto
   .AddAdditionalAssemblies(moduleAssemblies.ToArray());
```

> ⚠ **与现状的差异（必须一起改）**：现 `Program.cs:126-134` 在 `builder.Build()` 之后用
> `builder.Services.BuildServiceProvider()` **新建了一个与运行容器无关的容器**来跑 `InitializeAsync`。
> 危害有二：① 那个容器的单例被创建后无人释放（重复建容器 = 双份单例、双份 HttpClient/连接池）；
> ② 模块注册落进临时容器（`IAppBarService` 在 CJPlug 是 Scoped，各容器一份），真正在用的容器里
> 其实什么都没注册——这正是旧 `Routes.razor` 又跑了一遍初始化的原因。
> 换壳时这一段必须删掉，初始化只保留 `FrameworkApp` 一处。

### 6.2 壳模块（必须新增，否则宿主程序集页面 404）

```csharp
// CJ.Plug.MainPageContent/PlugShellModule.cs
using CJCore.Framework.Abstractions;

namespace CJ.Plug.MainPageContent;

/// <summary>
/// 宿主壳模块：唯一职责是让 CJ.Plug.MainPageContent 程序集进入路由发现。
/// 该程序集内含 4 个 @page（DownloadPackage / Error / JobManageList2 / JobMonitor2），
/// 切换到 FrameworkApp 后 AppAssembly 变成 CJCore.Framework.UI，不注册就会 404。
/// </summary>
public sealed class PlugShellModule : ModuleBase
{
    public override string Name     => "插座宿主壳";
    public override string ModuleId => "plug-shell";
    public override string Group    => "宿主扩展";
    public override bool   IsMandatory => true;
}
```

### 6.3 菜单迁移示例（前 / 后）

```csharp
// 前（CJ.Plug.HomePage/Menus/HomePageMenu.cs）
public class HomePageMenu : IMenuService
{
    public ValueTask<IEnumerable<CJ.Plug.Models.Shared.MenuItem>> GetMenuItemsAsync(CancellationToken ct = default)
        => new(new List<CJ.Plug.Models.Shared.MenuItem>
        {
            new() { Icon = Icons.Material.Filled.Home, Href = "/", Text = "首页" }
        });
}

// 后（仅换命名空间 + Match 由枚举变字符串）
using CJCore.Framework.Abstractions;

public class HomePageMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken ct = default)
        => new(new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Home,
                Href = "/",
                Text = "首页",
                GroupName = "业务",
                Match = "Prefix",          // NavLinkMatch.All → "Exact"
                AccessKey = "plug-home",   // P4/P5 补
            }
        });
}
```

### 6.4 AppBar 迁移示例（前 / 后）

```csharp
// 前（AIModule/ServiceCollectionExtensions.cs）
public class AIModule(IAppBarService appBarService) : ModuleBase
{
    public override ValueTask InitializeAsync(CancellationToken ct = default)
    {
        appBarService.AddAppBarItem<AskAI>();
        return ValueTask.CompletedTask;
    }
}

// 后（CJCore 的 AddAppBarItem 带 AccessKey，且服务是 Singleton）
using CJCore.Framework.Abstractions;

public sealed class AIModule(IAppBarService appBarService) : ModuleBase
{
    public override string Name     => "AI 助手";
    public override string ModuleId => "plug-ai";
    public override string Group    => "业务";

    public override ValueTask InitializeAsync(CancellationToken ct = default)
    {
        appBarService.AddAppBarItem<AskAI>("plug-ai");   // 关掉模块 → 按钮自动消失
        return ValueTask.CompletedTask;
    }
}
```

### 6.5 `hideMenu` + `title` 框架扩展（D7=B）

```razor
@* FrameworkLayout.razor —— 只增不改，默认行为零变化 *@
@if (!_hideMenu)
{
    <MudDrawer @bind-Open="@_drawerOpen" Variant="@DrawerVariant.Persistent"
               ClipMode="DrawerClipMode.Always" Elevation="1" Width="200px">
        <CoreMenu />
    </MudDrawer>
}

@* AppBar 内：菜单按钮同样按 _hideMenu 隐藏，否则桌面端会出现"点了没反应"的死按钮 *@
@if (!_hideMenu)
{
    <MudTooltip Text="@("菜单 (Alt+M)")">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit"
                       Edge="Edge.Start" OnClick="@ToggleMenu" />
    </MudTooltip>
}
<MudText Typo="Typo.h6" Class="ml-2">@(_title ?? FrameworkOptions.ProductName)</MudText>

@code {
    private bool _hideMenu;
    private string? _title;

    protected override void OnInitialized()
    {
        ReadLayoutQuery(Navigation.Uri);
        Navigation.LocationChanged += (_, e) =>
        {
            ReadLayoutQuery(e.Location);
            InvokeAsync(StateHasChanged);
        };
    }

    /// <summary>
    /// 嵌入形态参数（2026-10-29《CJPlug 接入 CJCore》）：
    /// hideMenu=true 隐藏侧栏与菜单按钮；title=xxx 覆盖顶栏标题。
    /// 桌面端（CJ.Plug.Desktop）用这两个参数复用同一批路由。
    /// </summary>
    private void ReadLayoutQuery(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) return;
        var q = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(u.Query);
        _hideMenu = q.TryGetValue("hideMenu", out var f)
                    && bool.TryParse(f.ToString(), out var b) && b;
        _title = q.TryGetValue("title", out var t) && !string.IsNullOrWhiteSpace(t)
                 ? t.ToString()
                 : null;
    }
}
```

> ⚠ 改框架后必须回归 CJOntology / CJEM：它们从不带这两个参数，预期零变化，但要实测一次登录页与三个业务页。

### 6.6 身份替换（P5 | D4=C）

**不需要写适配器**——ThreeRoleAdmin 已自带全栈：

```csharp
// 装配（HostWebServer + ApiServer 各一行）
builder.Services.AddCJCoreThreeRoleAdmin();
builder.Services.AddCJCoreData(o => o.ConnectionString = PlugDb.ConnectionString);  // 已有

// 管道
app.UseAuthentication();
app.UseAuthorization();

// 端点
app.MapCJCoreThreeRoleAdmin();
```

**密码兼容层（唯一的自写代码）**：

```csharp
// P5 临时件，迁移完成后（或一个版本周期后）删除
internal static class IdentityHashCompat
{
    // Identity v3 格式：0x01 | prf(4) | iter(4) | saltLen(4) | salt | subkey
    public static bool TryVerify(string? identityHashBase64, string password)
        => new Microsoft.AspNetCore.Identity.PasswordHasher<object>()
               .VerifyHashedPassword(new object(), identityHashBase64 ?? "", password)
           != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
}
```

迁移流程：`AspNetUsers.PasswordHash` → 原样搬到 `TRA_SysUser` 的临时列（或迁移日志表）→ 首登时 `IdentityHashCompat.TryVerify` 通过 → 用 `Pbkdf2PasswordHasher` 重算写 `PasswordHash/PasswordSalt/SaltIterations` → 清空临时列。

**数据映射表**：

| 源（Identity） | 目标（ThreeRoleAdmin） | 注意 |
|---|---|---|
| `AspNetUsers` | `TRA_SysUser` | 主键同为 int，**不改型**；`DepartmentId`/`Status`/`IsLocked` 直接映射 |
| `AspNetRoles` | `TRA_Role` | `Name` → `RoleCode`，`NormalizedName` 归一化差异需人工核对 |
| `AspNetUserRoles` | `TRA_UserRole` | 纯关联，直搬 |
| `Department`（CJPlug 自有） | `TRA_Department` | 同名同义（含 ParentId） |
| `UserGroup` / `UserGroupMember` | `TRA_UserGroup` / `TRA_UserGroupMember` | 同名同义 |
| `AspNetUserClaims` / `Logins` / `Tokens` | — | 不用，丢弃 |

### 6.7 `ModuleAdmin` 接入（P6）

```csharp
services.AddCJCoreModuleAdmin();     // P6 时 Data 底座已就位 ⇒ MA_ 表可持久化
app.MapCJCoreModuleAdmin();          // /admin/modules
```

### 6.8 删除清单（累计）

| 文件 / 资产 | 阶段 | 前置 |
|---|---|---|
| `CJ.Plug.MainPageContent/App_All.razor` | P2 | 2.2 静态资源贡献就位 |
| `CJ.Plug.MainPageContent/Routes.razor` | P2 | 2.3 壳模块就位 |
| `CJ.Plug.MainPageContent/Layout/MainLayout.razor` | P2 | 2.7/2.8/2.9 迁移完 |
| `CJ.Plug.MainPageContent/Layout/MyNavMenu.razor` | P2 | 2.4 壳菜单就位 |
| `CJ.Plug.MainPageContent/Layout/EmbeddedLayout.razor` | P2 | D7=B 生效 + 桌面端验收 |
| `CJ.Plug.Models/Contracts/IModule.cs` | P1 | 29 个模块类改继承 |
| `CJ.Plug.Models/Abstractions/ModuleBase.cs` | P1 | 同上 |
| `CJ.Plug.Models/Contracts/IMenuService.cs` | P1 | 20 个菜单类改类型 |
| `CJ.Plug.Models/Shared/MenuItem.cs` | P2 | 无剩余引用 |
| `CJ.Plug.Models/Services/DefaultAppBarService.cs`、`Contracts/IAppBarService.cs` | P2 | 2.7/2.8 迁移完 |
| `PlugTempGuardModule.cs` | P5 | 真认证生效 |
| `LoginModule`（登录页） | P5 | `/three-role/login` 可用 |
| `UserManageModule` | P5 | `/three-role/system` 四页签可用 |
| `AuthModule` | P5 | `Permission` 矩阵可用 |
| `AuditModule` | P5 | `/three-role/audit` 可用 |
| `RolePermissionService` + `RolePermission` 表 | P5 | 矩阵接管 |
| 6 个 `IFunctionPermissionProvider` | P5 | 改标 `[ModuleAccess]` |
| `MainDbContext` + Identity 包引用 | P5 末 | 5.2–5.6 全部验收通过 |

> **保留不动**：`IBaseRepositoryService`（内部换注入类型）、`ISeedDataProvider`（换契约）、`IModuleDbConfig`（改契约实现）、各模块 `Pages/*`。

---

## 7. 风险登记册

| ID | 风险 | 概率 | 影响 | 触发信号 | 缓解 |
|---|---|---|---|---|---|
| R1 | **P2–P5 期间只有 localStorage 临时守卫**（纯前端、可绕过、无服务端校验） | 高（必然存在） | 中 | 安全审计/渗透测试提出"可绕过登录" | 明确标注为过渡件（代码注释 + 交付说明）；P5 一到立刻删除；期间不对外宣传为"已鉴权"；必要时把 P5 提前 |
| R2 | 宿主程序集页面 404 | 高（若不建壳模块） | 中 | `/download-package` 404 | §6.2 壳模块；验收逐页点 |
| R3 | `ModuleAccessRouteView` 因"无认证体系"匿名放行，误以为鉴权已生效 | 高 | 高（安全错觉） | 未登录也能进页面 | P3 出口必须实测 302；文档明确标注 L0≠鉴权 |
| R4 | 两套建表引擎作用于同一 SQLite 文件 | 中 | 高（数据） | 自动迁移日志出现重复建表/归档 | D5-A 独立库文件；禁止同库 |
| R5 | `EnsureCreated` 无法加表 → P3 会话表/模块状态表受阻 | 高 | 中 | 新表查不到 | P3 用无状态 Cookie；P4 接受进程内存储；P6 先做迁移基线 |
| R6 | Scoped→Singleton 生命周期变更踩"singleton 捕获 scoped" | 中 | 中 | 启动即抛 `Cannot consume scoped service` | 菜单保持 Scoped；模块类只在 P2 换完 AppBar 服务（2.7/2.8）后再改 Singleton（2.13） |
| R7 | `RadzenComponents @rendermode="InteractiveAuto"` 依赖 WASM 注册 | 中 | 中 | 弹窗/通知不工作 | 保留 `.AddInteractiveWebAssemblyRenderMode()`；不用框架的 `MapCJCoreRazorComponents()` 直至 Radzen 清理 |
| R8 | `MainApiClient` 同名（CJPlug 全局命名空间 / CJCore `Framework.Api.ApiClient`）导致编译歧义 | 中 | 低 | CS0104 | 引用文件加别名 `using CoreApiClient = CJCore.Framework.Api.ApiClient.MainApiClient;` |
| R9 | `IModuleDbConfig` 同名不同契约，误 `using` 到另一个 | 中 | 高（编译期能挡住，但修起来成片） | CS0535 | 全仓 `using` 显式限定；禁止 `global using` 这两个命名空间 |
| R10 | 菜单项数量/分组在迁移中漂移 | 中 | 低 | 侧栏少项 | P1 出口做逐项对数 |
| R11 | 框架 15 行改动影响 CJOntology/CJEM | 低 | 中 | 布局异常 | 改动只增分支，默认值不变；改后回归两个产品 |
| R12 | `AddCJCoreFramework` 与 CJPlug 的 MudBlazor 服务/Providers 重复注册 | 中 | 低 | 弹窗重复/主题闪烁 | 只保留一处 `AddMudServices`；Provider 由 `FrameworkLayout` 统一渲染，删除旧布局里的 4 个 Provider |
| R13 | 电路内拿不到 Cookie 身份 → 已标注页面无限 302 | 中 | 高 | 登录后仍被踢回 `/login` | **P3 步骤 3.0 先验证**；不通过则补 `CookieAuthStateProviderBase<TState>` 子类（约 60 行） |
| R14 | Cookie 跨进程不生效：`ApiServer` 不认 UI 宿主签发的 Cookie | 高（若 P6 想开 API 鉴权） | 中 | API 恒 401 或恒匿名 | 本期不动 API；P6 前先让两进程共享 DataProtection key ring |
| R15 | `EnableRevocationCheck=false` = 无服务端真撤销，登出后旧标签页仍可用 | 高 | 低 | 安全审计提出问题 | 本期接受并在交付说明标注；P6 补 `IAuthSessionStore` + 会话表 |
| **R16** | **密码迁移失败**：惰性重算覆盖不全（如用户从不登录、或旧哈希格式变体）→ 用户无法登录 | 中 | 高 | 现场报"密码错误" | 迁移前统计全部哈希格式；对无法识别格式的账号生成一次性重置码；保留 Identity 校验器至**两个版本周期**后再删 |
| **R17** | **ThreeRoleAdmin 零产品验证**（本产品是首个使用方） | 高 | 高 | 登录/授权链路异常且无历史经验可查 | P5 前先做一次"空壳联调"（仅接 ThreeRoleAdmin 到空白宿主，跑通登录/改密/授权）；出问题时有 CJPlug 旧栈可回（P5 期间双表并存） |
| **R18** | **许可证替换语义不一致** → 现场激活失败 | 中 | 高 | 激活被拒/授权项缺失 | 先出差异对照表（D8 已要求）；未对齐不得替换；保留旧 License 模块一个版本 |
| **R19** | **数据归一期间读写分裂**：部分模块已切 `DataDbContext`、部分仍写 `MainDbContext`，同一实体两处读写 | 中 | 高 | 数据"写了看不见" | 按模块**整块**切换（读+写同批），禁止跨批混合；每批验收含"写后立即读"用例 |
| **R20** | 用户/授权/审计页面替换（`/three-role/*`）导致管理员操作习惯改变、误操作 | 高 | 中 | 现场投诉/误配权限 | 交付说明附新旧页面映射表；上线后一周保留旧页面只读入口（若成本可控） |

---

## 8. 验证方案

### 8.1 编译门禁（每阶段出口，由你执行）

```bash
# 子集（快）：仅 UI 宿主链路
dotnet build CJPlug/src/WebHost/CJ.Plug.HostWebServer/CJ.Plug.HostWebServer.csproj

# 全量（阶段收口）
dotnet build CJPlug/CJ.Plug-Aspire.sln
dotnet build CJCore/CJCore.sln          # 因 P2 改了 FrameworkLayout

# 下游回归（P2 收口必跑）
dotnet build CJOntology/CJOntology.sln
```

判据：错误 0；警告数 ≤ P0 基线 + 5。

### 8.2 冒烟矩阵

| # | 场景 | 期望 |
|---|---|---|
| S1 | `/` 首页 | `FrameworkLayout` 渲染；产品名"寸金插座平台"；图标 `/plug2.png` |
| S2 | 侧边栏 | 菜单项与 P0 逐项一致；**3 个分组**（通用/管理/调试）；`Order` 生效 |
| S3 | 主题切换 | 亮/暗切换生效并持久化 |
| S4 | Alt+M / Alt+L | 菜单开合 / 日志抽屉 |
| S5 | `/download-package`、`/Error`、`/JobManageList2`、`/JobMonitor2` | 200，非 404 |
| S6 | `/TAS?hideMenu=true&title=工具流程编排`（桌面端 9 条之一） | 无侧栏、**无菜单按钮**、标题为"工具流程编排" |
| S7 | `/ProcessEdit/...?hideMenu=true` | 编辑器正常（Z.Blazor.Diagrams） |
| S8 | 执行日志抽屉 | 打开并收到 SignalR 执行日志 |
| S9 | `/MCPToolManage` 发布工具 → MCP 客户端 `tools/list` | 工具可见（P8 后 `tools/call` 走通） |
| S10 | Elsa 设计器 `/elsa-studio` | 可打开、可拖拽（壳替换不影响） |
| S11 | 未登录访问受保护页面（P5 后） | 跳 `/three-role/login?returnUrl=…` |
| S12 | `/admin/modules`（P6 后） | 列出约 29 个模块；关某个 → 菜单项消失；重启后状态保留 |
| S13 | 8 个服务健康检查 | 全部 200 |
| S14 | **旧密码登录**（P5 后） | 迁移前的老账号用**原密码**能登录（惰性重算），且 `TRA_SysUser` 的哈希列已变为 PBKDF2 格式 |
| S15 | **数据行数核对**（P4 每批后） | 迁移前后各表 `COUNT(*)` 一致（抽样 + 关键表全量） |
| S16 | **登出真撤销**（P5 后） | 登出后同一 Cookie 再请求 → 401/跳登录（不再是"清 Cookie 就算"） |
| S17 | `/three-role/{system,auth,audit}` | 用户/角色/部门/用户组四页签可用；审计可查 |

### 8.3 回归重点（易被忽略）

1. **`CJ.Plug.Desktop`** 的 9 条入口（`MainViewModel.cs`）——它们是 `hideMenu` 的唯一消费方。
2. **`CJ.Plug.AmisIntegration`** 的 CSS/JS 顺序（`sdk.js` 必须在 body 末尾，`amisLoad` 内联脚本）。
3. **noVNC / xterm** 的静态资源路径（`_content/CJ.Plug.GuacamoleUI/...`）。
4. **ExcelPlug handsontable** 的 CSS（`_content/ExcelPlug/hst/...`）。
5. **SignalR 日志链路**（`SignalRLogSink("Front")`）在换日志装配后仍要工作。

### 8.4 观测

- 启动日志：`AddCJCoreStartupLogging()` 落盘到可执行文件同级 `Logs\`，用于确认"模块初始化顺序 + 静态资源自检"。
- 静态资源自检（`StaticAssetsProbe`）：任何 `200 + text/html` 兜底的 css/js 会被点名，正好覆盖 R2/R7 类问题。

---

## 9. 回滚方案

| 阶段 | 回滚动作 | 数据影响 | 耗时 |
|---|---|---|---|
| P1 | `git revert` 两个 commit；删除文件从 P0 快照恢复 | 无 | 分钟级 |
| P2 | `git revert` 批次；恢复 6 个删除文件；回滚 `FrameworkLayout` 25 行 | 无 | 30 分钟 |
| P3 | 还原 `Program.cs` 的 `Migrate()`→`EnsureCreated`；删 Migrations 目录 | 库未变（基线只是记账） | 10 分钟 |
| P4 | **按批** revert：把该批表从 Data 侧摘除，读写切回 `MainDbContext`（源表未删） | 无损失（双份数据） | 每批 30 分钟 |
| P5 | ⚠ **不可整体回滚**（数据已迁、页面已换） | 需还原 P0 的 db 备份 | 小时级 |
| P6 | 移除 `AddCJCoreModuleAdmin` 两行；`MA_` 表保留不读 | 无 | 10 分钟 |
| P7 | 恢复旧模块注册与页面；许可证保留旧栈 | 授权数据未迁则无损 | 1 小时 |
| P8 | 移除 `IToolExecutor` 注册 | 无 | 10 分钟 |

**两个不可回滚点**：

1. **P3 的 `Migrate()`**：一旦执行，回退需还原数据库文件 —— P0 的 db 备份是唯一退路。
2. **P5 的身份替换**：用户/角色数据已搬到 `TRA_*`，旧表即使保留也不再被写。**保护措施**：5.1–5.6 期间迁移脚本**只写不删**源表，保留一个完整版本周期；P5 出口前必须验证 P0 备份可用（做一次恢复演练）。

---

## 10. 工作量与里程碑

| 里程碑 | 内容 | 人日 | 累计 | 可上线 |
|---|---|---|---|---|
| M0 | P0 基线（含 db 备份） | 0.5 | 0.5 | — |
| M1 | P1 契约统一（UI 不变） | 3–4 | 3.5–4.5 | 内部 |
| **M2** | **P2 壳替换 + 嵌入布局 + 临时守卫** | 4–6 | **7.5–10.5** | **是（L1：统一壳）** |
| M3 | P3 迁移基线 | 1–2 | 8.5–12.5 | 否 |
| M4 | P4 业务表归一（分批） | 8–12 | 16.5–24.5 | 否（可分批灰度） |
| **M5** | **P5 身份替换 + 权限矩阵 + 4 模块下线** | 13–22 | **29.5–46.5** | **是（L2/L3：真守卫）** |
| M6 | P6 模块元数据/启停 | 3–4 | 32.5–50.5 | 是 |
| M7 | P7 系统配置 + 许可证 | 6–9 | 38.5–59.5 | 是 |
| M8 | P8 A2A 工具面 | 5–8 | **43.5–67.5** | 是 |

> **两个可交付时点**：
> - **M2（7.5–10.5 人日）**：统一壳上线（视觉/菜单/主题/日志/嵌入形态），**但身份仍是旧的**（localStorage 临时守卫）。
> - **M5（29.5–46.5 人日）**：身份/数据/权限全部归一，此时才叫"接入 CJCore 完成"。
>
> ⚠ 与初稿 13.5–19.5 的差额全部来自 C-2 + D9：迁移基线、41 张表归一、身份替换、权限矩阵。若只想拿 L1，做到 M2 停止即可，后续可随时接续。

## 11. 待确认清单

### 11.1 已随评审收口（无需再答）

| # | 问题 | 结论 |
|---|---|---|
| Q1 | 接入形态 | ✅ D1=A 壳内嵌 |
| Q2 | 认证是否与壳同批 | ✅ 不再需要（P2 用临时守卫，P5 才换身份） |
| Q3 | 是否接受无状态 Cookie | ✅ 不再适用（P5 换 ThreeRoleAdmin 后可开真撤销） |
| Q4 | 数据底座 | ✅ D5=C-2 单库单上下文 |
| Q5 | 权限模型 | ✅ D9=A 全量矩阵 |
| Q6 | 能力模块替换批次 | ✅ D8=B（系统配置 + 许可证） |
| Q7 | 嵌入布局 | ✅ D7=B（hideMenu + title） |

### 11.2 仍需你补充的输入

| # | 问题 | 我的建议默认值 |
|---|---|---|
| Q8 | 各模块 `ModuleId` 命名 | ✅ **`plug-<模块>`**（与 CJCore 的 `llm`/`data`/`permission` 及同名 `skills` 区分） |
| Q9 | 4 个页面去留 | ✅ **`JobManageList2`（709 行重复页）下线**；`JobMonitor2` 澄清为 Elsa 实例查看器（`/workflows/instances/{id}/view`）**必须保留**；`DownloadPackage` / `Error` 保留 |
| Q10 | 系统配置形态 | ✅ **合成一个页面**：用 `ISystemConfigFieldProvider`（`Group` = 页签标题）把「远程查看」作为新页签贡献进 `/system-config`；CJPlug 的 `SystemConfigModule`（页+API+Model）整体退役 |
| Q11 | 明文 `Password` 列处置 | ✅ **不迁移 + 强制改密**：目标表无此列；迁移报告列出"该列非空"的账号数，并要求这些账号改密 |
| Q12 | 密码兼容层 | ✅ **直接下线**（不做惰性重算）。代价：全员改密；硬前置见 P5/5.3（先给管理员写初始密码） |
| Q13 | 侧栏分组折叠 | ✅ **本期不做**；需要时用一个 `IMenuService` 装饰器把同组项聚合为父项（约 1 人日） |
| Q14 | P1 起步方式 | ✅ **直接开工**（按 §5-P1 的 commit 1 → commit 2 顺序） |

---

## 附录 A：契约差异速查

| 概念 | CJPlug | CJCore | 兼容性 |
|---|---|---|---|
| 模块 | `IModule { ValueTask InitializeAsync(ct) }`（**无 Name**） | `IModule { string Name; ValueTask InitializeAsync(ct) }` | ❌ 缺 `Name` |
| 模块基类 | `ModuleBase : IModule`（空） | `ModuleBase : IModule, IModuleDescriptor`（7 个 virtual 元数据） | ⚠ 可继承，但要补 `Name` |
| 菜单服务 | `IMenuService` → `Shared.MenuItem` | `IMenuService` → `Framework.Abstractions.MenuItem` | ❌ 返回类型不同 |
| 菜单项 | `Match` 是 `NavLinkMatch` 枚举；无 `Roles`/`AccessKey` | `Match` 是 `string`；有 `Roles`/`AccessKey` | ❌ 结构不同 |
| AppBar | `IAppBarService`（Scoped，无键、无抽屉） | `IAppBarService`（Singleton，带 `AccessKey` + `AppBarDrawers`） | ❌ 生命周期 + 能力不同 |
| 数据库配置 | `IModuleDbConfig { AddDbSets; ConfigEntities }`（41 实现） | `IModuleDbConfig { AddDbSets; ConfigEntities; StructureRevision }` | ⚠ **同名不同契约** |
| 种子 | `ISeedDataProvider { Name; Order; SeedAsync }`（6 实现） | 同签名 + `DisplayName` | ✅ 近乎一致 |
| 功能权限 | `IFunctionPermissionProvider { ModuleName; ModuleIcon; GetPermissions }`（6 实现） | 同签名，`FunctionPermissionItem` 多 `ParentKey`/`Kind`/`Category` | ✅ 超集 |
| ApiClient 聚合 | `MainApiClient`（**全局命名空间**，Singleton） | `MainApiClient`（`CJCore.Framework.Api.ApiClient`，Scoped） | ⚠ 同名不同命名空间 |
| 认证 | Identity + localStorage `userid` | Cookie/JWT + `ClaimsPrincipal` | ❌ 体系不同 |
| 数据演进 | `EnsureCreated` + 手写 ALTER | 自动迁移引擎（SchemaDiffer） | ❌ 机制不同 |
| 壳 | `App_All` + `Routes` + `MainLayout` + `MyNavMenu` | `FrameworkApp` + `FrameworkRoutes` + `FrameworkLayout` + `CoreMenu` | ❌ 完全不同 |

## 附录 B：同名类型冲突登记（编译期必须显式限定）

| 类型 | CJPlug | CJCore | 冲突形态 |
|---|---|---|---|
| `IModule` | `CJ.Plug.Models.Contracts` | `CJCore.Framework.Abstractions` | 同短名 |
| `IModuleDbConfig` | `CJ.Plug.Models.Contracts` | `CJCore.Modules.Data` | 同短名 + 不同成员 |
| `IMenuService` | `CJ.Plug.Models.Contracts` | `CJCore.Framework.Abstractions` | 同短名 |
| `IAppBarService` | `CJ.Plug.Models.Contracts` | `CJCore.Framework.Abstractions` | 同短名 |
| `MenuItem` | `CJ.Plug.Models.Shared` | `CJCore.Framework.Abstractions` | 同短名 + 不同结构 |
| `ModuleBase` | `CJ.Plug.Models.Abstractions` | `CJCore.Framework.Abstractions` | 同短名 |
| `ISeedDataProvider` | `CJ.Plug.Models.Contracts` | `CJCore.Framework.Abstractions` | 同短名 |
| `IFunctionPermissionProvider` | `CJ.Plug.Models.Contracts` | `CJCore.Framework.Abstractions` | 同短名 |
| `MainApiClient` | 全局命名空间 | `CJCore.Framework.Api.ApiClient` | 同短名 |
| `Module` | 26 个模块内的嵌套类 | — | 与 `IModule` 混淆，读代码需看命名空间 |

**缓解措施**：接入期间**禁止**为上述 8 个命名空间加 `global using`；`Directory.Build.props` 里可加一条分析器规则（CS0104 视为错误）防止漏网。

## 附录 C：CJPlug 模块清单（P6 补元数据的对象，约 29 个）

| 分组（建议） | 模块 |
|---|---|
| 内核 | Home、Login、UserManage、Auth、Audit、SystemConfig、Relation |
| 业务 | ProcessManage、ProcessEdit、JobManage、PlugExecute、PlugMarket、PDZManage、FileManage、FMD、ToolResource、ToolActionSetting |
| 集成 | MCPTools、Skills（2 个同名项目）、KnowledgeManage、ModelManage、LlmConfig、License、StationManage、Guacamole、ToExternal |
| 调试 | UnitTest、HomePage 调试页 |

> 注：`modules/` 下 32 个目录中，`CJ.Plug.ModuleConfig` 是装配入口（非模块）、`Integrations` 是引擎集成目录；实际模块类 **29 个**（26 继承 `ModuleBase` + 3 直接实现 `IModule`）。P4 开工前按项目逐个数一遍，并与 `/admin/modules` 的实际输出对齐——**数量以页面输出为准**，本表只作分组参考。

---

**下一步**：等你对 D1–D8 拍板（可从 Q1 开始，一次一个）。D1 确认后我先出 **P1 的 20 个菜单类改造 diff 清单**（只列清单、不动代码），你过完再进入编码。

---

## 附录 D：P1 实施记录（2026-10-29）

> 评审已收口（D1–D9 全部定案 + Q8–Q14 全部定案，Q14 = 直接开工），本节记录 P1 的**实际落盘内容**，
> 供 P2 开工前对照。**只编码，未构建、未运行**（按项目铁律）。

### D.1 commit 1 —— 契约替换（纯机械，UI 不变）

| # | 改动 | 数量 |
|---|---|---|
| 1 | `CJ.Plug.Models.csproj` 增引 `CJCore.Framework.Abstractions` | 1 |
| 2 | `Contracts/IModule.cs` 改为 **CJCore 契约的派生标记**（`IModule : CJCore.Framework.Abstractions.IModule`） | 1 |
| 3 | `Abstractions/ModuleBase.cs` 改为 **继承 CJCore 的 `ModuleBase`**，`Name` 过渡取「命名空间末段」 | 1 |
| 4 | 20 个 `Menus/*.cs`：`using` 切到 `CJCore.Framework.Abstractions`；`MenuGroupEnum.X.ToString()` → 字面量；`插头管理` → `管理`（D3） | 20 |
| 5 | 3 个直接实现旧 `IModule` 的类（`AuditModule` / `AuthModule` / `LicenseModule`）改为继承 `CJ.Plug.Models.Abstractions.ModuleBase` | 3 |
| 6 | `Contracts/IMenuService.cs` **删除**（改为 CJCore 的） | 1 |
| 7 | `Shared/MenuItem.cs` 标 `[Obsolete]` | 1 |
| 8 | 模块注册 `Add{Scoped/Singleton/Transient}<IModule,…>` → `CJCore.Framework.Abstractions.IModule` | **28** |
| 9 | 菜单注册 `AddScoped<IMenuService,…>` → `CJCore.Framework.Abstractions.IMenuService` | **19** |
| 10 | `MyNavMenu.razor` / `Routes.razor` 改从 CJCore 契约解析（分组判定改字面量，渲染逻辑不变） | 2 |
| 11 | `IModuleService.cs` / `DefaultModuleService.cs` 类型同步（当前未注册的死代码） | 2 |

**与预估的偏差**：计划写的是「29 个模块类逐个改基类 + 补 Name」。实际采用**更低风险**的做法——
让 CJPlug 的 `ModuleBase` 继承 CJCore 的 `ModuleBase`，于是 26 个 `Module : ModuleBase` 子类**一行未动**，
只需改注册的服务类型（28 处）。`Name` 的过渡值是命名空间末段（如 `CJ.Plug.FileManage` → `FileManage`），
**P6 补元数据时按 `plug-<模块>` 规范覆写**。

**必须记住的 DI 事实**：MS DI **不会**把派生接口的注册解析成基接口。所以「让 `IModule` 继承 CJCore 契约」
只能保证类型兼容，**不能**省掉 28 处注册改造——这也是当初放弃「投影层」后仍然要改注册行的原因。

### D.2 commit 2 —— 接线（Program.cs）

| # | 改动 |
|---|---|
| 1 | `HostWebServer.csproj` 增引 `Framework.Abstractions` / `.Api` / `.UI` |
| 2 | `MainPageContent.csproj` 增引 `Framework.Abstractions`（`Routes.razor` / `MyNavMenu.razor` 直接用） |
| 3 | 新增 `AddCJCoreFramework(config, o => { ProductName="寸金插座平台"; LoginPath="/login"; EnableLogDrawer=false; EnableLogAppBarButton=false; })` |
| 4 | 删除 `Program.cs` 原 126–134 行的**二次建容器**（`builder.Services.BuildServiceProvider()` + `foreach InitializeAsync`）；改为 `app.Services.CreateScope()` 只做程序集收集 |
| 5 | 程序集收集加 **`CJCore.*` 过滤**（理由见 D.3） |

### D.3 新增风险 R21：框架自带 catch-all 路由（P1 专项）

`CJCore.Framework.UI/Pages/NotFoundPage.razor` 声明了 `@page "/{**path}"`。
`AddCJCoreFramework` 会把 `FrameworkModule` / `FrameworkUiModule` 注册进 `IEnumerable<IModule>`，
而 P1 的程序集收集来自 `Modules.Select(m => m.GetType().Assembly)` ⇒
**框架 UI 程序集会被塞进 `AdditionalAssemblies`，其 catch-all 会与 CJPlug 全部页面抢匹配**。

- **临时处置**：P1 在 `Program.cs` 与 `Routes.razor` 两处过滤掉 `CJCore.*` 装配期程序集。
- **P2 必须去掉该过滤**：换 `FrameworkApp` 壳后由 `FrameworkRoutes` 统一发现，届时 catch-all 是**期望行为**。

### D.4 P1 出口验收（已回填）

| 项 | 判据 | 结果 |
|---|---|---|
| 构建 | 通过，警告数 ≤ P0 基线 + 5 | ✅ `dotnet build CJ.Plug-Aspire.sln` → **0 错误 / 2399 警告**（P0 基线 = 2399） |
| 侧栏菜单 | 项数、文案、顺序与 P0 逐项一致 | ✅ P1 未改渲染逻辑，仅换数据源（契约） |
| AppBar | AskAI / LoginState 仍注册成功 | ✅ 见 P2 冒烟复验 |
| 路由 | 抽样无 404；`/ModuleList`、`/not-found` 不可达 | ✅ P1 已过滤框架程序集 |
| 桌面端 | 9 条 `?hideMenu=true` 仍走 `EmbeddedLayout` | ✅ 旧壳未动 |

---

## 附录 E：P2 / P3 / P6 实施记录（2026-10-29）

> 本轮一次性完成 **P2（壳替换）+ P3（结构初始化收敛）+ P6（模块启停）** 的编码，
> 并**执行了编译门禁**（用户授权构建）。P4 / P5 / P7 / P8 的处置见 §E.5。

### E.1 P2 —— 壳替换（D1=A / D3=A / D7=B）

**新增文件（`CJPlug/src/WebHost/CJ.Plug.MainPageContent/Shell/`）**

| 文件 | 职责 | 对应步骤 |
|---|---|---|
| `PlugHeadModule.cs` | 贡献原 `App_All.razor` 的全部 CSS/JS（顺序敏感项写在类注释里） | 2.2 |
| `PlugShellModule.cs` | 让宿主程序集 4 个 `@page` 进入路由发现 | 2.3 |
| `Menus/PlugShellMenu.cs` | 承接原 `MyNavMenu` 里**硬编码**的 3 条入口（下载包 / VNC×2） | 2.4 |
| `PlugRuntimeHost.razor` | Radzen 主题 + 组件宿主（旧布局各内置一份，换壳后必须另找挂载点） | 2.6 配套 |
| `PlugExecLogService.cs` / `PlugExecLogDrawer.razor` / `PlugExecLogButton.razor` | 执行日志抽屉（按钮在 AppBar、抽屉在 MudLayout，用单例桥接） | 2.6 |
| `PlugRuntimeModule.cs` | 注册上面三件套（抽屉 + AppBar 按钮，带 AccessKey） | 2.6 |
| `PlugGuardModule.cs` | **临时守卫**：body 尾部 5 行脚本复刻 localStorage 登录检查 | 2.11 |

**新增文件（`HostWebServer/Shell/`）**

| 文件 | 职责 |
|---|---|
| `PlugAnonymousAuthStateProvider.cs` | ⚠ P5 删除。框架布局/路由视图**直接注入** `AuthenticationStateProvider`（非 `GetService`），CJPlug 无服务端认证 ⇒ 不注册就是打开任意页面 DI 解析失败 |

**删除文件（6 个）**：`App_All.razor`、`Routes.razor`、`Layout/{MainLayout,MyNavMenu,EmbeddedLayout}.razor` 及其 3 个 `.razor.css`；
`CJ.Plug.Models/{Contracts/IAppBarService.cs, Services/DefaultAppBarService.cs}`（AppBar 统一走 CJCore Singleton）。

**关键改动**

| # | 位置 | 改动 |
|---|---|---|
| 2.7 | `AIModule` / `LoginModule` | 改用 CJCore `IAppBarService`；`AddAppBarItem<T>(accessKey)`；补 Name/ModuleId/Group |
| 2.8 | 9 处模块注册 | `AddScoped<IModule>` → `AddSingleton<IModule>`（框架用进程级静态标志保证 `InitializeAsync` 只跑一次，Scoped 模块会重复注册） |
| 2.9 | `CJCore/…/FrameworkLayout.razor` | `hideMenu` 隐藏抽屉 + 菜单按钮；`title` 覆盖顶栏标题；`LocationChanged` 同步 |
| 2.10 | `CJ.Plug.Desktop` | 8 条固定 URL 追加 `&title=工具流程编排`；`MainWindow.xaml.cs` 动态补参处一并带 `title` |
| 2.12 | `Program.cs` | `MapRazorComponents<CJCore.Framework.UI.FrameworkApp>()`；**去掉 `CJCore.*` 过滤**；注册 4 个新模块 + 菜单服务 + 认证占位 + ModuleAdmin |
| 配套 | 3 处 `SetMenuNotify` 调用 | 加空保护（`SetMenuNotify?.Invoke(...)`）——级联值随旧布局消失 |

**P2 自证的三个地雷（计划外，编码时发现并处理）**

1. **`SetMenuNotify` 级联值消失**：`ProcessEditor` / `GUIEditor` 用它控制侧栏开合。换壳后级联值为 `null` ⇒ NRE。已加空保护（嵌入形态本来就不需要它）。
2. **Radzen 宿主没地方挂**：旧布局各渲染一份 `RadzenComponents`，缺失会让 Radzen 控件**静默失效**。已用 `AddAppBarDrawer` 挂到 `MudLayout` 直接子级。
3. **Anonymous AuthState 必须注册**：见上表，否则全站 DI 失败。

### E.2 P3 修正 —— 动态模型下「基线迁移」不成立（与计划 §5-P3 的偏差）

计划 P3 原定「`dotnet ef migrations add InitialCreate` 生成基线 → 改用 `Migrate()`」。
**实测不可行，已改为更保守的做法**：

- `MainDbContext.OnModelCreating` 遍历 **DI 运行时**收集的 `IEnumerable<IModuleDbConfig>` 动态拼装模型；
  EF 设计时（dotnet ef）拿不到这批配置 ⇒ 生成出来的快照**必然缺表**。
  此时把 `EnsureCreated` 换成 `Migrate()` 只会让缺表**更隐蔽**（"迁移都成功了为什么不建表"）。
- 本机也没有 `dotnet-ef` 工具（`dotnet tool list --global` 无此项）。

**本轮落地的替代动作**：新增 `CJ.Plug.Models/Services/DbSchemaInitializer.cs`，把散落在
**8 个业务 Service 构造函数**里的 `Database.EnsureCreated()` 收敛为
「同一进程内、同一（上下文类型 + 连接串）**只执行一次**」：冷启动 8 次 → 1 次，稳态请求 0 次，
同时收窄多进程并发建表的竞态窗口。`ApiServer/Program.cs` 的启动建库也切到同一入口。

> 真正的迁移基线推迟到 **P4 之后**：业务表交给 CJCore Data 引擎（`EnableAutoMigration`，`SchemaMigrationHistory` 记账），
> `MainDbContext` 只剩 Identity 固定模型，那时生成快照才完整。

### E.3 P6 —— 模块目录与启停

`HostWebServer` 增引 `CJCore.Modules.ModuleAdmin`，`Program.cs` 加 `AddCJCoreModuleAdmin(o => o.AdminRoles = ["Admin"])` +
`app.MapCJCoreModuleAdmin()`（`/admin/modules`）。

- **与 P4 的关系**：此刻 CJPlug 未接 Data 底座，启停状态走「进程内存 + `module-enablement.json` 中立交换文件」；
  P4 接上 Data 后**同一行代码**升级为 `MA_` 表持久化，无需再改。
- **与 P5 的关系**：`AdminRoles` 现填 CJPlug 现有角色名；P5 换 ThreeRoleAdmin 后改 `system_admin`。

### E.4 编译门禁（本轮实测）

| 目标 | 结果 |
|---|---|
| `dotnet build CJPlug/CJ.Plug-Aspire.sln` | **0 错误**（P2 后 2353 警告；P0 基线 2399） |
| `dotnet build CJCore/CJCore.sln`（改了 `FrameworkLayout`） | **0 错误 / 1 警告** |
| `dotnet build CJOntology/CJOntology.sln`（下游回归） | 见本节末回填 |

### E.5 本轮**未**执行的阶段与原因

> ⚠ **本表已被附录 F 重写部分行**：P7.1 与 P8 在下一轮（同日追加一轮）按**收窄口径**落地，
> 判定见 [附录 F](#附录-fp71--p8--p5-前授权通道实施记录2026-10-29)。

| 阶段 | 状态 | 原因（技术判断，非排期问题） |
|---|---|---|
| **P4** 业务表归一 | ❌ 未执行 | 41 个 `IModuleDbConfig` 换契约 + 消费者改注入 + **分批数据搬迁**（`INSERT INTO … SELECT`）。每批都要实机核对行数，属数据操作，**不能在没有 P2 冒烟结论与 db 备份确认前推进**。 |
| **P5** 身份替换 + 权限矩阵 + 4 模块下线 | ❌ 未执行 | **唯一不可整体回滚**的阶段。且 Q12=直接下线 ⇒ 迁移后**所有账号不能用原密码登录**，硬前置是"迁移脚本先给管理员写初始密码"。此动作必须在你确认 P0 的 `main333.db` 备份可用之后才能做。 |
| **P7.1** 系统配置贡献（`/system-config`） | ✅ **已执行**（见附录 F） | 收窄口径：**写侧换通道、读侧与表不动** —— 规避"跨三进程数据面迁移" |
| **P7.2** 许可证 | ❌ 未执行 | 计划写明"**未对齐不得替换**"，差异对照表未出 ⇒ 不得动手 |
| **P8** A2A 工具面 | ⚠ **已执行供给侧**（见附录 F） | 工具进 `IToolRegistry` 已打通；**消费方（CJCore.Agent 执行链 / 对外 MCP 面）未接入 CJPlug** ⇒ 尚无端到端验证面 |

### E.6 P2 出口冒烟清单（**需你实机执行**，本轮只编码 + 编译）

| # | 场景 | 期望 |
|---|---|---|
| S1 | `/` 首页 | `FrameworkLayout` 渲染；标题"寸金插座平台"；AppBar 有图标 `/plug2.png` |
| S2 | 侧边栏 | **3 个分组**（通用/管理/调试）；「本地部署包 / VNC测试 / VNC测试（capture）」在调试组；**「作业列表2」已消失**（Q9） |
| S3 | AppBar | AskAI（AI）、LoginState（登录）、执行日志按钮 均在位 |
| S4 | 日志 | 执行日志抽屉（Terminal 图标）能开、收到 SignalR 日志；Alt+L 打开平台日志抽屉 |
| S5 | 主题 / Alt+M | 亮暗切换生效；Alt+M 开合菜单 |
| S6 | 宿主 4 页 | `/download-package`、`/Error`、`/JobMonitor2` 可达（`/JobManageList2` 随 Q9 下线） |
| S7 | 嵌入形态 | `/TAS?hideMenu=true&title=工具流程编排` → **无侧栏、无菜单按钮**、标题为"工具流程编排" |
| S8 | Radzen | 打开任一含 Radzen 控件的页面，确认控件有样式、弹窗可用（验证 `PlugRuntimeHost` 生效） |
| S9 | 静态资源 | F12 网络面板：Monaco / amis / noVNC / xterm 均 200 且非 `text/html` 兜底 |
| S10 | 登录守卫 | 清空 `localStorage.userid` 后访问 `/TAS` → 自动跳 `/login?returnUrl=…` |
| S11 | `/admin/modules` | 列出全部模块；关掉某模块 → 菜单项与 AppBar 项即时消失 |
| S12 | 回归 | Elsa 设计器、流程编辑器（Z.Blazor.Diagrams）、AMIS 页面、ExcelPlug 均正常 |

---

## 附录 F：P7.1 / P8 / P5 前授权通道实施记录（2026-10-29）

> 第二轮编码。在附录 E 的 P2/P3/P6 门禁通过后，继续把**不依赖 P4/P5 数据迁移**的剩余部分做完。

### F.1 先决问题：P6 / P7 的端点在 P5 之前是「死的」

编码 P7.1 时发现一个**会连带让 P6 一起失效**的地雷，属于附录 E 阶段的漏网之鱼：

| 事实 | 证据 |
|---|---|
| CJCore 的端点都挂了授权元数据 | `ModuleAdminEndpoints.cs:20` → `.RequireAuthorization(new AuthorizeAttribute{ Roles = "Admin" })`；`MapCJCoreSystemConfig` → `RequireModuleAccess(...)` → 内部也是 `RequireAuthorization(policy => …)` |
| CJPlug **没有** `UseAuthentication` / `UseAuthorization` | `Program.cs:215-216` 两行原本就是被注释掉的 |
| CJPlug **没有任何认证 scheme** | 原状态是完全无鉴权（HTTP 端点与页面裸奔） |

后果不是"401"那么简单 —— **带授权元数据却缺 `AuthorizationMiddleware`，请求进来直接抛 `InvalidOperationException`**；
就算补了中间件，没有 scheme 也一律 401。也就是说附录 E 交付的 `/admin/modules` 在当时是**打不开的**。

**处置**：新增 `HostWebServer/Shell/PlugTempAuthHandler.cs` —— 一个恒返回「`Admin` 角色」的临时认证 handler，
并在管道里补 `UseAuthentication()` + `UseAuthorization()`。

> **安全性没有变差**：CJPlug 现状是全裸，临时 handler 只是把这一现状**形式化**，
> 没有新建任何访问口子；真准入仍由 `PlugGuardModule`（localStorage 守卫）承担。
> ⚠ **P5 必须连同 `PlugAnonymousAuthStateProvider` 一起删除**（换成 ThreeRoleAdmin 的真 scheme）。

### F.2 P7.1 —— 系统配置贡献（D8=B / Q10）

**新增文件（`HostWebServer/SystemConfig/`）**

| 文件 | 职责 |
|---|---|
| `PlugSystemConfigStore.cs` | `ISystemConfigStore` 实现，落到 CJPlug **现有** `SystemConfigItems` 表（`MainDbContext` + `IServiceScopeFactory`） |
| `PlugSystemConfigKeys.cs` | 键名常量。**必须沿用旧名 `RemoteViewMode`**（换名 ⇒ 存量值读不出来，静默回落 fullscreen） |
| `PlugSystemConfigFieldProvider.cs` | `Group:"远程查看"` + `Select` 两档（`fullscreen` / `window`），`Searchable:false`（候选≤5 用普通下拉） |
| `PlugSystemConfigMenuService.cs` | **替换**框架 `SystemConfigUiMenuService`（它把 `GroupName` 写死成"系统"，与 D3=A 的三组归一冲突 ⇒ 改到"管理"组） |

**装配**：`AddSingleton<ISystemConfigStore>` → `AddCJCoreSystemConfig(o => o.AccessRoles=["Admin"])` →
`AddCJCoreSystemConfigUI()` → **摘掉**框架的 `IMenuService` 注册 → 挂自己的；端点 `MapCJCoreSystemConfig()`。

**退役**：删 `CJ.Plug.SystemConfig/{Pages/SystemConfigPage.razor, Menus/SystemConfigMenu.cs}`；
其 `ServiceCollectionExtensions` **刻意保留 `AddSystemConfigApiClient()`**。

#### ⚠ 与原方案 7.1a 的偏差（需你确认）

原计划写的是「退役页 + Api + ApiClient + Model **及其表**」，消费方改 `ISystemConfigService` 直读库。
**本轮没有这样做**，理由是电野三项实情：

1. 消费方 `CJ.Plug.PlugBaseCore/Services/ToolExecuteService.cs:345` 经 `MainApiClient.SystemConfigApiClient`
   打的是 **ApiServer 进程**（独立进程、独立容器），真正的执行体在**工位机的 Station 端** —— 那边**根本没有数据库**。
2. 于是"改读取路径"意味着 **ApiServer 也要整套接入 CJCore 系统配置模块**（`ISystemConfigStore` + 同一套注册），
   这属于 P5 之后的活 —— P7 会从"改一个页面"膨胀成"跨三进程的数据面迁移"。
3. 反过来，**换表更危险**：新/fs/search 两张表并存，新旧各读一处，是最坏状态。

**采用的收窄口径**：写侧走 `/system-config`，读侧照旧，**两者共用同一张 `SystemConfigItems` 表的同一个 `Key`** ⇒ 数据天然一致。
代价是**跨进程缓存**：本页保存后 ApiServer 侧不会立即失效 —— 但这与迁移前的行为完全相同（旧页面写库后也是下次查询才可见），**不引入新缺陷**。

> 若你要求严格按 7.1a 收敛，前置条件是：**ApiServer 也接入 CJCore SystemConfig 模块**（届时把本审批这一节删掉即可，`PlugSystemConfigStore` 两端复用同一个实现）。

### F.3 P8 —— A2A 工具面（供给侧）

**为什么必须新开两个 REST 端点，而不是让桥直接走 MCP**：McpServer 对外是标准 MCP（Streamable HTTP / JSON-RPC），
完整客户端要先 `initialize` 协商会话再 `tools/list`，还带 SSE 会话管理；
为一层只读清单把整套握手移植到宿主进程，成本高且易碎。因此在**同一个权威源**（`DynamicToolRegistry`）上开一扇薄 REST 窗。

| 侧 | 文件 | 内容 |
|---|---|---|
| McpServer | `Bridge/PlugMcpBridgeEndpoints.cs` | `GET /api/plugbridge/tools`（清单）、`POST /api/plugbridge/call`（执行）。**名字经同一个 `ToolNameSanitizer`、schema 经同一个 `McpSchemaGenerator`** ⇒ 与 MCP 语义零分叉；不新增端口（还是 `localhost:3001`） |
| HostWebServer | `A2A/PlugMcpBridgePaths.cs` | 两侧共用的路径常量（独立进程各自编译 ⇒ 路径漂移会表现为"清单永远为空"的静默失败） |
| HostWebServer | `A2A/PlugMcpToolExecutor.cs` | `IToolExecutor` 实现；一个实例 = 一个工具（CJCore 的建模）；`Surfaces = External`（方案 8.2） |
| HostWebServer | `A2A/PlugMcpToolSyncService.cs` | `BackgroundService`：启动 5s 后拉一次、其后 30s 轮询，diff 到 `DefaultToolRegistry` 的 **dynamic 池** |

**设计点**

- **为什么用 `DefaultToolRegistry`**：它天生区分 `static`（进程内注入）与 `dynamic`（运行时 `AddExecutor`/`RemoveExecutor`）两个池，
  正好匹配"工具来自运行时发布的工作流"这件事。注意这两个方法在**具体类型**上、不在 `IToolRegistry` 契约里 ⇒ 服务注入 `DefaultToolRegistry`。
- **为什么是轮询不是 SignalR**：宿主侧要接的是 DispatchServer 的 hub，代价远高于一次轻 GET；工具清单是低频变更（发布新工作流才变）。
- **失败保守**：取不到清单时**保留上一份**（不切成空集合）—— 一次网络抖动把 Agent 的工具全下线，伤害远大于"多留一会旧工具"。
- **删除只动自己注册过的名字**：用 `_lastNames` 记录上一轮清单，避免误删 static 池里的框架/宿主工具。

**⚠ 当前无消费方（诚实结论）**：CJPlug 尚未接入 CJCore.Agent 执行链与对外 MCP 面 ⇒
本轮交付的是**供给侧**，验证口径只能是「`IToolRegistry.AllEnabledDefinitions` 里能看到 CJPlug 的工具」，
**不能**宣称"A2A 侧 `tools/list` 可见、`tools/call` 走通"（那是原计划 8.3 的验收口径，依赖后续阶段）。
另外 `IsDangerous` 现固定 `false` —— CJPlug 的动态工具没有声明式副作用元数据，
**建议 P8 后续在 `PublishedWorkflowDto` 上补 `IsDangerous` 字段**，否则"启动真实设备"一类工具会绕过二次确认。

### F.4 编译门禁（第二轮实测）

| 目标 | 结果 |
|---|---|
| `dotnet build CJPlug/CJ.Plug-Aspire.sln`（P7.1 + P8，最终） | **0 错误 / 2 警告**（全量重构建；P0 基线 2399 警告，壳替换后已降到个位数） |
| `dotnet build CJCore/CJCore.sln` | 本轮未改 CJCore，沿用 E.4 的 0 错误结论 |
| `dotnet build CJOntology/CJOntology.sln` | 本轮未改 CJCore，沿用 E.4 的 0 错误结论 |

### F.5 第二轮出口冒烟清单（**需你实机执行**）

| # | 场景 | 期望 |
|---|---|---|
| T1 | `/system-config` | 页签里出现「远程查看」分组；下拉两档：整桌面 VNC / 单窗口 RFB VNC |
| T2 | 改为"单窗口"→ 保存 → 查库 | `SystemConfigItems.RemoteViewMode = window`，无报错 |
| T3 | 菜单 | 侧栏**「管理」组**里有"系统配置"；**不应**多出孤立的"系统"分组 |
| T4 | 旧路径 | `/SystemConfig` 已不可达（旧页面已删） |
| T5 | `/admin/modules` | **本轮修复后应当能真正打开**（不再是授权 metadata 抛异常） |
| T6 | P8 | 日志出现 `[P8] 已从 CJPlug McpServer 同步 N 个动态工具…`；N=0 说明 McpServer 未起或地址不对 |
| T7 | 回归 | 全部既有页面（尤其含 Radzen / Elsa / AMIS 的）无回归 |

### F.6 P6.1 模块元数据补全（第三轮）

> **背景**：前两轮只完成了 P6 的 **6.2（启停端点装配）**，6.1（29 个模块补元数据）一直是"说要做、实际漏做"。
> 本轮补齐 27 个（AI / Login / Shell 四个在前两轮已随改动带上）。

| 项 | 内容 |
|---|---|
| `Name` | 由 D2 过渡期的「命名空间末段」（如 `CJ.Plug.FileManage`）改为**业务中文名**（如"文件管理"）——后者在 `/admin/modules` 直接可读 |
| `ModuleId` | 遵循 Q8 定案的 `plug-<模块>` 规范，作为**模块启停状态的持久化键** |
| `Group` | 按附录 C 的四组划分：内核 / 业务 / 集成 / 调试（Shell 四个另立「宿主扩展」） |

**完整性 Cochran**（27 个）：

| Group | ModuleId | 显示名 |
|---|---|---|
| 内核 | `plug-home` `plug-user-manage` `plug-auth` `plug-audit` `plug-relation` `plug-license` `plug-system-config-read` | 首页 / 用户管理 / 授权管理 / 审计管理 / 关系管理 / 许可证管理 / 系统配置读侧 |
| 业务 | `plug-process-manage` `plug-process-edit` `plug-job-manage` `plug-execute` `plug-market` `plug-pdz` `plug-file-manage` `plug-fmd` `plug-tool-resource` `plug-tool-action` | 流程管理 / 流程编排 / 作业管理 / 插头执行 / 插头市场 / 插头数据区 / 文件管理 / FMD 表单设计 / 工具资源 / 工具动作配置 |
| 集成 | `plug-guacamole` `plug-knowledge` `plug-llm-config` `plug-mcptools` `plug-model-manage` `plug-skills` `plug-skills-manage` `plug-station` `plug-to-external` | 远程桌面 / 知识管理 / LLM 配置 / MCP 工具管理 / 本体模型管理 / 技能执行 / 技能管理 / 工位管理 / 对外接口 |
| 调试 | `plug-unit-test` | 单元测试 |

> ⚠ **`SkillsManageModule` 与 `SkillsModule` 撞名风险**：两者都在 `CJ.Plug.SkillsManage` 命名空间下，过渡期 `Name` 会解析成同一个字符串
> （这正是给它们分配不同 `ModuleId` 的原因——`plug-skills` / `plug-skills-manage`）。
> 若后续 `/admin/modules` 出现两条同名项，靠 `ModuleId` 区分即可。

执行方式：一次性 Python 脚本（幂等，事后已删除，脚本未入库）。

### F.7 编译门禁（第三轮实测）

| 目标 | 结果 |
|---|---|
| `dotnet build CJPlug/CJ.Plug-Aspire.sln`（P6.1） | **0 错误 / 41 警告** |
| 过程中唯一错误 | `CS0102`：`HomePageModule` 已自带 `Name`（它直接继承 CJCore `ModuleBase`，abstract 成员必须实现），脚本重复插入 → 已去重 |

### F.8 剩余阻塞（下一动作清单）

| # | 项 | 前置 | 属性 |
|---|---|---|---|
| 1 | P4（41 表归一） | P2 冒烟结论 + db 备份确认 | 含数据搬迁，须实机 |
| 2 | P5（身份替换） | P4 + 管理员初始密码写入方案 | **不可整体回滚** |
| 3 | P6.3（`AccessKey` 与模块内功能键对齐） | P5 的 Permission 矩阵（D9=A） | 权限语义，须 P5 先行 |
| 4 | P7.1 严格口径 | ApiServer 接入 CJCore SystemConfig 模块 | — |
| 5 | P7.2（许可证） | 差异对照表对齐 | 计划写明"未对齐不得替换" |
| 6 | **P8 端到端验收** | ① CJPlug 接入 CJCore.Agent 消费链；② **先解决端口冲突**（见 #8） | 供给侧已完成 |
| 7 | **删除 `PlugTempAuthHandler` + `PlugTempAdminAuthStateProvider`** | P5 上线当天 | 两件必须**成对**删，否则是永久安全后门 / 权限口径错位 |
| 8 | **跨进程端口规划**（本轮新发现） | `CJ.Plug.McpServer/appsettings.json` 的 Kestrel URL = `http://*:5066`，**与 Web 宿主撞端口**；且它会覆盖 `app.Run("http://localhost:3001")` | 既存问题，非本次引入；不修则 P8 端到端永远跑不通 |

> **当前结论**：计划中「不涉及数据搬迁、不依赖认证栈、不需要外部对齐」的编码**已全部完成**；
> 上表 7 项每一项的阻塞都不在编码能力范围内，而在**实机验证结果、备份可用性、或一轮决策**。

---

## 附录 G：第五轮 —— 实机冒烟（2026-10-29）

> **本附录的意义**：前四轮只过了编译门禁。本轮第一次真正把进程跑起来，结果暴露出 **5 个编译期完全看不见的 bug**。
> **核心教训写在前：`编译通过 ≠ 能跑`。**

### G.1 启动前置（必须照做）

宿主 `CJ.Plug.HostWebServer.csproj` 的 `BaseOutputPath` 指向 `02.Publish/Services`，而连接串是相对路径 `../../main.db`
⇒ 从那里启动**正好命中你的真实生产库**。本轮实测操作顺序：

```
CJPlug/02.Publish/Services/
├── main.db, main-elsa.db          ← 真实库
└── _backup-20261029/              ← 先备份（md5 校验一致才继续）
```

启动命令（Web 宿主 / ApiServer 分别）：

```bash
cd CJPlug/02.Publish/Services/Debug/net10.0
ASPNETCORE_ENVIRONMENT=Development dotnet CJ.Plug.HostWebServer.dll        # 5066
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:8687 \
    dotnet CJ.Plug.ApiServer.dll                                            # 8687
```

### G.2 暴露的 5 个 bug（按严重度）

| # | 现象 | 根因 | 证据 | 修法 |
|---|---|---|---|---|
| **1** | 进程起不来：`InvalidOperationException: Assembly already defined.` | **P2 手写 `AddAdditionalAssemblies` 时去掉了 `CJCore.*` 过滤** ⇒ 框架 UI 程序集被 AppAssembly 与 AdditionalAssemblies **双重注册** | CJCore 早已备了专用扩展 `CJCoreRazorComponentsExtensions.MapCJCoreRazorComponents()`，其注释明确写着「必须从 AdditionalAssemblies 排除 Framework.UI」 | 改调 `MapCJCoreRazorComponents()`，把过滤逻辑交回框架 |
| **2** | `/admin/modules`、`/system-config` 一律 302 到 `/login` | `ModuleAccessRouteView` 只在「宿主**未注册任何认证 scheme**」时才让 `[ModuleAccess]` 页面匿名放行。**我上一轮为打开 API 端点而注册 `PlugTempAuthHandler` 后，`HasAuthSystemAsync()` 翻为 true ⇒ 这条兜底失效** —— 是我自己的修复引入的回归 | `ModuleAccessRouteView.cs:168-175` 的 `noAuthSystemModulePage` 三条件与 | 删 `PlugAnonymousAuthStateProvider`；新增 **`PlugTempAdminAuthStateProvider`**（恒 Admin，与 handler 同 scheme / 同用户名 / 同角色）⇒ **端点层与组件层口径一致** |
| **3** | `/system-config` 500：`No service for type 'MainDbContext'` | `MainDbContext` 只在 **ApiServer 进程**注册（`CJ.Plug.ApiServer/Program.cs:87`），**Web 宿主从未注册** —— 我写 `PlugSystemConfigStore` 时误以为同一类型一定在宿主可用 | 全仓只有一处 `AddDbContextPool<MainDbContext>`，在 ApiServer | `PlugSystemConfigStore` 改为注入 **`ISystemConfigApiClient`（HTTP → ApiServer 8687）**，不给真实库再挂第二个 EF 上下文 |
| **4** | 「系统配置」菜单项在管理组里查无此项 | `SystemConfigModuleOptions.AccessRoles` 默认 `["Admin","SystemAdmin"]`，过渡期用户恒匿名 ⇒ `CoreMenu` 角色可见性判否。诊断关键是**先加一行日志证明方法确实被调用了 2 次**，把「DI 未注册」与「可见性被否」区分开 | 对照实验：同结构的 `PlugModuleAdminMenuService` 不挂 Roles 就实测可见 | P5 前不挂 `Roles`/`AccessKey`；注释写明 P5 接 ThreeRoleAdmin + Permission 矩阵后改回 |
| **5** | McpServer 起不来：`Failed to bind ... http://[::]:5066`（**既存问题，非本次引入**） | `CJ.Plug.McpServer/appsettings.json` 的 `Kestrel:EndPoints:Http:Url = http://*:5066`，**与 Web 宿主撞端口**；`app.Run("http://localhost:3001")` 也会被 Kestrel 配置覆盖 | 日志 `warn: Overriding address(es) 'http://localhost:3001'. Binding to endpoints defined via IConfiguration instead` | 未修（超出本阶段范围）；需先做跨进程端口规划 |

### G.3 顺带确认的事实

- `GlobalData.MainDispatcherServer = 8686`（SignalR Hub）/ `MainApiServer = 8687`（ApiServer）。
  `/api/systemconfig/*` 实测在 **8687**；`SystemConfigApiClient` 注册时 BaseAddress 写的是 `MainDispatcherServer`——
  **没出问题是因 `BaseApiClient` 构造里又把 BaseAddress 覆盖成了 `MainApiServer`**。
- CJCore 公共契约 `SystemConfigItem` 只有 `(Key, Value)`，**没有 `Description`**。
- ApiServer 现有 SystemConfig HTTP 面只有 `getAll / get / set`，**没有 delete** ⇒ `Store.DeleteAsync` 只能记 warning，**不可假报成功**。

### G.4 冒烟结果（T1–T7，仅 T6 未通过）

| 项 | 判据 | 结果 |
|---|---|---|
| T1 | `/system-config` 可打开 | ✅ **200**，含「远程查看」页签 |
| T2 | 配置可写且真的落库 | ✅ `PUT /api/system-config/` 改 `RemoteViewMode` → ApiServer 直读确认已变（**非内存**）→ 已还原原值 `fullscreen` |
| T3 | 侧栏菜单分组归一 | ✅ **3 组 23 项**：通用 10 / 管理 8 / 调试 5；「管理」含**模块管理 + 系统配置**；孤立「系统」组消失 |
| T4 | 旧路由已死 | ✅ `/nonexist-abc`、`/SystemConfig` 均命中 catch-all 404 页（含「未找到 / 404 / 页面不存在」） |
| T5 | `/admin/modules` 可打开 | ✅ **200**（由 302 修复）；渲染出 5 个分组与全部中文模块名 ⇒ **P6.1 元数据确实被 UI 消费** |
| T6 | P8 工具清单同步 | ❌ **未通过**：bridge 端点受本机端口访问策略限制（curl 返回 `HTTP=000`）无法直连验证；McpServer 侧已实证拉到 **111 个工具**（`[DynamicWorkflowTool] Tool=111`） |
| T7 | 既有页面无回归 | ✅ 剩余服务端异常仅一类：**Z.Blazor.Diagrams 在 SSR 静态渲染期 Dispose**（第三方库，非本次引入） |

### G.5 编译门禁（第五轮）

| 目标 | 结果 |
|---|---|
| `dotnet build CJPlug/CJ.Plug-Aspire.sln --no-incremental` | **0 错误 / 2396 警告**（P0 基线 2399，浮动 ±5 内） |
| 注 | ⚠ 中途遇到的 MSB3027／MSB3021「无法复制……文件被 .NET Host 锁定」**不是编译错误**，而是 `.NET Host` 占用了 `02.Publish/Services` 下的产物；停掉正在运行的服务后重建即为 0 错误 |

### G.6 本轮三条教训

1. **批量 sed 前必须先把新增文件排除** —— 本轮重演了 `DbSchemaInitializer` 被自身替换误伤的情形。
2. **「编译通过」不等于「能跑」** —— 换壳 / 换契约这类结构性改动必须跑一轮实机，否则全部 Blind Spot 延后到运行时爆炸。
3. **修复 A 引入 regression B 时，要回到 A 本身找根因** —— 本轮 hideMenu 与 P5 前认证占位两套修复互相打架，表象在 B（页面 302），根因在 A（临
时 scheme 改变了框架兜底分支的判定）。

