# （进行中）CJPlug 接入 CJCore 实施计划

> 状态：进行中（P0–P9 + P13 完成；**P9 + P9.1 已完成** —— 唯一宿主 `CJPlug.Server` 上线，**所有回退形态 exe 已下线**（D2=B，附录 Q.8）；**P13 已完成** —— 死代码三件套清理 + `DeleteByFilter`/`IsDangerous` 两处缺陷收口 + 内部密钥读侧实测 100%（附录 U）；**P7.1a 已完成** —— SystemConfig 四项目退役 + ElsaStudio 退役 + 配置读写收敛 CJCore（附录 V，方案文档已翻转"已完成"）；**P7.1b 已完成** —— Amis 120 个示例页构建期排除（附录 W，B 批次的前置收口，页面门控缺口 151→31 页）；剩 **P7.2 许可证**（等授权语义）、**B 批次覆盖面**（用户拍板**缓做**，仅剩 B-1/B-2/B-3 三项）　|　日期：2026-10-29（**2026-10-30 更新**）　|　作者：旺财　|　位置：`CJPlug/docs/`
> **⚠ 2026-10-30 进度更正（本轮）**：
> ① **P5 身份替换已完成**（P5.1~P5.9 + P5.8 C1/C2 收口，见 `（已完成）D-P5-6 Cookie会话绑定方案-20261030.md`）；
> ② **P6.3 权限键对齐已完成**（见 `（已完成）P5.5第三批+P6.3 权限声明归口方案-20261030.md`）；
> ③ **P6.3 的"矩阵无接口行"残留已彻底解决** —— 原记载「端点扫描在 CJPlug 托管形态下失效」是**错误归因**，真因与修法见 `CJCore/docs/（已完成）策略②接口权限键可授性方案-20261030.md`（D1=A / D6=C / D4，V1~V14 全部实测通过）；
> ④ **H.6 第 7 项（删 `PlugTempAuthHandler` + `PlugTempAdminAuthStateProvider`）已执行**（P5 收尾批，两个文件已物理删除）；
> ⑤ **P8 供给侧零数据已量化定位**（见附录 L）：链路三跳全通，工具数为 0 是 `MCPTools` 表 0 条记录的**数据空态**，非缺陷。
> ⑥ **P8 端到端验收已执行（D1=B，见附录 M）**：发布 / 同步 / 发现 / 删除 **四步全部实测通过**（`/api/plug-agent/tools` 由 `[]` → 1 条 `P8_E2E_Tool`）；**调用失败** —— 根因是 Elsa 保存链（`DeleteByDefinitionId` 405 被吞 + 保存结果不校验 ⇒ 定义未入库 ⇒ 执行不存在的定义 ⇒ 500）。数据已全部清理复原（与备份逐表核对一致）。
> ⑦ **P8 调用链已打通（D7=A）+ P9 宿主中立已完成**：`/elsa/api` 认证交回 Elsa 后 `addTool → call` 全绿；宿主收敛为 `CJPlug.Server`（ApiServer 降级类库），部署链（AppHost 清单 / 打包断言 / 启动脚本）已同步（附录 O / P）。
> ⑧ **M.4 两个遗留缺陷已收口（见附录 Q）**：`deletePDZ` 恒 500 已修（实测 API 删除成功）；`entryVariables 恒 []` 经复验判定为**误诊**（数据空态），但顺带查出并修掉真实缺陷「`PublishToolAsync` 只 Include 一个子集合 ⇒ Use0 PDZ 丢壳」。打包脚本 `-Target main` 已实跑通过。`CJCore/docs/CJCore-登录认证统一架构方案.md` 补了 §8（宿主自带身份体系的接缝）。
> ⑨ **D2 = B 已执行（附录 Q.8）**：回退形态全部下线 —— `DispatchServer`/`McpServer`/`HostWebServer` 类库化（源码保留、删启动壳），`ElsaApiServer`/`ElsaStudio` 移出 sln；执行前先交叉核对 3 个旧 `Program.cs`，确认**注册层面 0 缺失**。Release 实机八面 + Hub 协商 + Elsa 接缝全绿，发布包 Service 面只剩 `CJPlug.Server.exe`。
> **最新进度看附录 Q**（M.4 缺陷收口 + 打包实跑，2026-10-30）；此前各轮记录依次见附录 D（P1）、E（P2/P3/P6）、F（P7.1/P8/P6.1）、G（实机冒烟）、H（端口归一）、J（P8.3 Agent 链路）、K（P4 业务表归一）、L（P8 供给侧量化取证）、M（P8 端到端）、N（D2=B）、O（D7=A）、P（P9）。
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
| 6.3 | 菜单/AppBar 的 `AccessKey` 与模块 `ModuleId` 对齐；模块内功能键（每模块 1–3 个） | ✅ **2026-10-30 完成**：4 个缺席模块补 `IModuleAccessDeclaration` + 2 个菜单补 `AccessKey`；`/api/modules` 实测各得 1 个 feature。详见 `（已完成）P5.5第三批+P6.3 权限声明归口方案-20261030.md` §9 |
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

#### 🔍 2026-10-30 复核：偏差的技术前提比原文更硬，短期不能收敛

为评估能否"顺手做掉 P7.1a"，本轮重新核了一遍依赖面：

| 事实 | 证据 |
|---|---|
| `CJ.Plug.HostWebServer/Program.cs`（独立 Web 薄壳）**没有任何数据库注册** —— 纯前端，全部数据经 HTTP 打 ApiServer | 该文件 `AddData` / `DbContext` / `IModuleDbConfig` 零命中 |
| `PlugSystemConfigStore` 因此**只能**走 `ISystemConfigApiClient`（HTTP 自调用） | `SystemConfig/PlugSystemConfigStore.cs` 构造函数 |
| 工位机 `CJ.Plug.StationApiServer` 引用 `CJ.Plug.ApiClient` ⇒ 删 `CJ.Plug.SystemConfigApiClient` 会连带改工位机 | 其 csproj `ProjectReference` |
| `CJ.Plug.ModuleConfig` 同时引用 `CJ.Plug.SystemConfig` + `CJ.Plug.SystemConfigApi`，并调用 `AddSystemConfigPageModuleServices` / `AddSystemConfigModuleApiServices` / `AddSystemConfigModuleApi` | `ModuleConfig.cs:10,17,70,108,147` |

⇒ P7.1a 的真实工作量 = **给 Web 薄壳补数据访问层（或推翻"薄壳无库"的架构前提）+ 改工位机引用 + 拆 3 个模块注册点 + 数据迁移**。
不是"一次性机械改动"，需单独决策。**本轮不做**。

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
| 6 | **P8 端到端验收** | ① CJPlug 接入 CJCore.Agent 消费链；② ~~先解决端口冲突（见 #8）~~ | 供给侧 + 桥接**已端到端打通（附录 H.3）**；剩**消费链**未接 |
| 7 | **删除 `PlugTempAuthHandler` + `PlugTempAdminAuthStateProvider`** | P5 上线当天 | 两件必须**成对**删，否则是永久安全后门 / 权限口径错位 |
| 8 | **跨进程端口规划**（本轮新发现） | `CJ.Plug.McpServer/appsettings.json` 的 Kestrel URL = `http://*:5066`，**与 Web 宿主撞端口**；且它会覆盖 `app.Run("http://localhost:3001")` | ✅ **已修复**（附录 H.1/H.2）：appsettings 改为 `http://*:3001`，与 `app.Run`、Aspire `KestrelUrl` 三处口径归一 |

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
| T6 | P8 工具清单同步 | ❌ **未通过**：bridge 端点受本机端口访问策略限制（curl 返回 `HTTP=000`）无法直连验证；McpServer 侧日志见 `[DynamicWorkflowTool] Tool=111` |
| ⚠ | **上条 T6 备注存在误读（附录 H.4 更正）** | 当时把 `Tool=111` 读成「111 个工具」。**真相：当前库里只有 1 条已发布工作流，其 `Name` 就叫 `111`** ⇒ `Tool=111` 是**工具名**而非数量。第六轮已用 bridge 返回值实证（`count=1`，`name="111"`） |
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

---

## 附录 H：第六轮 —— 跨进程端口归一 + P8 供给侧端到端（2026-10-29）

> **本轮产出**：修掉第五轮留下的 #8（端口冲突），并把 T6 从 ❌ 翻成 ✅。
> 同时**更正第五轮一处读错日志导致的错误结论**（H.4）。

### H.1 端口冲突根因：一处错值，三处口径

| 来源 | 端口值 | 判定 |
|---|---|---|
| `CJ.Plug.McpServer/appsettings.json` → `Kestrel:EndPoints:Http:Url` | `http://*:5066` | ❌ **错**：与 Web 宿主撞端口，且 Kestrel 配置优先级**高于** `app.Run` |
| `CJ.Plug.McpServer/Program.cs:116` → `app.Run("http://localhost:3001")` | `3001` | ✅ 对，但被上一行覆盖 |
| `CJ.Plug.AspireHost.AppHost/AppHost.cs:243` → `KestrelUrlEnvKey=http://*:3001` + `WithUrl("http://localhost:3001")` | `3001` | ✅ 对（部署形态） |
| `PlugMcpBridgeClientOptions.DefaultBaseAddress` | `http://localhost:3001` | ✅ 对 |

**根因**：`appsettings.json` 是从其它服务的模板复制而来，端口没跟着改 —— 属于「复制粘贴型错值」，不是设计决策。
**修法（最小改动）**：appsettings 改为 `http://*:3001`，让「不带任何环境变量裸启动」也落在正确端口；同步改
`02.Publish/Services/{Debug,Release}/net10.0/appsettings.CJ.Plug.McpServer.json`（该目录不随构建刷新，是打包脚本产物）。

> ⚠ **顺带确认的既存打包约定（本轮不改）**：全部服务共用同一输出目录，`appsettings.json` **只有一份**（最后构建的服务那份会被复制进去，当前是 McpServer 的）。
> 因此「从该目录裸启动任一服务」读到的都是同一份；**部署形态靠 `ASPNETCORE_ENVIRONMENT` + 每服务的 `appsettings.CJ.Plug.X.json` 区分**。
> 这意味着：**任何服务都不要依赖 appsettings 里的 Kestrel 端口裸启动**，端口必须由启动方显式给（AppHost 已经是这么做的）。

### H.2 本轮第二个「假故障」：会话注入的 `HTTP_PROXY`

| 现象 | 根因 | 证据 | 结论 |
|---|---|---|---|
| McpServer 调 `http://localhost:8687/api/mcp/getPublishedWorkflows` 返回 **502 Bad Gateway**；SignalR 连 `8686` 同样 502；curl 直连返回 `HTTP=000` | **本会话 shell 注入了 `HTTP_PROXY/HTTPS_PROXY=http://127.0.0.1:52257`**，.NET `HttpClient` 默认继承代理，把 loopback 请求也送进代理 | `env \| grep -i proxy` 可见四个代理变量；加 `no_proxy` 后同一请求立刻 200 | **不是产品缺陷** —— 第五轮把 T6 判成 ❌，真实原因其实是「端口冲突 + 代理假象」两件事叠加 |

**冒烟必须这样起进程**（否则任何跨进程调用都会 502）：

```bash
env -u HTTP_PROXY -u http_proxy -u HTTPS_PROXY -u https_proxy \
    NO_PROXY=localhost,127.0.0.1 no_proxy=localhost,127.0.0.1 \
    ASPNETCORE_ENVIRONMENT=Development \
    Kestrel__EndPoints__Http__Url=http://127.0.0.1:<port> \
    dotnet <Service>.dll
```

### H.3 T6 复跑结果（✅ 通过）

| # | 判据 | 结果 |
|---|---|---|
| H-1 | McpServer 绑 3001 | ✅ `Now listening on: http://127.0.0.1:3001`，无 `Failed to bind` |
| H-2 | 工具清单来自 ApiServer（非缓存） | ✅ 启动期 `GET /api/mcp/getPublishedWorkflows` 成功（不再 502） |
| H-3 | bridge 端点可用 | ✅ `GET http://127.0.0.1:3001/api/plugbridge/tools` → **200 / count=1**（工具名 `111`） |
| H-4 | 宿主自动同步（**未加任何环境变量、未改代码**） | ✅ Web 日志：`[P8] 已从 CJPlug McpServer 同步 1 个动态工具到 CJCore IToolRegistry` |
| H-5 | 执行链路错误路径不炸 | ✅ `POST /api/plugbridge/call {"name":"__no_such_tool__"}` → **404** `{"error":"未找到工具: __no_such_tool__"}`（非 500、非未处理异常） |
| H-6 | 既有页面无回归 | ✅ `/`=200、`/admin/modules`=200、`/system-config`=200、`/nonexist-abc` → 404 页（含「未找到 / 404 / 页面不存在」） |

**供给侧可达性（代码级证明）**：`DefaultToolRegistry.AllEnabledDefinitions` = static ∪ dynamic，仅按 `Enabled` 过滤；
`PlugMcpToolExecutor` 的 `Enabled=true`、`AgentName=null` ⇒ CJPlug 工具会同时出现在 `AllEnabledDefinitions` 和任意
`GetDefinitionsForAgent(agentName)` 里。

**仍缺的那一半（= #6 消费链）**：CJPlug 宿主目前**没有 `CJCore.Agent` 执行链** —— `IToolRegistry` 的消费方只存在于
`CJCore.SystemMonitorAgent` 与 `CJCore.Modules.Dsl.Workbench`。即工具「能查到」但「没人调」。

### H.4 更正第五轮的一处误读（重要）

G.4 的 T6 备注把日志 `[DynamicWorkflowTool] Tool=111` 读成「111 个工具」。
**真相：当前库里只有 1 条已发布工作流，它的 `Name` 就叫 `111`** ⇒ `Tool=111` 是**工具名**而不是数量。
本轮用 bridge 的返回体实证：`count=1`、`name="111"`（`description="Execute workflow: 111"`）。
**教训：日志里的数字要先确认语义（ID？名称？计数？）再写进结论。**

### H.5 编译门禁（第六轮）

| 目标 | 结果 |
|---|---|
| `dotnet build CJPlug/CJ.Plug-Aspire.sln` | **0 错误 / 2394 警告**（基线 2399 ±5） |
| 下游 `CJCore.sln` / `CJOntology.sln` | 本轮**未跑**（仅改 JSON，无代码改动，无 API 面变化） |

### H.6 剩余阻塞（更新版）

| # | 项 | 需要的输入 | 状态 |
|---|---|---|---|
| 1 | P4（41 表归一） | P2 冒烟结论（✅ 已出）+ **db 备份确认** | 含数据搬迁，须实机 |
| 2 | P5（身份替换） | P4 + 管理员初始密码写入方案 | **不可整体回滚** |
| 3 | P6.3（`AccessKey` 与模块内功能键对齐） | ✅ **已收口（2026-10-30）**：4 模块补声明、5 模块旧权限契约归口、27 个功能点进矩阵；显示名 3 处统一（`plug-tool-action` features 0→1）；P5.5 真第三批 `/api/plug-agent/tools` 收口（匿名 401 实测）+ 临时认证死代码删除。**原"矩阵无接口行"残留已于同日解决**：原归因（端点扫描失效）**是错的**，真因是 `GetAllAsync` 硬编码 `includeEndpoints:false`，修法见 `CJCore/docs/（已完成）策略②接口权限键可授性方案-20261030.md`（V1~V14 全过） | 见方案 §9、§10 |
| 4 | P7.1 严格口径（ApiServer 侧贡献配置） | 确认 F.2 记录的偏差口径 | 需你拍板 |
| 5 | P7.2（许可证） | 差异对照表对齐 | 计划写明「未对齐不得替换」 |
| 6 | **P8 消费链**（CJPlug 接入 CJCore.Agent） | **先拍 D1（附录 I.3）**：A 可观测 / B 换工具源 / C 起 Agent / D 维持现状 | 供给侧 ✅，消费侧未接（**调研已发现既有 MCP 直连消费链，见附录 I.1 第 4 条**） |
| 7 | **删 `PlugTempAuthHandler` + `PlugTempAdminAuthStateProvider`** | ~~P5 上线当天~~ | ✅ **已执行（2026-10-30 P5 收尾批）**：两件成对删除（`Shell/PlugTempAuthHandler.cs`、`Shell/PlugTempAdminAuthStateProvider.cs`）。附录 K 末尾登记的 `InvalidCastException: PlugTempAdminAuthStateProvider → AccessToken…` 应随之消失（未单独复测） |

> **本轮未 commit**（沿用纪律：commit/push 由你触发）。
> 本轮改动文件：`src/PlugApiServer/CJ.Plug.McpServer/appsettings.json`、
> `02.Publish/Services/{Debug,Release}/net10.0/appsettings.CJ.Plug.McpServer.json`、本附录。

---

## 附录 I：P8 消费链 —— 调研结论与 D1 决策点（待拍板，2026-10-29）

> **结论先行：D1 的真问题不是"接哪条 Agent"，而是"要不要现在就给 `IToolRegistry` 造消费方"。**
> 因为调研发现 —— **「让 LLM 调用 CJPlug 工具」这件事在 CJPlug 里早就通了**，走的是 MCP 协议，不是 `IToolRegistry`。

### I.1 现状盘点（逐条附证据）

| # | 事实 | 证据 |
|---|---|---|
| 1 | 宿主只引了 `CJCore.AgentTool.MCPTools`（注册表），**未引 `CJCore.Agent.Abstractions`** | `CJ.Plug.HostWebServer.csproj:44` |
| 2 | `IToolRegistry` 的**消费方只存在于 CJCore 侧**：`CJCore.SystemMonitorAgent`、`CJCore.Modules.Dsl.Workbench` | 全仓 grep `IToolRegistry` |
| 3 | `McpSurface` 在 CJCore 里**只有枚举定义、零消费方** ⇒ 注释里写的「内部面 `/mcp`、对外面 `/mcp/device`」**尚未实现** | 仅 `MCPTools/McpSurface.cs` 命中 |
| 4 | **CJPlug 已有一条「LLM + 工具」消费链**：`AskAI.razor` → `IDeepSeekService.AskWithTool` → `OllamaSharp.Chat` + `AllowRecursiveToolCalls` → `GetFromMcpServers()` 直连 **MCP `http://localhost:3001`（SSE）** | `DeepSeekService.Ollama.cs:75-102`：`Command = "http://localhost:3001"`、`TransportType = McpServerTransportType.Sse` |
| 5 | 另有**平行的第三条**：`CJ.Plug.AIChat`（`MEAI` 的 `IChatClient` + `UseFunctionInvocation`，Ollama `localhost:11434`/`qwen3:4b`），它的工具是**自带的** `DataIngestor`/`SemanticSearch`，与 `IToolRegistry` 无关 | `CJ.Plug.AIChat/Program.cs`、`ServiceCollectionExtensions.cs` |
| 6 | CJCore 的 Agent 宿主官方入口是 `AddCJCoreAgentHost(...)`，但它管的是 **Agent *插件* 的装配/门控**（`IAgentPluginCatalog`），**不是**单次会话执行 | `CJCore.Agent.Host/AgentHostServiceCollectionExtensions.cs:22` |
| 7 | CJCore 的 `ILLMChatService` **没有通用注册扩展**，只有 `SystemMonitorAgent` 自己 `new` 出来注册 | `SystemMonitorAgent/Program.cs:77` |

### I.2 由调研得出的三条推论

1. **3001 是 CJPlug 的真实约定端口**（既有 AI 问答把它硬编码在代码里）⇒ 附录 H.1 把 appsettings 改成 3001 不只是"修冲突"，
   而是让「裸启动 McpServer」与既有消费方对齐 —— **裸启动场景下 AI 问答的工具此前一直是断的**（部署形态由 Aspire 注入 `KestrelUrl=3001`，所以线上不受影响）。
2. **「LLM 调 CJPlug 工具」已通** ⇒ 附录 F/H 建的 `IToolRegistry` 链路**不是它的替代品**，而是**给 CJCore 平台内 Agent 用的第二条路**。
   现在给它造消费方 = 同一批工具多一条通道，**有重复建设的实质风险**。
3. 因此 D1 应当先回答：**这一步到底要解决什么** —— 是"让工具被 CJCore 平台内 Agent 看见"，还是"再给 AI 问答换一条工具源"。

### I.3 D1 决策点（本轮只定这一个）

| 选项 | 做法 | 成本 | 收益 | 风险 / 副作用 | 与「接入 CJCore」主线 |
|---|---|---|---|---|---|
| **A（推荐）** | **不新增消费方**。把 `IToolRegistry` 定位为「CJCore 平台内 Agent 的唯一工具源」；本阶段只补**可观测面**（调试页/端点列出 `AllEnabledDefinitions` + 工具数），把"能查到"变成**可见、可验收** | 低（1 个页面/端点） | 供给侧可验收、零重复建设、不动在用链路 | 工具仍"没人调"，但这是**当前事实**，不是缺陷 | ✅ 一致（等 Agent 来了自然接上） |
| **B** | 让现有 `AskAI` 改走 `IToolRegistry`（把 OllamaSharp 的 MCP 工具源换成注册表适配出的 `AIFunction`） | 中 | 统一到进程内注册表 | ⚠ **等价于重复建设**：既有 MCP 直连已可用且是同一批工具；且要改**正在使用**的 AI 链路 | ⚠ 表面一致，实际动的是 CJPlug 自有链路 |
| **C** | 宿主内起 **CJCore.Agent 执行链**：`LLMAgentBase` + 自写 `ILLMChatService` 的 Ollama 适配 + `IAgentConfig`/SystemPrompt | 中高 | 与主线**最一致**，可复用审批门控 / 执行追踪 / 上下文压缩 | 需新增 `CJCore.Agent.Abstractions` + `CJCore.LLM.Abstractions` 引用、并把 CJPlug 现有 LLM 通路（Ollama 直连）翻译成 CJCore 接口 | ✅✅ 最一致 |
| **D** | 维持现状（MCP 给平台外/既有问答，`IToolRegistry` 给未来平台内 Agent），**明确这就是终态**，不再为此立项 | 0 | 立即收口，避免反复 | 交付物里多一条"暂未消费"的通路，需在文档里写明定位 | ✅ 一致（但等于放弃本阶段验收可视化） |

> **推荐 A**：先让"工具进了注册表"这件事**看得见**（这正是当前唯一缺的验收证据），不重复造消费方、不动在用链路；
> 等 CJPlug 真要起平台内 Agent 时（= C 的时机），`IToolRegistry` 直接就是它的工具源，无需返工。

### I.4 顺带发现的既有缺陷（本轮不定，登记为 D2 候选）

| # | 缺陷 | 证据 |
|---|---|---|
| 1 | `AskAI` 从 LLM 配置取到 `_mcpConnectionString` 并传给 `AskWithTool`，但**实现里根本没用它**（硬编码 `http://localhost:3001`）⇒ **用户在配置里改 MCP 地址不生效** | `AskAI.razor:110-116` vs `DeepSeekService.Ollama.cs:214` |
| 2 | AI 资产三套并存、职责重叠：`CJ.Plug.AI`（页面 + LLM 配置读取）、`CJ.Plug.DeepSeekIn`（Ollama/DeepSeek + MCP 工具）、`CJ.Plug.AIChat`（MEAI + 自带 RAG 工具） | 三个项目并列于 `modules/AIModule/` |

### I.5 本轮不做的事

- **不编码**（纪律：先评审后落地）。
- 不改 `AskAI` / `DeepSeekService`（缺陷 #1 属既有行为，是否与 D1 一起修由你定）。
- 不动 `McpSurface`（它在 CJCore 里尚无消费方，本次不为其立项）。

> **D1 已定：选 C（宿主内起 CJCore.Agent 执行链）** ⇒ 展开见**附录 J**。

---

## 附录 J：P8 消费链 —— C 分支实施方案（**待评审**，2026-10-29）

### J.1 结论先行：C 分支比 I.3 预估的**轻很多**

调研前以为要"手搓 LLM 循环 + 翻译工具协议"，实际 CJCore 已备好绝大部分：

| 需要的东西 | 我以为 | 实际 | 证据 |
|---|---|---|---|
| LLM 客户端（带工具调用） | 要自己写 Ollama 适配 | **现成 `LLMChatService`**，构造 `(ILLMConfigReader, HttpClient)`，已含 `CompleteWithToolsAsync` / `CompleteStreamingWithToolsAsync` 两个带工具重载 | `CJCore.LLM.LLMClient/LLMChatService.cs:13` |
| LLM 配置来源 | 要接 CJCore 的配置体系 | **`ILLMConfigReader` 只有一个方法**：`GetDefaultModelConfigAsync() → (Endpoint, ApiKey, ModelName)`；CJPlug 现有 `ILlmConfigApiClient`（`AskAI` 在用，返回 `ProviderName/ApiBaseUrl/ModelName/ApiKey`）一行就能满足 | `CJCore.LLM.Abstractions/ILLMConfigReader.cs` |
| 多轮循环 / 工具结果回填 / 轮次上限 / 危险工具中断 | 要自己写 | **`LLMAgentBase` 全包**（`RunAgentLoopAsync`），派生类只管"装请求 + 交回响应" | `LLMAgentBase.cs:299+` |
| 工具定义类型转换 | 要自己映射 | 基类直接吃 `ToolDefinition`，与 `IToolRegistry.AllEnabledDefinitions` 同源 | `LLMAgentBase.cs:240-302` |

⇒ **CJPlug 侧真正要新写的只有 4 件**：`ILLMConfigReader` 实现、`IAgentConfig` 实现、一个继承 `LLMAgentBase` 的 Agent、一个调用入口（+ 对应的 DI 注册）。

### J.2 新增引用与已知坑

**新增 ProjectReference**（宿主 `CJ.Plug.HostWebServer`）：

| 程序集 | 用途 |
|---|---|
| `CJCore.Agent.Abstractions` | `LLMAgentBase`、`IAgentConfig`、`AgentRequest`、`DefaultAgentContext`、`AgentResponse` |
| `CJCore.LLM.Abstractions` | `ILLMChatService`、`ILLMConfigReader`、`ChatRequest`、`ChatMessage` |
| `CJCore.LLM.LLMClient` | 现成 `LLMChatService` 实现 |
| （已有）`CJCore.AgentTool.MCPTools` | `IToolRegistry` / `DefaultToolRegistry` / `PlugMcpToolExecutor` / `PlugMcpToolSyncService` |

**已知坑（务必先看）**：

1. **同名类型冲突**（沿用附录 B 登记）：`ToolDefinition` 在 `CJCore.AgentTool.MCPTools` 与 `CJCore.LLM.Abstractions` **两边都有**；`ChatMessage` 同理 ⇒ 必须显式限定，否则 CS0104。
2. **`HttpClient` 共享陷阱**：`LLMChatService` 内部 `new LLMClient(httpClient)` 会写 `BaseAddress`/`Timeout`。
   SystemMonitorAgent 踩过：HttpClient 一旦发过请求，这两个属性**再也不能改**（`InvalidOperationException`）⇒ **必须 transient**（`AddHttpClient()` 默认即 transient，别改成 Singleton）。
3. **未配默认模型时的表现**：`LLMChatService` 直接抛 `InvalidOperationException("未配置默认模型，请先在 LLM 配置中设置")` ⇒ 入口要有兜底提示，不能把异常直接抛到页面上。

### J.3 落地步骤（P8.3.1 – P8.3.6）

| 步骤 | 内容 | 落点 |
|---|---|---|
| P8.3.1 | `PlugLlmConfigReader : ILLMConfigReader` —— 走现有 `ILlmConfigApiClient.GetDefaultModelInfoAsync()`，映射成 `(Endpoint, ApiKey, ModelName)`；取不到就返回空串让上层给出可读提示 | 宿主 `A2A/` 或 `Agent/` 目录 |
| P8.3.2 | DI 注册：`ILLMConfigReader`（Scoped）→ `ILLMChatService` = `new LLMChatService(reader, transient HttpClient)`；**不套** SystemMonitorAgent 那套台账装饰器（本阶段不做调用上报） | `Program.cs` |
| P8.3.3 | `PlugAgentConfig : IAgentConfig` —— `AgentName="plug-workflow-agent"`、`SystemPrompt`（说明：可用工具 = 平台已发布的工作流；不确定就先问清再调）、`Model=null`（用全局默认）、`Temperature`、`MaxToolCallRounds` **先给显式上限**（见 J.6 风险 4） | 同上 |
| P8.3.4 | `PlugWorkflowAgent : LLMAgentBase<PlugAgentConfig>` —— 只暴露 `RunAsync(string question, CancellationToken)`；不注入 `ISessionStore`（⇒ 每次独立会话，与 SystemMonitorAgent 做法一致）、不注入 `IProgressReporter`（本阶段不做流式进度） | 同上 |
| P8.3.5 | **最小可验收入口**：一个调试页（挂「调试」组，与 `/admin/modules` 并列）+ 一个只读端点 `GET /api/plug-agent/tools`（列出 `IToolRegistry.AllEnabledDefinitions` 中 CJPlug 的工具：名称/描述/启用态/来源）。**不碰既有 `AskAI`** | 宿主 `Pages/` 或 `CJ.Plug.MainPageContent` |
| P8.3.6 | 观测：日志统一前缀 `[P8-Agent]`，记录"本轮喂给模型的工具数 / 模型返回的 tool_calls / 每个调用耗时 / 结果摘要" | 同上 |

### J.4 非目标（本分支明确不做）

- ❌ **不替换**既有 `AskAI` → MCP(3001) 直连链路（附录 I.4 缺陷 #1 另议，不在本分支）。
- ❌ 不做会话持久化（不注入 `ISessionStore`）。
- ❌ 不做审批流程 UI（见 J.7 的 D2）。
- ❌ 不接 `AddCJCoreAgentHost`（它管的是 Agent **插件**装配，不是单次会话执行）。
- ❌ 不做 LLM 调用台账 / 预算闸（SystemMonitorAgent 那套装饰器）。

### J.5 验收清单

| # | 判据 |
|---|---|
| 1 | 调试页能看到**来自 `IToolRegistry`** 的工具清单（数量与 McpServer bridge 一致，当前 = 1） |
| 2 | 提问 → LLM 返回 `tool_calls` → 宿主经 `PlugMcpToolExecutor` 打到 bridge → 结果回填 → LLM 给出最终答复（**须先用一条无副作用的工作流验证，见 J.6 风险 1**） |
| 3 | 未配置默认模型时，页面给出可读提示（不是 500） |
| 4 | 既有页面无回归（`/`、`/admin/modules`、`/system-config` 均 200） |
| 5 | `dotnet build CJPlug/CJ.Plug-Aspire.sln` 0 错误 |

### J.6 风险登记

| # | 风险 | 说明 | 处置 |
|---|---|---|---|
| 1 | **工具副作用未知（最大）** | 当前库里唯一工具叫 `111`；`main-elsa.db` 的 `WorkflowDefinitions` 里存在 `111 / 333 / 666 / 888` 等**测试命名**的工作流，可能驱动真实工位机 | **验收前置**：先在客户端人工打开该工作流确认它做什么；必要时先发布一条只读/空操作工作流用于验收 |
| 2 | 危险工具门控语义 | `LLMAgentBase` 对 `IsDangerous=true` 的工具是**永不直接执行**（fail-closed，进待审批队列）；而 `PlugMcpToolExecutor.IsDangerous` 当前**硬编码 `false`** ⇒ 等于"无审批即可执行" | 见 J.7 的 D2 |
| 3 | LLM 配置通路 | `ILlmConfigApiClient` 走 ApiServer(8687)；宿主内可用（`AskAI` 在用）⇒ 风险低，但需实测 | P8.3.1 完成后先单测一次 |
| 4 | 轮次无上限烧 token | `MaxToolCallRounds ≤ 0` = 不限轮次 | P8.3.3 **显式给上限**（建议 8），后续按实际调整 |
| 5 | 同名类型冲突 | `ToolDefinition` / `ChatMessage` 双份 | 全程显式限定（附录 B） |
| 6 | 两条消费链并存 | 既有 MCP 直连 + 新增 Agent 链 | 本分支**刻意并存**（J.4）；定位写清楚，避免后续误判为重复建设 |

### J.7 D2 决策点（本轮只定这一个）

**D2：CJPlug 动态工具的「危险 / 审批」口径怎么定？**
（背景：`LLMAgentBase` 的语义是 —— `IsDangerous=true` ⇒ **永不直接执行**，必须走 `IPendingApprovalStore` 待审批队列由外部消费；`IConfirmationGateway` 只是**第一道闸**（返回 false 才拒绝），通过之后仍然只是"进队列"。⇒ 标 true 而没有审批消费方 = 工具**永远跑不起来**。）

| 选项 | 做法 | 后果 | 成本 |
|---|---|---|---|
| **A（推荐）** | `IsDangerous` 保持 `false`（可直接执行）+ 入口限定在**调试页** + 只暴露经确认的工作流；把"危险标记 + 审批"列入下一阶段 P8.4 | 本阶段能真跑通（J.5 判据 2 可达）；安全靠"入口收敛 + 人工先确认工作流副作用"兜底 | 低 |
| **B** | `IsDangerous=true` + 补完整审批（`IPendingApprovalStore` 实现 + 审批页 + 审批后执行通道） | 最安全，但本阶段交付量翻倍，且**在此之前工具一次都跑不起来** | 高 |
| **C** | 按工作流元数据区分危险与否（需 `PublishedWorkflowDto` 补 `IsDangerous` 字段，由发布侧声明） | 最合理，但**跨进程 + 跨端**（McpServer / ApiServer / 发布端），本阶段动不了 | 高（依赖外部） |

> **推荐 A**：先把链路跑通（这是 C 分支的目的），安全边界用"调试入口 + 发布前人工确认"兜；P8.4 再按 B/C 收口审批。

### J.8 本轮不做的事

- **不编码**（等 J 评审通过）。
- 不改 `PlugMcpToolExecutor.IsDangerous`（等 D2）。
- 不发布/不执行任何工作流（等 J.6 风险 1 的人工确认）。

> **D2 已定：选 A**（`IsDangerous=false` + 入口收敛到调试页 + 只暴露经确认的工作流；审批门控留 P8.4）⇒ 编码已完成，记录见 **J.9**。

### J.9 P8.3 实施记录（2026-10-29 实机验收）

**新增/改动文件**

| 文件 | 作用 |
|---|---|
| `Agent/PlugLlmConfigReader.cs` | P8.3.1：`ILLMConfigReader` 实现，把既有 LLM 配置的 `ApiBaseUrl/ApiKey/ModelName` 翻译给 CJCore（**不另建模型配置真理源**） |
| `Agent/PlugAgentConfig.cs` | P8.3.3：`IAgentConfig`（`MaxToolCallRounds = 8`，显式上限） |
| `Agent/PlugWorkflowAgent.cs` | P8.3.4：`LLMAgentBase<PlugAgentConfig>` 派生类 + `GetVisibleTools()`（只读，源 = `IToolRegistry.AllEnabledDefinitions`） |
| `Agent/PlugAgentModule.cs` | P8.3.5 **必要条件**：宿主程序集首个 `IModule` —— 没有它，`/debug/plug-agent` 一律 404 |
| `Agent/PlugAgentMenuService.cs` | 菜单项挂「调试」组 |
| `Pages/PlugAgentDebug.razor` | 调试页（工具清单 + 提问 + **勾选确认才可执行** + 未配模型的兜底提示） |
| `Program.cs` | 注册（LlmConfig HttpClient、`AddLLMChatService<PlugLlmConfigReader>()`、Agent、模块、菜单）+ 端点 `GET /api/plug-agent/tools`（只读） |
| `CJ.Plug.HostWebServer.csproj` | +4 引用：`Agent.Abstractions` / `LLM.Abstractions` / `LLM.LLMClient` / `CJ.Plug.LlmConfigApiClient` |

**比 J.3 更省的地方**：`AddLLMChatService<TConfigReader>()` 一行同时注册
`ILLMConfigReader` + `ILLMChatService` + **transient HttpClient**（`CJCore.LLM.LLMClient/ServiceCollectionExtensions.cs`），
无需手写 `new LLMChatService(...)`。

**实机验收（四进程：8686 Dispatch / 8687 Api / 3001 Mcp / 5066 Web）**

| # | 判据 | 结果 |
|---|---|---|
| 1 | `GET /api/plug-agent/tools` | ✅ **200**：`[{"name":"111","description":"Execute workflow: 111","parametersJson":{...}}]` ⇒ 工具确实进了 `IToolRegistry` 且被 Agent 面看到 |
| 2 | `/debug/plug-agent` 可打开 | ✅ **200**，SSR 渲染出工具清单 + 「没有审批门控」警示 + 确认勾选框 |
| 3 | 宿主程序集 `@page` 进路由发现 | ✅（`PlugAgentModule` 生效，验证了对 J.2 坑 #1 之外那条框架约束） |
| 4 | 菜单 / 模块登记 | ✅ 首页 HTML 命中 `plug-agent`；`/admin/modules` 命中 2 次（模块元数据被消费） |
| 5 | 既有页面无回归 | ✅ `/`、`/admin/modules`、`/system-config` 均 200 |
| 6 | 编译门禁 | ✅ `dotnet build CJ.Plug-Aspire.sln --no-incremental` → **0 错误 / 2395 警告**（基线 2399±5） |
| 7 | **真跑一次工具调用** | ⏸ **未做**（J.6 风险 1：工作流 `111` 的副作用未人工确认；且需平台已配默认模型）⇒ 保持 J.8 的纪律 |

**本轮新发现（登记，未修）**

| # | 发现 | 影响 |
|---|---|---|
| 1 | **`DynamicToolRegistry` 只在启动时刷新一次**，之后仅靠 DispatchServer(8686) 的 SignalR `MCPToolUpdated` 通知 | 若 McpServer 先于 ApiServer 起来 ⇒ 拉到 0 个工具且**再也不会自愈**（本次实测复现：日志 `Failed to refresh... Using 0 cached`，重启 McpServer 后 `refreshed: 1 tools`）。建议给 `DynamicToolRegistry` 加周期刷新兜底（属 CJPlug 既有代码，不在 P8.3 范围） |
| 2 | 该日志同时给出**确证**：`DynamicToolRegistry refreshed: 1 tools registered` ⇒ 数量就是 1，工具名才是 `111`（再次印证 H.4 的更正） |

**J.2 坑 #1 的更正**：方案里写「`ToolDefinition` 两边同名」——实测**只有一份**
（`CJCore.LLM.Abstractions`，`AgentTool.MCPTools` 只是 `using` 它）⇒ 无同名冲突，无需显式限定。
（同类提醒对 `ChatMessage` 同样不成立。）


---

## 附录 K：P4 业务表归一实施记录（2026-10-29）

> **口径变更（用户指令）**：「不用管历史数据，直接迁移功能」⇒ §5-P4 的 4.5「分批 `INSERT INTO … SELECT` 数据搬迁」
> 整条取消，本阶段只做**代码层的表归属迁移**。这同时消掉了原计划里最大的风险项（数据搬迁不可回滚）。

### K.1 关键结论：同库双上下文 + 「身份先建、业务后建」的顺序契约

原计划 §P4 写的是「两者都用迁移记账，同库安全」，但 P3 的 EF 基线已确认不可行（动态模型 ⇒ 设计时快照必然缺表）。
本轮改用**不依赖 EF 迁移基线**的方案，靠顺序解决 `EnsureCreated` 的 `HasTables` 语义：

| 顺序 | 动作 | 落点 |
|---|---|---|
| ① | `MainDbContext.EnsureCreated()` —— 只创建 **7 张身份/权限表** + Identity 的 `AspNet*` | `ApiServer/Program.cs`（`app.Run()` **之前**） |
| ② | `AutoMigrationHostedService` 冷启动建 **全部业务表** + 写 `__CjModelSnapshot` | `AddCJCoreDataApi` 注册的 HostedService（`Run()` 时启动） |

> ⚠ **顺序不可颠倒**：若业务表先建，`EnsureCreated` 见「库里已有表」整体 no-op ⇒ 身份表永久缺失、登录全挂。
> 这条已写进 `Program.cs` 与 `WebHostStartup.cs` 的注释。

### K.2 契约分层：两个派生标记，模块项目零 csproj 改动

```
CJCore.Modules.Data.IModuleDbConfig            ← CJCore 原生契约（AddDbSets / ConfigEntities / StructureRevision）
├── CJ.Plug.Models.Contracts.IModuleDbConfig         身份侧标记 → 只注册 UserModuleDbConfig（7 张身份表）
└── CJ.Plug.Models.Contracts.IModuleBusinessDbConfig 业务侧标记 → 18 个业务 DbConfig
```

- **18 个模块项目为什么不用加 CJCore 引用**：`BusinessModuleDbConfigAdapter`（Singleton）把 18 个业务配置
  汇总成**一个** CJCore `IModuleDbConfig` 注册进 DI，于是 18 个业务配置类只依赖 `CJ.Plug.Models` 的标记接口。
  只有 `CJ.Plug.Models` 新增一句 `CJCore.Modules.Data.Model` 引用。
- **代价（已知）**：`DataDbContext` 的模型缓存键按「配置**类型**清单」拼，汇总成一个适配器后键里只有适配器类型。
  CJPlug 恒全集注册 ⇒ 模型恒等，不构成问题；将来要按模块裁剪才需改回逐项注册。
- **为什么不直接注册成 CJCore 接口**：MS DI 不把派生接口注册解析成基接口（P1 已踩过，28 处注册被迫逐个改）。

### K.3 改动清单

| # | 位置 | 改动 |
|---|---|---|
| 1 | `CJ.Plug.Models/Contracts/IModuleDbConfig.cs` | 拆成两个派生标记接口（原为含 `AddDbSets`/`ConfigEntities` 的独立契约） |
| 2 | `CJ.Plug.Models/Services/BusinessModuleDbConfigAdapter.cs` | **新增**：18 业务配置 → 1 个 CJCore 配置 |
| 3 | `CJ.Plug.Models/Services/BaseRepositoryService.cs` | 泛型化上下文（`<TEntity,TId,TContext>`）+ 保留两泛型兼容形态（默认 `DataDbContext`）；**删掉构造函数里的 `EnsureCreatedOnce`** |
| 4 | 18 个 `*ModuleDbConfig` | `: IModuleDbConfig` → `: IModuleBusinessDbConfig`（纯标记替换，方法体零改动） |
| 5 | 19 处注册行 | `AddSingleton<IModuleDbConfig, …>` → `<IModuleBusinessDbConfig, …>`（`UserModuleDbConfig` 保持原样） |
| 6 | 34 个业务消费者 | `MainDbContext` → `DataDbContext` + 补 `using CJCore.Modules.Data;` |
| 7 | **保留** `MainDbContext` 的 6 处 | `UserManageService`（改 `BaseRepositoryService<User,int,MainDbContext>`）、`UserManageSeedDataProvider`、`RolePermissionService`、`GroupManageService`、`DepartmentManageService`、`LoginServiceCollection`（`AddEntityFrameworkStores<MainDbContext>`） |
| 8 | 7 个业务 Service 构造函数 | 删除各自的 `EnsureCreatedOnce(_dbContext)` —— 否则形成「迁移前偷偷建表」的第二条通道 |
| 9 | `HostWebServer/WebHostStartup.cs` | 新增 `AddCJCoreDataApi`（连接串复用 `DbConnectionString`，与 MainDbContext **同一个** `main.db`）+ 注册适配器 + **`InitAction` 承接种子** |
| 10 | `ApiServer/Program.cs` | 身份库 `EnsureCreated` 保留并改注释；**移除** `SeedDataRunner.RunAllAsync(app.Services)`（见 K.5） |
| 11 | `HostWebServer.csproj` | 加 `CJCore.Modules.Data.Api` 引用。**刻意不引伞项目 `CJCore.Modules.Data`** —— 它会连带 `AddCJCoreDataUI()`，把「数据」页面/菜单塞进侧栏，破坏 D3=A 三组归一 |

### K.4 实机冒烟（临时库，未动真实数据）

为不动生产库，冒烟时临时把 `ConnectionStrings:PlugDb` 指向 `main-p4smoke.db`，跑完即还原并删库。

| 判据 | 结果 |
|---|---|
| 编译门禁 | `dotnet build CJ.Plug-Aspire.sln` → **0 错误** |
| 启动 | 5066 监听，全程无 DB 相关异常 |
| **建表** | 全新库 **72 张表**：19 个业务模块表（Plugs / Jobs / KnowledgeBases / SystemConfigItems / MCPTools / Ontology* / PDZ* …）+ 7 张身份/权限表 + `AspNet*` + `MA_ModuleDefinitions` / `MA_ModuleFeatures` + `DataProtectionKeys` |
| **迁移记账** | `__CjModelSnapshot` = **1** 行、`SchemaMigrationHistory` = 0（冷启动路径，符合预期） |
| **种子**（P4.6） | 6 个提供者**全部成功**：admin/123456、4 个角色、开发部、专家组、**17 个默认工具**、6 种本体 + 18 实例、11 个基础枚举、SysML V2（171 条）、M4/M5/M5.5/M6/M7 全部落库 |
| **业务读路径** | `GET /api/systemconfig/getAll` → 返回种子行 `RemoteViewMode=window`（**经 DataDbContext**）；`GET /api/mcp/getPublishedWorkflows` → `[]` |
| 页面 | `/` `/admin/modules` `/system-config` `/api/plug-agent/tools` 均 **200** |
| 模块启停持久化 | `MA_ModuleDefinitions` = **36** 行 ⇒ 已从「中立交换文件」升级为落库 |

> **顺带修掉 A.5 第 1 项**：`SeedDataRunner` 原来吃 `app.Services`（根容器）⇒ 5 类种子 `Cannot resolve scoped service`、admin 种不上。
> 本轮改由引擎 `InitAction` 触发并**自建 scope**，上面「admin/123456 + 4 角色」即为实证。

### K.5 遗留与注意（必须知道）

| # | 项 | 说明 |
|---|---|---|
| **1** | ⚠ **存量 `main.db` 首次启动不会补齐新表** | 引擎在「无快照 + 已有模型表」时走**基线化**（只写快照、不建表）。存量库里 `MA_*`、`DataProtectionKeys`、`__Cj*` 等新表不会被创建 ⇒ 模块启停/数据保护会报错。**处置：把 `02.Publish/Services/main.db` 移作备份，让全新库冷启动**（种子会重建 admin/123456）。这正是「不迁移历史数据」的落地动作 |
| 2 | 身份/业务**同库双上下文** | 过渡态。P5 换 ThreeRoleAdmin 后身份表并入 Data 模型，`MainDbContext` 与 `IdentityDbContext` 引用一并删除 |
| 3 | `SchemaMigrationHistory` 为 0 | 本仓未使用声明式迁移（`IModuleDbMigration`），记账走 `__CjModelSnapshot`，非缺陷 |
| 4 | `P7.1` 的 `PlugSystemConfigStore` 仍走 HTTP 自调用 | 现已同进程（5066），可退化为本地调用，属合并后的收尾，未在本轮动 |
| 5 | 未跑开发环境（Development） | Development 的容器校验会撞上既有的 `IDeepSeekService` 生命周期冲突（文档 A.5 第 2 项），与本轮无关 |

### K.6 存量库处置执行记录（2026-10-29 20:00）

按 K.5 第 1 项的处置路径**已实际执行**，把开发机的存量库换为全新冷启动库。

**执行动作**

| 步骤 | 命令/动作 | 结果 |
|---|---|---|
| 1 | 备份 `main.db` + `main.db-wal` + `main.db-shm` → `02.Publish\Services\_backup-p4-before\` | 2.0 MB + 3.3 MB + 32 KB，三件齐全，可原样回退 |
| 2 | 启动 `CJ.Plug.ApiServer.exe`（cwd = `Services\Debug\net10.0`，`ASPNETCORE_CONFIG_PATH` 指同目录 `appsettings.CJ.Plug.ApiServer.json`） | 5066 监听，无 DB 异常 |
| 3 | `PRAGMA wal_checkpoint(TRUNCATE)` | WAL 合并回主库，`main.db` 自包含（852 KB），`-wal`/`-shm` 消失 |

**前后对比**

| 指标 | 存量库（66 表） | 冷启动后（72 表） |
|---|---|---|
| 表总数 | 66 | **72** |
| `MA_ModuleDefinitions` / `MA_ModuleFeatures` | ❌ 缺失 | ✅ 36 行 / 3 行 |
| `DataProtectionKeys` | ❌ 缺失 | ✅ 存在 |
| `AspNet*` | 5 张（`AspNetRoles` 不在其中，角色走业务 `RoleInfos`） | 5 张（同构） |
| `Users` | — | 1（admin，`PasswordHash` 84 字符、`SecurityStamp` 已生成） |
| `AspNetUserRoles` / `UserRoles` | — | 1 / 4 |
| `Tools` | — | 17 |
| `PRAGMA integrity_check` | — | **ok** |

**种子（日志实证）**：4 个系统角色（系统管理员/授权管理员/审计管理员/普通用户）、开发部、专家组、`admin` 用户 + 管理员角色分配、17 个默认工具、6 种本体 + 18 实例、11 基础枚举、SysML V2 —— 全部成功。

**端点复验**：`/` `/admin/modules` `/system-config` `/api/plug-agent/tools` `/api/systemconfig/getAll` 均 **200**。

> ⚠ **注意 curl 走代理**：本机 `http_proxy=http://127.0.0.1:60819`，直接 `curl http://127.0.0.1:5066/` 会被代理拦成 **502**，看起来像服务崩了。冒烟必须加 `--noproxy '*'`（详见 K.7）。

### K.7 本轮新发现的两条隐患（未修，登记）

| # | 现象 | 根因 | 影响面 | 建议 |
|---|---|---|---|---|
| **1** | `[HUB] 第 1~3 次连接失败：502 (Bad Gateway)`，第 4 次才成功 | `HttpClient` 默认继承系统 `HTTP_PROXY`，连**本机环回** `http://localhost:5066/` 也被送进代理 | 客户现场若配了系统代理/上网行为管理，`localhost` 也会走代理 ⇒ 启动慢 3 个重试周期、日志刷错误、可能判为故障 | 给服务间自调用（Hub、`CJPlugMcpBridge`、`PlugSystemConfigStore`）的 `HttpClient` 配 `UseProxy=false`，或 `WebProxy.BypassList` 加 `localhost;127.0.0.1` |
| **2** | `InvalidOperationException: The HubConnection cannot be started if it is not in the Disconnected state` × N | 与 #1 同源：重试逻辑在 `Connecting` 状态下再次 `StartAsync`；502 让状态机卡在中间态 | 日志刷屏（与用户此前反馈的 FirstChanceException 刷屏高度疑似同源）；最终能连上但掩盖真因 | 修 #1 后大概率自然消失；若仍复现，重试前应 `await StopAsync()` 或判 `State == Disconnected` 再 Start |

> 两条都在**本机**触发（因为这台机器装了代理），客户内网若无代理不会复现；但既然一台机器能触发，就不能算"环境特例"，应修。

### K.8 K.7 两条隐患的修复实施记录（2026-10-29 20:10）

#### K.8.1 先修正我自己的判断：K.7 对第 2 条归因错了

K.7 把 `cannot be started` 归因为「重试逻辑并发」。翻代码后**推翻**：

| 现象 | K.7 的判断 | 查证后的真实根因 |
|---|---|---|
| `502 Bad Gateway` | 代理拦环回 | ✅ 判断正确。`HttpClient.DefaultProxy` 继承 `HTTP_PROXY`，连 `localhost:5066` 也被送进代理 |
| `InvalidOperationException: cannot be started if it is not in the Disconnected state` ×N | 「重试逻辑在 Connecting 状态再次 StartAsync」 | ❌ **不是 Hub 重试逻辑**。真凶是 `SignalRLogSink`（日志 Sink）**自己**，见下 |

**真凶链条**（`SignalRLogSink.cs` 原实现）：

```
构造函数：_hubConnection.StartAsync();          // ← 不 await、不接异常（fire-and-forget）
                                                 //    启动期 Hub 还没监听 ⇒ 失败无人观察 ⇒ 状态卡在 Connecting
Emit（async void）：
    if (_hubConnection.State != HubConnectionState.Connected)   // ← Connecting 也满足这个条件！
        await _hubConnection.StartAsync();                       // ← 必抛 InvalidOperationException
    catch (Exception ex) => Console.WriteLine(ex.ToString());    // ← 每条日志刷一段完整堆栈
```

即：**日志 Sink 在日志发送路径上做连接管理，异常又被自己打出来。**
和之前 AppHost Manager 的 Stack overflow（全局异常处理器自己抛异常）是**同一类错误：诊断设施成了故障源**。

> 这也解释了为什么刷屏"莫名奇妙、反复出现"——只要 Hub 比 Sink 晚就绪，每条日志都会重新撞一次，且永远自愈不了（异常不改变状态机）。

#### K.8.2 修复清单

| # | 文件 | 改动 |
|---|---|---|
| 1 | **`CJ.Plug.Models/Shared/LoopbackProxyBypass.cs`**（新增） | 进程级幂等配置：把环回地址加入 `WebProxy.BypassList`，**其余地址的代理行为完全不变**。改 `HttpClient.DefaultProxy` 而非逐个改 handler —— SignalR 内部 `new HttpClient()` 也吃它，一处覆盖全部 |
| 2 | **`CJ.Plug.Models/LogModels/SignalRLogSink.cs`**（重写） | ① `Emit`（同步）只投递到**有界队列**（2048，DropOldest），绝不等网络、绝不抛；② 单消费者后台泵串行发送；③ 连接管理收口到 `EnsureConnectedAsync`：**只有 `Disconnected` 才 Start**，失败指数退避（1/2/4/8/16/30s 封顶）；④ 告警限流 5 秒一条；⑤ 实现 `IDisposable`；⑥ 补齐 `WithAutomaticReconnect()` |
| 3 | `CJ.Plug.Models/Services/HubConnectionManagerService.cs` | `SemaphoreSlim` 串行化 Start/Stop；抽出 `TryStartAsyncAsync` 统一状态守卫（补上原先漏掉的 `Connecting` 分支）；`ConnectWithRetryAsync` 区分「失败」与「跳过」 |
| 4 | 5 个进程/客户端入口 | 启动最早期调一次 `LoopbackProxyBypass.Apply()`：`CJ.Plug.ApiServer` / `CJ.Plug.ElsaApiServer` / `CJ.Plug.StationApiServer` / `HostWebServer`（薄壳回退路径） / `StationSettingUI.App.OnStartup` |

**刻意没做**：不给 40+ 处 `AddHttpClient` 逐个配 handler，也不设 `UseProxy=false`（那会打断只能靠代理出网的 LLM 调用）。

#### K.8.3 验证

| 判据 | 修复前 | 修复后 |
|---|---|---|
| `502` 出现次数 | 4+ | **0** |
| `cannot be started if it is not in the Disconnected state` | 数十条 | **0** |
| 环回请求的失败形态 | `502 Bad Gateway`（**代理答的**） | `目标计算机积极拒绝`（**直连答的**）← 决定性证据，证明 BypassList 生效 |
| 连接期日志 | 每条日志刷一段堆栈 | 4 条 `[HUB] 第 N/4 次连接失败` + 1 条 `[LOG-SINK] 退避 1s` + 1 条 `已重新连上`，全程无堆栈 |
| 编译 | — | `dotnet build CJ.Plug-Aspire.sln` → **0 错误** |
| 启动/端点 | — | `/` `/admin/modules` `/api/systemconfig/getAll` 均 **200** |

> 启动初期仍会看到 3 次 `[HUB] 第 N/4 次连接失败：目标计算机积极拒绝` —— 这是**正常竞态**（进程自己还没开始监听），不是缺陷：重试后成功，且日志干净可读。

#### K.8.4 顺带发现（未修，登记）

| 现象 | 位置 | 说明 |
|---|---|---|
| 启动期 `CJActivityProvider 取插头失败`（Connection refused 带堆栈） | Elsa 集成 | **已在 K.9 修复** |
| `InvalidOperationException: JavaScript interop calls cannot be issued at this time` | `Blazor.Diagrams` 第三方库 | 见 K.9.4，非本方代码 |
| `MarketPlug.UsersCount` nullable 标记错误 | EF 模型配置 | 「int 不能标 nullable/optional」，模型配置笔误，与本次无关 |

### K.9 启动期自调用时序修复（2026-10-29 20:22）

#### K.9.1 问题

单进程单端口合并后，多个组件仍在**启动期通过 HTTP 自调用本进程的 API**，但 Kestrel 要等 `IHostedService.StartAsync` 链跑完才真正监听 ⇒ 必然 `Connection refused`，且 `HttpClientFactory` 会把完整 inner stack 打出来，还白等超时（实测单次 4 秒 × 2 个地址）。

首例是 Elsa 的 `CJActivityProvider.GetDescriptorsAsync`：Elsa 的**启动任务是一批顺序执行**的，它排在前面，此时本进程尚未监听。

#### K.9.2 修复

| # | 文件 | 改动 |
|---|---|---|
| 1 | `CJ.Plug.Models/Shared/HostStartupSignal.cs`（新增） | 进程级 `IsStarted` 标志，由宿主 `ApplicationStarted` 回调置位。用静态标志而**不**注入 `IHostApplicationLifetime` —— 使用方（Elsa 的活动提供者）由第三方容器创建，改构造函数签名有兼容风险 |
| 2 | `CJ.Plug.ElsaIntegration/Services/CJActivityProvider.cs` | `GetDescriptorsAsync` 开头加守卫：未 started ⇒ 直接返回空 + 一条明确日志。**不改成"等服务起来再取"** —— Elsa 启动任务顺序执行，在此阻塞会连带拖住 `AdminUserInitializer`（2026-10-08 踩过：管理员永远种不上、组件库首屏空白） |
| 3 | 4 个宿主入口 | `app.Lifetime.ApplicationStarted.Register(HostStartupSignal.MarkStarted)`：ApiServer / ElsaApiServer / StationApiServer / HostWebServer(薄壳) |

**语义等价性**：守卫分支与原有的 `catch` 降级分支**返回完全相同**（空集合，Studio 带 `Refresh=true` 时重取）。区别只是把"试了才知道失败"变成"明知不可能成功就不试"，因此不引入功能退化风险。

#### K.9.3 验证（日志时序为决定性证据）

```
199: [Elsa] 启动期跳过取插头（本进程尚未开始监听）…     ← 守卫生效（IsStarted=false）
200: [Elsa] 启动期跳过取插头（本进程尚未开始监听）…
213: [Host] 已开始监听（ApplicationStarted）：启动期自调用守卫解除   ← 置位完成
```

| 判据 | 修复前 | 修复后 |
|---|---|---|
| `HTTP request failed` 堆栈段数 | 3 | **0** |
| `Connection refused` 未处理堆栈 | 3 段 | **0** |
| 启动期白等耗时 | ~8 秒（4s × 2 地址，两次调用） | **0** |
| 启动日志总行数 | 311 | **246** |
| `502` / `cannot be started` | 0（K.8 已修） | **0**（无回归） |
| 编译 / 端点 | — | 整解 **0 错误**；`/` `/admin/modules` `/api/systemconfig/getAll` **200** |

> `/elsa/api/descriptors/activities` 无凭证时返回 **302**（跳登录），说明该端点本身可达 —— 运行期取插头路径正常，只是需要 Studio 的认证态。

#### K.9.4 顺带精确定位：JS interop 异常来自第三方库

K.8.4 那条 `JavaScript interop calls cannot be issued at this time` 原先只记了"某 Blazor 页面"。查全栈后定位到**不是本方代码**：

```
Blazor.Diagrams.Components.DiagramCanvas.DisposeAsync()
  → Blazor.Diagrams.Extensions.JSRuntimeExtensions.UnobserveResizes(jsRuntime, element)
  → RemoteJSRuntime.BeginInvokeJS(...)   // 静态渲染（预渲染）期调 JS ⇒ 抛
```

**成因**：`DiagramCanvas` 在预渲染阶段被创建又销毁，`DisposeAsync` 里无条件调 `UnobserveResizes`，而那时 JS 运行时不可用。

**影响**：`fail: Kestrel[13] An unhandled exception`，但页面本身仍返回 200；属噪音 + 一条被 Kestrel 记为未处理异常。

**为什么不顺手改**：可选修法都是在**页面层**调整（如该页 `@rendermode` 关闭预渲染，或给 `DiagramCanvas` 外包一层"仅在 `OnAfterRenderAsync` 之后渲染"）。二者都会改变首屏行为，落在 2026-10-08《流程编辑器组件库空白修复方案》的历史雷区内。**需单独评审，不在本轮动手。**

### K.10 DiagramCanvas 预渲染期 JS interop 异常修复（2026-10-29 20:40）

K.9 登记的第三方库问题单独评审后落地，改动比预想小得多。

#### 量化定位（无头探针实测）

| 判据 | 结果 |
|---|---|
| 基线（服务就绪后未访问页面） | 0 条 |
| `curl /` 每访问 1 次 | **稳定 +1 条**（连续 3 次 → 3 条） |
| `/admin/modules`、`/system-config` | 0 增量 |
| `/ProcessManageList` `/TAS` `/MCPToolManage` `/ToolResource` `/DataConnector`（同样引用 DiagramCanvas） | **0 增量** ⇒ 爆炸半径只有首页 |
| 首页实际渲染 | `HomePage.razor` 只启用了 `LandingShowcaseDiagram`，其余 4 个组件被注释 ⇒ 每次恰好 1 条 |

#### 关键约束（为什么不选"关预渲染"）

`CJCore.Framework.UI/FrameworkRoutes.razor` 明确写着 `InteractiveServerRenderMode(prerender: true)`，注释原文：**需要 prerender: true —— SSR 阶段 HttpContext 可用，AccountAuthStateProvider 从 Cookie 恢复认证态 + token，否则 F5/电路重建后登录态与 Bearer token 全部丢失，被迫跳回登录页。** ⇒ 关预渲染不可行；"包一层 OnAfterRender 后渲染"与 IsInteractive 守卫等价但引入多余重渲染。

#### 修复（5 文件，全部纯增量 @if）

`@if (RendererInfo.IsInteractive) { <CascadingValue Value="_diagram"><DiagramCanvas>…</DiagramCanvas></CascadingValue> }`

- `LandingShowcaseDiagram.razor`（唯一激活，含详细成因注释）
- `Features/FeaturesExample.razor`、`Groups/GroupsExample.razor`、`SvgAndHtml/SvgAndHtmlExample.razor`、`WidgetsExample.razor`（当前被注释，防将来启用复发）
- 不动：`Documentation/MyDiagram.razor`（无人引用）、`ProcessEdit/GUIDesigner` 的 MyDiagramDesigner（实测其页面 0 增量，天然条件渲染）

原理：预渲染实例 `IsInteractive=false`（不创建 Canvas ⇒ 无 Dispose 调 JS）；circuit 实例 `IsInteractive=true`（正常渲染）。预渲染 HTML 里画布本来就是 JS 空壳，跳过不影响首屏视觉。

#### 实测（curl + Playwright 无头浏览器登录后验证）

| 判据 | 修复前 | 修复后 |
|---|---|---|
| 连续访问 `/` 5 次累计异常 | 每次 +1 | **0** |
| 预渲染 HTML 首屏文字锚点（中午吃啥/寸金/开始） | 有 | 有（无损失） |
| circuit 内 `.diagram-canvas` / html / svg layer | — | **1 / 1 / 1，html 层 5 个节点**（与代码建的 5 节点一致） |
| 截图目检 | — | 流程图+连线+文字完整，无塌陷 |
| 编译 | — | 整解 0 错误 |

#### 顺带登记（无头浏览器 console 观察到，均未修）

1. `InvalidCastException: PlugTempAdminAuthStateProvider → Elsa.Studio.Login.Services.AccessToken…`（登录后 circuit 内抛）—— 与 P5 身份替换直接相关，P5 时需一并处理。
2. `Can only have one anonymous define call per script file` ×2 —— 疑似 AMD 脚本（Monaco/require）重复加载，既有噪音。
3. 登录表单提交后 `page.url()` 停在 `/login`（cookie 已种、后续访问正常）—— 需人工确认登录后是否自动跳转，可能是脚本时机问题。

---

## 附录 L：P8 供给侧量化取证 —— 工具清单为何是空的（2026-10-30）

> **结论先行：`/api/plug-agent/tools` 返回 `[]` 不是缺陷，是数据空态。** 三跳链路全部 200 可用，
> 工具数为 0 是因为 `MCPTools` 表 **0 条记录**（没有任何已发布并启用的 MCP 工具）。
> 这一条同时意味着：**P8.3 的"可验收"目标至今没有真正达成过** —— 管道建好了，但从未流过水。

### L.1 三跳链路逐跳实测（2026-10-30 08:00，服务已停）

| 跳 | 端点 | 实测 | 说明 |
|---|---|---|---|
| 1 | `GET /api/mcp/getPublishedWorkflows` | **200 `[]`** | 上游数据源（ApiServer 侧 `MCPToolsManageService`） |
| 2 | `GET /api/plugbridge/tools` | **200 `[]`** | McpServer 桥接窗，读 `DynamicToolRegistry.GetProtocolTools()` |
| 3 | `GET /api/plug-agent/tools`（管理员） | **200 `[]`** | 宿主侧 `PlugWorkflowAgent.GetVisibleTools()`，源 = `IToolRegistry.AllEnabledDefinitions` |
| 回归 | 同上（匿名） | **401** | P6.3 权限收口仍生效（未因本次调研回退） |

### L.2 数据源为空的实证（直接读库，非推断）

```
CJPlug/02.Publish/Services/main.db        1,191,936 B  (-wal 424,392 B)
sqlite> SELECT COUNT(*) FROM MCPTools;  →  0
```

过滤条件是 `IsEnabled == true`（`MCPToolsManageService.GetActiveToolsAsync():60`），
而表里**连一条记录都没有** ⇒ 不是"有工具但没启用"，是**从未发布过**。

### L.3 途中排除的两个错误假设（留档，避免重复排查）

| 假设 | 排除依据 |
|---|---|
| 「`PlugPortGate` 把 `/api/plugbridge/*` 拦在 3001，5066 上访问不到」 | 错。`case WebPort` 分支是 `!IsMcpEndpoint(path) && !path.StartsWithSegments("/elsa")`，`plugbridge` 在 **5066 上放行**；`case McpPort` 才显式列它。实测第 2 跳 200 也直接证伪 |
| 「`MapPlugMcpBridge()` 没挂载 ⇒ 404」 | 错。`ApiServer/Program.cs:485` 与 `McpServer/Program.cs:113` **都挂了** |

### L.4 这件事的真正含义（比"清单为空"重要）

P8.3（附录 J）的验收目标是把「工具进了注册表」变成**可见、可验收**。现状是：

| 已验证 | 从未验证 |
|---|---|
| 三跳 HTTP 200、`IToolRegistry` 可被解析、`PlugMcpToolSyncService` 已注册为 HostedService、`/api/plug-agent/tools` 权限收口 | `PlugMcpToolExecutor` **从未真正执行过一次**、`LLMAgentBase` 的**多轮工具循环从未真正跑过**、`ToolNameSanitizer` 对真实工作流名的净化结果未见过 |

⇒ **J.5 验收清单里凡是"喂给模型的工具数 > 0"相关的条目，实际都没跑过。**
这不阻塞交付（CJPlug 本来就没有已发布工作流可暴露），但**不能用"实测通过"来描述 P8.3**。

### L.5 决策点 D1（本轮只定这一个）：要不要让 P8 供给侧真正流过一次水？

> **✅ 已选定 B（2026-10-30 08:0x）**：真发端到端跑一遍。完整执行记录与结论见**附录 M**。
> 结论剧透：**"调用"这一跳是坏的** —— 桥接层以上全通，末端 Elsa 执行必然 500。

| 选项 | 做法 | 成本 | 收益 | 副作用 |
|---|---|---|---|---|
| **A（推荐）** | **不动真实库**：补一个单测/集成探针，用桩 `IToolExecutor` 直接塞进 `DefaultToolRegistry`，断言 `AllEnabledDefinitions` 与 `Resolve` 的行为（含同名覆盖、Enabled 过滤） | 低，只加测试 | 把"注册表语义"变成可回归的断言，零数据污染 | 不覆盖真实 MCP 工具执行路径 |
| **B** | 在测试库发一个工作流做端到端验收（发 → 同步 → 列表 → 调用 → 删） | 中 | 真正跑通 `PlugMcpToolExecutor` + 多轮循环 | ⚠ 会写 `MCPTools` 表与 Use0 PDZ；删除能否干净无保证 |
| **C** | 结案不动：文档写明「P8.3 管道已建、零数据未验收，待有真实工作流时补验」 | 0 | 立即收口 | 交付物里 P8.3 的"可验收"目标实际是空的 |

---

## 附录 M：P8 供给侧端到端验收（D1=B 执行记录，2026-10-30）

> **结论先行：发布 / 同步 / 发现 / 删除四条链路全部打通且实测通过；「调用」这一跳是坏的。**
> 更准确地说：**P8 自己负责的桥接层（发现 → 执行器 → 桥 → 引擎入口）全部走通，
> 断在它下游的 Elsa 保存链上**。这一个"第一次真正执行"暴露了附录 J 从未测到的真实缺陷。

### M.0 安全前置（B 方案是写库操作，先做了三件事）

| # | 动作 | 结果 |
|---|---|---|
| 1 | 备份 `main.db` 三件套（db / -wal / -shm，共 1.67 MB） | `CJPlug/02.Publish/Services/_bak_20261030_P8/` |
| 2 | 确认执行目标 `G8zB` 是否安全 | `ActivityJsonData` 仅 **88 字节**，只有 `id/nodeId/definitionId/type/name`，**无任何活动节点** ⇒ 执行为 no-op |
| 3 | 选定工具名 `P8 E2E 验收-Tool`（含中文+空格+连字符） | 专门用来验 `ToolNameSanitizer` 对真实中文名的净化结果 |

> 库内现状：`Plugs` 1 条（`G8zB`）、`PlugDataZones` 2 条（含 `adminG8zBDesi` / Type=`Desi`）、`PlugDatas` 1 条变量 `111`。
> 具备发布条件（`PublishToolAsync` 需要 Desi PDZ 才能复制出 Use0 模板）。

### M.1 五步实测结果

| 步 | 动作 | 实测 | 判定 |
|---|---|---|---|
| 1 | `POST /api/mcp/addTool`（sourcePlugId=`G8zB`） | **200**，`Id=1`；库内 `MCPTools` 0→1，自动创建 `MCP_Use0_G8zB`（Type=Use0，UserName=`MCP_System`） | ✅ |
| 2 | 同步（`SignalR MCPToolUpdated` 即时 + 宿主 30s 兜底） | 发布后**立即可见**，无需等待轮询 | ✅ |
| 3 | 发现三跳 | `getTools`=1、`getPublishedWorkflows`=1、**`/api/plug-agent/tools` 0→1** | ✅ **核心判据达成** |
| 4 | 调用 `POST /api/plugbridge/call` | ❌ **HTTP 400 / 下游 500**（详见 M.3） | ❌ |
| 5 | 删除 `DELETE /api/mcp/deleteTool/1` | **200**，`getTools` 回到 `[]` | ✅ |

### M.2 净化器的真实结果（J.5 里"从未见过"的那一项）

发布名 `P8 E2E 验收-Tool` ⇒ **实际工具名 `P8_E2E_Tool`**。

| 阶段 | 值 |
|---|---|
| 非法字符替换（`[^A-Za-z0-9_.\-]`→`_`） | `P8_E2E___-Tool`（空格→`_`、中文"验收"→`__`） |
| 连续分隔符压缩（`[_.\-]{2,}`→`_`） | `P8_E2E_Tool` |
| Trim | `P8_E2E_Tool` |

⇒ **中文工作流名会被整段吞掉，只留下拉丁字符骨架**。两个同名中文流程会**碰撞**，
靠 `EnsureUnique` 追加 `_2` 区分 —— 管理员在 MCP 客户端里看到的将是 `P8_E2E_Tool` 与 `P8_E2E_Tool_2`，**无法反推原流程**。

### M.3 调用失败的根因链（四环，每环都是通用缺陷）

异常（服务端日志实拍）：

```
System.InvalidOperationException: 触发流程执行失败（DefinitionId=G8zB）：
  引擎返回 200 但未带 x-elsa-workflow-instance-id 头
  at ElsaApiClient.ExecuteAsync (ElsaApiClient.cs:309)
  at ElsaApiClient.PrepareAndExecuteWorkflow (:371)
  at McpWorkflowRunner.ExecuteMcpWorkflowFromRequest (:167)
  at PlugExecutionEngine.ExecuteFlowSourcePathAsync (FlowSource.cs:38)
  at PlugExecuteService.ExecuteMcpTool (:321)
```

逐环实证：

| 环 | 事实 | 位置 |
|---|---|---|
| **①** | `SaveWorkflowFromJsonAsync` 先调 `DeleteByDefinitionIdAsync`，用 **DELETE** 打 `/elsa/api/workflow-definitions/{id}` ⇒ Elsa 返回 **405 MethodNotAllowed**；该方法**只 `Console.WriteLine` 不抛**，异常被吞 | `ElsaApiClient.cs:196 / 231-250`（日志实拍 `Error: MethodNotAllowed`） |
| **②** | `PrepareAndExecuteWorkflow` 对 `SaveWorkflowFromJsonAsync` 的返回值**丢弃不校验**，保存失败仍继续 | `ElsaApiClient.cs:356` |
| **③** | 结果：`G8zB` **根本没进 Elsa** —— `main-elsa.db` 的 `WorkflowDefinitions` 6 条定义的 DefinitionId 为 `emrq/3kFU/ae78/myfD/Vm2I/thDC`，**没有 G8zB** | 库级实证，非推断 |
| **④** | 随后执行一个不存在的定义 ⇒ Elsa 返回 2xx 但无 `x-elsa-workflow-instance-id` 头 ⇒ 抛 `InvalidOperationException` ⇒ **HTTP 500** | `ElsaApiClient.cs:304-310` |

> **性质判定**：① 的 405 与 ② 的不校验都**与流程内容无关**，是通用缺陷。
> 差别只在于"空流程是否会让保存也失败"——但即使保存成功，① 仍会吞掉 405。
> ⇒ **保守结论：MCP Workflow 工具的执行链路当前不可用；待用非空流程复验以确认是否叠加了空流程因素。**

**排除的两个误判（留档）**：

| 我的误判 | 纠正 |
|---|---|
| "`PlugExecutionRequest.PlugDefinitionId` 与 `ExecuteResultData.Ids.PlugDefinitionId` 字段错位" | 错。前者是**间接属性**，setter 直写后者（`PlugExecutionRequest.PlugDefinitionId` 的 `set => ExecuteResultData.Ids.PlugDefinitionId = value`）。读代码要认出间接属性 |
| "`ExecuteNoPlugIdPathAsync` 因 PlugTypeKey 为 null 而崩" | 错。Ids 有 `= new()` 默认值，不会 NRE；实际走的是 `ExecuteFlowSourcePathAsync` |

### M.4 顺带发现的两个数据空态缺陷

| # | 现象 | 实证 | 影响 |
|---|---|---|---|
| 1 | **Use0 PDZ 复制了壳，没复制变量** | 发布后 `PlugVariableDatas` 仍为 0（该表与 `PlugDatas` 是两张表，`CopyPDZ` 复制的是前者）⇒ 日志 `No PlugVariableDatas found. pdZ exists=True, count=0` ⇒ `EntryVariables count=0` | 工具能被列出，但**入口参数恒为空**，`parametersJson` 永远是 `{"type":"object","properties":{}}` |
| 2 | **`DELETE /api/pdz/deletePDZ/{id}` 恒 500** | 6 个 PDZ 逐个删除全部 500 | 发布产生的 PDZ **无法通过 API 清理**（本次改由库级删除完成） |

### M.5 清理与复原（已核对，与备份逐表比对）

| 项 | 处理 |
|---|---|
| `MCPTools` | 走 API 删除（`deleteTool/1` ⇒ 200），已回 0 |
| `PlugDataZones` | 库级删除 Id 3–8（`MCP_Use0_G8zB` + 5 个 `MCP_CallerG8zBJob3*`），已回 2 条 |
| `Jobs` / `PlugVariableDatas` | 库级删除各 5 条（本次执行产生的孤儿），已回 0 |
| **残留差异** | 仅 `AuditLogs` +15、`TRA_SysLoginSessions` +1、`sqlite_sequence` +3 —— **审计日志按合规保留，未删** |

`MCPTools / PlugDataZones / PlugDatas / Plugs / Jobs / PlugVariableDatas` 六张业务表现值**与备份完全一致**。

### M.6 决策点 D2（本轮只定这一个）：Elsa 保存链要不要修？

| 选项 | 做法 | 收益 | 代价 |
|---|---|---|---|
| **A（推荐）** | 只修**最小必要**：② 保存结果强制校验（失败即中断并回明确错误）+ ① 405 不再静默（至少升为 Warning 日志） | 让失败**立刻可见且语义正确**，不再伪装成"引擎 200 无头" | 不修根因（DELETE 端点可能本身就该换路径/方法），执行仍会失败，只是失败得明白 |
| **B** | 修根因：定位 Elsa 侧 `workflow-definitions/{id}` 正确的删除方式并改对，再用**非空流程**复验整链 | 真正让 MCP Workflow 可执行 | 要动 Elsa 集成；需先造一个非空流程（库里没有现成的） |
| **C** | 结案不动：文档写明「发布/同步/发现/删除已验收，调用不可用（断在 Elsa 保存链）」 | 立即收口，交付物诚实 | 附录 J 的"可验收"目标里，调用一项为红 |

> 另有两个**独立**缺陷不入 D2，需另立条目：`Use0` 变量未复制（M.4-#1）、`deletePDZ` 恒 500（M.4-#2）。

---

## 附录 N：D2 = B 执行记录（2026-10-30）

> 决策：D2 选 **B（修根因 + 非空流程复验）**。
> 结果：**代码修复已完成并生效；但复验把根因从"删除端点用错方法"升级到认证层，保存链仍未打通** —— 需新决策（N.5）。

### N.1 代码修复（已落地，2 处）

| # | 位置 | 改动 | 效果 |
|---|---|---|---|
| 1 | `ElsaApiClient.DeleteByDefinitionIdAsync` | 返回 `Task<bool>`；**2xx=成功、404=本来不存在（也判成功）、其余（含 405/302）记 `CLog.Warning` 并带响应体**；传输异常降为 Warning。**不再只 `Console.WriteLine` 吞掉**，也不阻断保存（保存是 upsert，删除只是清历史版本的可选前置） | 失败可见；不再伪装"已删除" |
| 2 | `ElsaApiClient.PrepareAndExecuteWorkflow` + 新增 `EnsureWorkflowSavedAsync` | 保存后**按 definitionId 回读校验**，读不到就抛「流程保存未生效……」；`SaveWorkflowFromPlugAsync` / `SaveWorkflowFromJsonAsync` 同样校验 | 环②「保存结果丢弃」被彻底堵死：症状不再漂移成"引擎 200 无实例 ID" |

**修复效果实证**：第④步调用失败时，异常文本由原来的
`触发流程执行失败：引擎返回 200 但未带 x-elsa-workflow-instance-id 头`
变为
`流程保存未生效（DefinitionId=G8zB）：引擎接受了保存请求，但按 definitionId 回读不到定义……`
—— 语义正确，指向真因。

### N.2 复验环境先撞上「部署形态与已合并架构不一致」

| 现象 | 证据 | 判定 |
|---|---|---|
| 按 `02.Publish/start-services.cmd` 起了 **4 个进程**（8686/8687/3001/5066） | 该脚本仍写着"starting 4 services" | ⚠ **脚本停留在融合前形态**：单端口收敛（第二轮，2→1 进程 / 5→1 端口 5066）后应只起主宿主 |
| 5066（独立 HostWebServer）启动即被自动迁移拦死 | `MASS-ARCHIVE-BLOCKED`：删表 51 张 ≥ 阈值；模型 **29 张** vs 库 **80 张** | HostWebServer 只调 `ConfigModulePageServices()`，**17 个业务 `IModuleBusinessDbConfig` 全在 `ConfigModuleApiServices()` 里** |
| 数据是否受损 | 逐表核对 `Plugs 1 / MCPTools 0 / PlugDataZones 2 / PlugDatas 1 / Jobs 0 / PlugVariableDatas 0`，`__CjModelSnapshot` 的 ModelHash 与备份**完全一致**，无新增 `_dropped_*` 表 | ✅ **护栏生效，数据零丢失** |

**修法（已落地）**：`ModuleConfig` 新增 `ConfigModuleDbConfigServices()`（只注册 17 个 DbConfig，**不注册**端点/API 客户端），调用点放在 **`HostWebServer/Program.cs`（独立形态专用薄壳）**，不放在共享的 `AddPlugWebHost()` —— 否则合并形态会重复注册。

### N.3 合并形态复验（唯一宿主 = `CJ.Plug.ApiServer` @ 5066）

| 事实 | 证据 |
|---|---|
| 合并方向 = **Web 并入 ApiServer** | `ApiServer/Program.cs:87 ContractUrls="http://*:5066"`、`:286 builder.AddPlugWebHost()`、`:465 app.MapPlugWebHost()`；`WebHostStartup.cs:31-35` 注释 |
| 单进程启动成功 | `Now listening on http://[::]:5066`、`Application started`、`MASS-ARCHIVE-BLOCKED` **0 次** |
| 服务面齐全 | 首页 200、`/api/mcp/getTools` 200、登录 `/api/three-role/account/login` 200 |
| DbConfig 打印 42 行 = **21 模块 × 迁移引擎两次建模**（快照模型 + 目标模型） | 不是重复注册；重复注册会是同一集合进两次 DI |

**五步结果（非空流程夹具：`Elsa.Flowchart` + `Elsa.WriteLine`，311 B）**：

| 步 | 动作 | 结果 |
|---|---|---|
| ① | `POST /api/mcp/addTool`（sourcePlugId=`G8zB`） | ✅ 200，`Id=2` |
| ② | 同步 | ✅ 立即可见 |
| ③ | `getTools` / `getPublishedWorkflows` | ✅ 1 条 / 1 条（`entryVariables` 仍为 `[]`，见 N.6） |
| ④ | `POST /api/plugbridge/call` | ❌ HTTP 400，下游 500 —— **但错误语义已正确**（N.1） |
| ⑤ | `DELETE /api/mcp/deleteTool/2` | ✅ 200，`getTools` 回 `[]` |

**清理**：删 PDZ Id 9–13（5 条）、Jobs Id 6–9（4 条）、`PlugVariableDatas` 0 条；`Plugs.ActivityJsonData` 还原为原 88 B 值。六张业务表回到基线。

### N.4 ④ 的真根因：**`/elsa/api/*` 被 CJCore 三员认证拦成 302**（比 M.3 深一层）

| # | 事实 | 证据 |
|---|---|---|
| 1 | Elsa 3.9 删除端点契约**本来就是对的**：`DELETE /workflow-definitions/{definitionId}`，删成功 **204**、找不到 **404** | Elsa 源码 `Endpoints/WorkflowDefinitions/Delete/Endpoint.cs`（`Delete("/workflow-definitions/{definitionId}")` + `RequirePermission(Definitions, CoreVerbs.Delete)`） |
| 2 | 现状：`/elsa/api/*` **全部 302 到登录页**（DTO 连 `/elsa/api/workflow-definitions` POST 也是 302） | `curl -i POST /elsa/api/workflow-definitions` → `302 Location: /three-role/login?ReturnUrl=...`；带三员 Bearer 同样 302 |
| 3 | HttpClient **自动跟随** 302 → 拿到 **200 的登录页 HTML** ⇒ 保存被判"成功" | 库实证：`main-elsa.db` 的 `WorkflowDefinitions` 仍只有 6 条旧定义，**没有 G8zB**；日志出现 `'<' is an invalid start of a value`（HTML 撞 JSON 反序列化） |
| 4 | 机制：CJCore 的 `ForwardDefaultSelector` **只认 `Bearer ` 前缀** → 一律送 CJCore 自己的 JwtBearer（密钥与 Elsa 不同）⇒ Elsa 的 admin JWT 验签失败 ⇒ 未认证 ⇒ Cookie challenge 302 | `CJCoreAuthServiceCollectionExtensions.cs:62-68`；Elsa 侧配置是 `UseDefaultAuthentication(UseAdminApiKey())` + Elsa.Identity 自己的 JWT |
| 5 | 归因：**P5 认证替换的遗留缺口** —— P5.5 收紧认证时没给 `/elsa/api` 留身份通道；而 `InternalApiKeyGate` 注释里写的「/elsa/api 按 D-P5-9=A 保持匿名」只覆盖内部密钥网关，**没覆盖三员认证** | `InternalApiKeyGate.cs:114` vs 实际 302 |

### N.5 待拍板（新决策点 D7）：认证接缝怎么补 —— **已拍板 A 并已实施，见附录 O**

| 选项 | 做法 | 代价 |
|---|---|---|
| **A（推荐）** | CJCore 认证的 `ForwardDefaultSelector` 增加「路径 → scheme」映射：`/elsa/api` + `Bearer` ⇒ 转发到 **Elsa 自己的 JwtBearer scheme**（每个前缀用自家身份体系，语义最正） | 改 CJCore 框架一处（需确认 Elsa 注册的 scheme 名并做可配置化），影响面小 |
| **B** | ElsaApiClient 改发 Elsa 的 **ApiKey** 头 + Elsa 侧显式配置 ApiKey（3.9 未配置=全拒）+ Selector 认 `ApiKey` 头转发 | 两处改，密钥要落配置 |
| **C** | `/elsa/api` 认证层放行（匿名），安全边界交给 `InternalApiKeyGate`（EnforceWrite 档）+ 内网 | 依赖网络边界；Elsa 端点自身 `RequirePermission` 仍需处理，可能仍 302/403 |

> 推荐 **A**：它是 P5 认证替换的收口动作，且能同时修好流程编辑器（Studio 侧 `CjElsaAutoLoginJwtAccessor` 走的是同一套 Elsa JWT）。

### N.6 顺带取证

| 项 | 结论 |
|---|---|
| `getPublishedWorkflows.entryVariables` 恒 `[]` | **M.4-#1 复现**：`CopyPDZ` 复制 `PlugVariableDatas`（0 行）而非 `PlugDatas` ⇒ 入口参数恒空。独立缺陷，不入 D2/D7 |
| `02.Publish/start-services.cmd` | **必须改**：融合后只剩 1 个进程（`CJ.Plug.ApiServer`，5066）。留着 4 进程会持续制造"回退形态"事故 |
| `CJPlug.Server` 中立宿主提议 | 方向认同（装配入口单一化），建议作为独立决策点（P9）评审后再动；眼下先修脚本 + 收敛双头装配注释 |

## 附录 O：D7 = A 执行记录 —— `/elsa/api` 认证交回 Elsa（2026-10-30）

> 状态：**已实施并实测通过**。P8 供给侧 ①~⑤ 全绿，④ 调用首次返回真实实例 ID。

### O.1 结论先行

| 项 | 结果 |
|---|---|
| 根因 | CJCore 统一认证基座的 `ForwardDefaultSelector` 只按 `Bearer ` 前缀分流 ⇒ Elsa 的 JWT 被送进 CJCore 的 JwtBearer 验签失败 ⇒ Cookie challenge ⇒ 302 登录页 ⇒ 客户端跟随重定向拿到 200 的 HTML（保存"假成功"） |
| 修法 | CJCore 新增中立扩展点 `IAuthSchemeOverrideSelector`（CJCore 不认识 Elsa）；Elsa 模块注册"`/elsa/api` 前缀 ⇒ Elsa 的 `Jwt-or-ApiKey`" |
| 效果 | `POST /elsa/api/workflow-definitions` 真正入库（`main-elsa.db` 出现 `G8zB`，`IsPublished=1`），`plugbridge/call` 返回实例 ID `8bb06570f4d1e74d` |
| 影响面 | 仅"带 selector 且路径命中"的请求改变分流；未注册 selector 的宿主（CJCore 其它产品）**零行为变化** |

### O.2 改动清单（4 处）

| # | 文件 | 改动 |
|---|---|---|
| 1 | `CJCore/src/Framework/CJCore.Framework.Abstractions/IAuthSchemeOverrideSelector.cs`（新增） | 扩展点接口：`string? SelectScheme(string path)`，约定 Singleton、返回 null 不接管、多实现按注册顺序首个非空生效 |
| 2 | `CJCore/src/Framework/CJCore.Framework.Api/Auth/CJCoreAuthServiceCollectionExtensions.cs` | `ForwardDefaultSelector` 先问 selector（惰性解析并缓存一次），命中即整体转交；未命中回落到原 Bearer/Cookie 分流 |
| 3 | `CJPlug/.../CJ.Plug.ElsaIntegration/Auth/ElsaAuthSchemeOverride.cs`（新增） | 路径按"段边界"匹配 `/elsa/api`，返回 Elsa 的 scheme `Jwt-or-ApiKey` |
| 4 | `CJPlug/.../CJ.Plug.ElsaIntegration/ElsaExtensions.cs` + `.csproj` | 在 `AddElsaServicesForApi` 注册 selector（**只在此处**：此处必然已 `AddElsa`，scheme 一定存在）；csproj 增 `CJCore.Framework.Abstractions` 项目引用 |

### O.3 关键取证：Elsa 侧的 scheme 名

| 取证点 | 结论 | 来源 |
|---|---|---|
| Elsa 注册了哪些认证 scheme | `Services.AddAuthentication("Jwt-or-ApiKey").AddPolicyScheme("Jwt-or-ApiKey") + .AddJwtBearer() + .AddJwtBearer("RefreshToken") + ApiKey` | `Elsa.Identity/Features/DefaultAuthenticationFeature.cs` @3.9.0（`private const string MultiScheme = "Jwt-or-ApiKey"`） |
| 该方案自己怎么分流 | 头部含 `ApiKey` ⇒ `ApiKey`；否则 ⇒ `Bearer` | 同上 `ForwardDefaultSelector` |
| `IdentityAuthenticationSchemes.AccessToken` | = `JwtBearerDefaults.AuthenticationScheme`（即 `"Bearer"`） | `Elsa.Identity/Constants/IdentityAuthenticationSchemes.cs` |
| 为什么 302 而不是 401 | 失败后由 `DefaultChallengeScheme`（CJCore 设为自己的 Cookie）处理 ⇒ `Location: /three-role/login?ReturnUrl=...` | 实测响应头 |

> ⚠ `"Jwt-or-ApiKey"` 是 Elsa 的**私有常量**，升级 Elsa 大版本须复核（未命中会抛 "Scheme not registered" —— 显性失败，不会静默）。已在代码注释中标注。

### O.4 实测（合并形态：唯一宿主 `CJ.Plug.ApiServer` @ 5066）

| 步 | 操作 | 结果 |
|---|---|---|
| 0 | 启动 | `Now listening on http://[::]:5066`、`Application started`、**MASS-ARCHIVE 拦截 0 次**；`GET /`=200 |
| — | 认证面基线 | `GET /elsa/api/...` 无凭据仍 302 → `/three-role/login`（未命中 selector 的路径行为不变）；`POST /elsa/api/identity/login` = **200**（此前被 302 成登录页 HTML，是 `'<' is an invalid start of a value` 的来源之一） |
| ① | `POST /api/mcp/addTool`（sourcePlugId=G8zB） | 200，`Id=3` |
| ② | `GET /api/mcp/getTools` | 1 条（`P8 E2E 验收-Tool`） |
| ③ | `GET /api/mcp/getPublishedWorkflows` | 1 条（`workflowDefinitionId=G8zB`，`entryVariables` 仍 `[]` = M.4-#1 未修，已知独立缺陷） |
| ④ | `POST /api/plugbridge/call` `{name:"P8_E2E_Tool"}` | **200，1.2s，`工作流执行成功，实例 ID: 8bb06570f4d1e74d`** |
| ⑤ | `DELETE /api/mcp/deleteTool/3` | 200，`getTools` = `[]` |
| 入库校验 | `main-elsa.db` | `WorkflowDefinitions` 7 条（新增 `G8zB`/`111`/v1/`IsPublished=1`）、`WorkflowInstances` 7 条 |

> 与附录 M 的对照：M 阶段 ④ 恒 400/500；本次 ④ 首次成功。**"保存假成功"链路彻底消除**（D2=B 的回读校验也一并验证：未生效即抛，不再静默）。

### O.5 数据零残留

清理口径：`MCPTools` 走 API 删除；`PlugDataZones`/`Jobs`/`PlugVariableDatas` 按 Id 段库删；`Plugs.ActivityJsonData` 还原为原 88 B；`main-elsa.db` 删除 `G8zB` 的定义与实例。

与 `_bak_20261030_P8/main.db` **逐表内容比对（行数 + 全行 sha256 前 16 位）**：

| 表 | 当前 | 备份 | 一致 |
|---|---|---|---|
| Plugs | 1 / `bf90a07358d66b19` | 1 / `bf90a07358d66b19` | ✅ |
| MCPTools | 0 / `4f53cda18c2baa0c` | 0 / `4f53cda18c2baa0c` | ✅ |
| PlugDataZones | 2 / `b5b681ee1918422f` | 2 / `b5b681ee1918422f` | ✅ |
| PlugDatas | 1 / `ebcfcca8a11e10d9` | 1 / `ebcfcca8a11e10d9` | ✅ |
| Jobs | 0 / `4f53cda18c2baa0c` | 0 / `4f53cda18c2baa0c` | ✅ |
| PlugVariableDatas | 0 / `4f53cda18c2baa0c` | 0 / `4f53cda18c2baa0c` | ✅ |

`main-elsa.db` 回到 6 条定义 / 6 条实例（本轮前基線）。

### O.6 遗留与后续

| 项 | 状态 |
|---|---|
| M.4-#1 `entryVariables` 恒 `[]` | ✅ **已收口（附录 Q.3）**：原归因「CopyPDZ 复制错表」经复验推翻，实为数据空态；顺带修掉真缺陷（`PublishToolAsync` Include 不全） |
| M.4-#2 `DELETE /api/pdz/deletePDZ/{id}` 恒 500 | ✅ **已修（附录 Q.2）**：`FindAsync` 用 string 打 int 主键，改按业务键查 + 数字回退 |
| `02.Publish/start-services.cmd` 仍起 4 进程 | ✅ 已改为**只起 `CJPlug.Server`**（P9，原件备份 `start-services.cmd.bak4proc`） |
| `CJPlug.Server` 中立宿主（P9） | ✅ **已完成**，见 `（已完成）CJPlug.Server中立宿主方案-20261030.md` |
| CJCore 侧是否需要"认证方案覆盖"的对外文档 | ✅ 已补：`CJCore/docs/CJCore-登录认证统一架构方案.md` §8（`IAuthSchemeOverrideSelector` 契约 + 约束 + 实测） |

## 附录 P：P9 中立宿主 CJPlug.Server 实施记录（2026-10-30）

> 详细方案与逐项改动见 **`（已评审）CJPlug.Server中立宿主方案-20261030.md`**（D1 = A，含 §9 实施记录）。本附录只留验收结论与跨文档引用。

### P.1 结论

**CJPlug 的宿主已由 `CJ.Plug.ApiServer` 改为 `CJPlug.Server`**（新项目 `src/Server/CJPlug.Server`，`Microsoft.NET.Sdk.Web`）；`CJ.Plug.ApiServer` 降级为**类库**，只保留打包发布业务（PackageApi / PackageService / Hubs / Infrastructure）+ 两个扩展方法。

装配入口从此**物理唯一**：改之前分裂在 `ApiServer/Program.cs`（553 行）与 `HostWebStartup.cs`（552 行）两处，边界只写在注释里 —— 2026-10-30 上午的"补丁放错位置 ⇒ 重复注册 ⇒ 17677 条 ESTABLISHED"就是它导致的。

### P.2 顺带决策：`CJ.Plug.Setup` 废弃

用户决策：**CJPlug 安装器废弃，交付统一走 CJSuite 打包与安装器**。取证确认它早已弃用（不在 sln、不在打包脚本、无项目引用），已物理删除 `src/Framework/CJ.Plug.Setup`。

### P.3 验收（单进程 Debug，全绿）

| 项 | 结果 |
|---|---|
| 构建 | `CJPlug.Server` 0 错误；全解构建见 §P.4 |
| 启动 | `Now listening on http://[::]:5066`、MASS-ARCHIVE 拦截 **0 次** |
| 模型集 | `Module Db EntityConfig` **42 行**（21 模块 × 迁移引擎两次建模，与附录 O 合并形态同值） |
| 五面 | `/` 200、`/three-role/login` 200、`/api/mcp/getTools` 200、`/api/plugbridge/tools` 200、`/api/pdz/getAllPDZ` 200 |
| 认证接缝（附录 O） | `POST /elsa/api/identity/login` 200；无凭据 `/elsa/api/*` 仍 302 到登录页（行为不变） |
| Elsa 链 | `addTool`(Id=4) → `call` **200 / 1.13s / 实例 ID `a86ac1f5cdff5205`** → 定义入库 → `deleteTool/4` 200 → `getTools=[]` |
| 数据 | 与 `_bak_20261030_P8/main.db` 逐表 hash **全等**；`main-elsa.db` 回 6 条定义 |

### P.4 待补

- ~~全解构建结果~~：`dotnet build CJ.Plug-Aspire.sln` **0 错误**（139 条既有警告）—— ApiServer 类库化 + Setup 删除未破坏任何项目。
- ~~`assemble-release.ps1 -Target main` 实跑一次~~：**已完成（2026-10-30，见附录 Q.5）**，0 错误、断言通过、发布根清单正确。
- ~~`02.Publish/CJ.Plug.AspireHost.AppHost`（含旧 Release）与 `02.Publish/CJ.Plug.Setup`~~：**已移出（见附录 Q.4）**，落在 `CJPlug/_trash_20261030/`（1.5 GB），确认无误后可整目录删除。

---

## 附录 Q：M.4 遗留缺陷收口 + 打包实跑（2026-10-30）

> 本轮无新决策点，都是**已登记缺陷的收口**与**误诊纠正**。唯一新抛出的决策点是 Q.6（回退形态去留）。

### Q.1 动作清单

| # | 项 | 结果 |
|---|---|---|
| 1 | `DELETE /api/pdz/deletePDZ/{PDZId}` 恒 500（M.4-#2） | **已修**，实测三个场景全通（Q.2） |
| 2 | `entryVariables` 恒 `[]`（M.4-#1） | **定性纠正**：原归因错误（Q.3）；顺带修掉一个**真实**缺陷 |
| 3 | `CopyPDZ` 无 null 守卫 | **已加**：子集合归一成空集合，语义 = "无子项可复制" |
| 4 | 陈旧产物目录（1.5 GB） | **已移出**工作路径（Q.4） |
| 5 | 发布根组装实跑 | **通过**（Q.5） |
| 6 | CJCore 侧「宿主自带身份体系的接缝」文档 | **已补**（`CJCore/docs/CJCore-登录认证统一架构方案.md` §8） |

### Q.2 M.4-#2：`deletePDZ` 恒 500 的根因与修复

**根因（一行代码，类型错配）**：

```csharp
// 改前
var pdz = await _dbContext.Set<PlugDataZone>().FindAsync(new object[] { PDZId }, ct);
```

`PlugDataZone` 的主键是 `Id(int)`，而路由 `/api/pdz/deletePDZ/{PDZId}` 传进来的是**业务键 `PDZId`（string，如 `MCP_Use0_G8zB`）**。`FindAsync` 按主键查找，string 塞进 int 主键 ⇒ EF 抛键值类型异常 ⇒ 端点恒 500。**与数据库、与方法/路径、与权限都无关** —— 这也是 M.5 当时只能"改由库级删除"的原因。

**修复**（`PDZManageService.DeletePDZ`）：

| 改前 | 改后 | 效果 |
|---|---|---|
| `FindAsync(PDZId)` 按 int 主键找 string | `FirstOrDefaultAsync(p => p.PDZId == PDZId)`；未命中且入参是纯数字时再按 `Id` 回退 | 业务键命中；旧调用方传主键也兼容 |

子表清理依赖**库侧级联**（`PRAGMA foreign_key_list` 实测：`PDZVariable` / `PlugVariableDatas` / `PlugDatas` / `PlugStatusDatas` / `ActionDatas` / `ActionVariableDatas` / `DataFlowDatas` 对 `PlugDataZones` 全部 `ON DELETE CASCADE`），不需要逐个 Include 后再删。

> 顺带登记（本轮**未改**）：同文件 `DeleteByFilter` 用 `p.PDZId.Contains(filter.PlugDefinitionId)` 做**子串**匹配 —— 传 `G8` 会连 `adminG8zBDesi` 一起删。`DeletePDZByDeletePlug` 走的正是这个方法，属"删插头时连带删 PDZ"的批量路径，误删面较大，建议后续改为按 `PlugDefinitionId` 精确匹配 + `Type` 过滤。

**实测**（单进程 `CJPlug.Server` @5066，Debug）：

| 场景 | 改前 | 改后 |
|---|---|---|
| 不存在的 PDZId（`__no_such_pdz__`） | 500 | **200 + `false`** |
| 纯数字未知主键（`999999`） | 500 | **200 + `false`** |
| 真实 PDZ（`__smoke_del__`，带 1 条 PlugDatas + 1 条 PlugVariableDatas） | 500 | **200 + `true`**，三表子行同时归零 |
| 真实 PDZ（`MCP_Use0_ZzSmokePlug` / `adminZzSmokePlugDesi`） | — | **200 + `true`**（发布产生的 PDZ 现在能通过 API 清理） |

### Q.3 M.4-#1：`entryVariables` 恒 `[]` 是**误诊**，但底下压着一个真缺陷

原记载（附录 M.4）是「**Use0 PDZ 复制了壳，没复制变量** —— `CopyPDZ` 复制的是 `PlugVariableDatas`，该表与 `PlugDatas` 是两张表」。**这句归因是错的**，直接读代码即可推翻：

`PlugDataZone.CopyPDZ` 依次复制 **7 个集合**（`PDZVariables` / `PlugVariableDatas` / `PlugDatas` / `PlugStatusDatas` / `ActionDatas` / `ActionVariableDatas` / `DataFlowDatas`），并不存在"复制错表"。

**真因（数据空态，直读库取证）**：

| 表 | 值 | 说明 |
|---|---|---|
| `PlugVariableDatas` | **0 行** | 整个库没有任何 PDZ 变量行 |
| `PDZVariable` | **0 行** | 同上 |
| `PlugVariables` | **0 行** | Plug 定义层也没有变量 |
| `PlugDatas`（PDZ 1 = `adminG8zBDesi`） | 1 行（`Name='111'`） | 唯一的插头数据，不是变量 |

⇒ `G8zB` 这个 plug **本身没有变量定义**，复制出来的 Use0 PDZ 自然是 0 条 ⇒ 日志 `No PlugVariableDatas found. count=0` ⇒ `entryVariables=[]`。**它不是 bug，是空数据。**

**顺带查出的真缺陷**（这才是要修的东西）：

```csharp
// 改前：PublishToolAsync 查 Desi PDZ 时只 Include 一个集合
.Include(p => p.PlugVariableDatas)
// 而 CopyPDZ 要遍历 7 个集合 ⇒ 其余 6 个是空集合 ⇒ 复制出来的是"缺壳"的 Use0 PDZ
```

| 改前 | 改后 | 效果 |
|---|---|---|
| `PublishToolAsync` 只 Include `PlugVariableDatas` | 与 `CopyPDZ` 遍历的 7 个集合**完全对齐** | Use0 PDZ 的壳（`PlugDatas` / `PDZVariables` / 动作/数据流）不再丢 |
| `CopyPDZ` 直接 `foreach`（未 Include 时导航为 null 会 NRE） | 方法入口把这些集合 `??= new()` | 调用方 Include 不全时降级为"无子项"，不炸 |

**用带变量的源复验（这才是 M.4-#1 的正确判据）**：造一个源 Desi PDZ（`PlugDatas` 1 / `PlugVariableDatas` 2 / `PDZVariable` 1），走 `POST /api/mcp/addTool`：

| 检查项 | 结果 |
|---|---|
| `MCP_Use0_ZzSmokePlug` 是否创建 | 是（Id=20，Type=`Use0`） |
| 复制出的 `PlugDatas` / `PlugVariableDatas` / `PDZVariable` | **1 / 2 / 1**（与源一致） |
| `POST /api/mcp/addTool` | 200 |
| `GET /api/mcp/getPublishedWorkflows` 的 `entryVariables` | **2 条**（`v1` / `v2`，`type=String`）——不再是 `[]` |

> 结论：**链路上没有"变量复制不了"的缺陷**；M.4 当时看到 `[]` 是因为那条数据本身没有变量。修复的价值在"壳"（缺少 `PlugDatas` 时，Use0 PDZ 无法作为参数模板供后续 UI/执行链使用）。

### Q.4 数据零残留（与备份逐表比对）

备份：`02.Publish/Services/_bak_20261030_pdzfix/main.db`。比对方式：行数 + 全行 sha256 前 16 位。

| 表 | 现值 | 备份 | 一致 |
|---|---|---|---|
| Plugs | 1 / `21a721f495486690` | 1 / `21a721f495486690` | ✅ |
| MCPTools | 0 | 0 | ✅ |
| PlugDataZones | 2 / `4459fdd1e83d925d` | 2 / `4459fdd1e83d925d` | ✅ |
| PlugDatas | 1 / `6e717e1563b97ba9` | 1 / `6e717e1563b97ba9` | ✅ |
| Jobs | 0 | 0 | ✅ |
| PlugVariableDatas | 0 | 0 | ✅ |
| PDZVariable | 0 | 0 | ✅ |
| `AuditLogs` | 561 | 561 | ✅（无新增） |

仅 `sqlite_sequence` 前移（自增不复用，属预期）。

**产物清理**：`02.Publish/CJ.Plug.Setup`（1047 MB）、`02.Publish/CJ.Plug.AspireHost.AppHost`（228 MB，含旧 Debug/Release，项目源码与 sln 条目早已不存在）→ 移入 `CJPlug/_trash_20261030/`，**未直接删除**（可回退）。确认无误后可直接删该目录。

### Q.5 发布根组装实跑（`assemble-release.ps1 -Target main`）

| 检查项 | 结果 |
|---|---|
| 退出码 | **0** |
| 构建 | 整解 Release，**0 错误** |
| 断言 `Services\Release\net10.0\CJPlug.Server.dll` | **通过**（P9 改的断言生效） |
| 发布根结构 | `CJCore.AppHost.Manager` / `CJCore.AspireHost.AppHost` / `PlugConfig` / `Services` |
| AppHost 清单 `Resources[]` | **2 条**：`apiservice`=`CJPlug.Server.dll`@5066（Locked）、`stationapi`@7660；**不再出现** DispatchServer/McpServer/WebFrontend |
| 耗时 | 1 分 25 秒 |

> 说明：`Services/Release/net10.0` 仍是**共享输出目录**，所以包里同时存在 `CJ.Plug.DispatchServer.exe` / `CJ.Plug.HostWebServer.exe` / `CJ.Plug.McpServer.exe` / `CJ.Plug.ElsaApiServer.exe` 等文件 —— 但**清单不启动它们**，属于"冗余体积"，不是行为错误。这正是 Q.6 的决策对象。

### Q.6 决策点：回退形态的去留（**已决议 D2 = B，执行记录见 Q.8**）

现状取证：

| 形态 | 产物 | 清单是否启动 | 源码 |
|---|---|---|---|
| `CJPlug.Server`（唯一宿主） | `CJPlug.Server.exe` | ✅ 启动（5066） | `src/Server/CJPlug.Server` |
| `CJ.Plug.HostWebServer` | `.exe` | ❌ | 仅剩**回退薄壳 Program.cs** |
| `CJ.Plug.DispatchServer` | `.exe` | ❌ | 完整 |
| `CJ.Plug.McpServer` | `.exe` | ❌ | 完整 |
| `CJ.Plug.ElsaApiServer` / `ElsaStudio` | `.exe` | ❌ | 完整（Elsa 独立进程形态） |

| 选项 | 做法 | 收益 | 代价 |
|---|---|---|---|
| **A** | **保留**回退形态（现状不动），只在文档里写清"非交付路径" | 零风险；排查时可单独起某个面 | 包体冗余（多个 exe + 各自 deps/runtimeconfig）；新人误起旧 exe 会重演"双宿主同库"事故 |
| **B** | **删除**回退 exe（HostWebServer / DispatchServer / McpServer / ElsaApiServer / ElsaStudio 全部类库化或移出 sln） | 包体瘦身、装配入口真正唯一、杜绝误起 | 工作面较大；Elsa 独立进程形态若仍被工位机/Studio 依赖，需要逐个确认 |
| **C** | **折中**：只删 `HostWebServer`（已无独有价值，全站已在 `CJPlug.Server`），其余保留 | 干掉最危险的那个（它能独立起一个"半套"UI 宿主，正是 2026-10-30 事故的引信） | 冗余仍在，只是去掉了最危险项 |

> 建议 **C 起步、B 作后续**：`HostWebServer` 作为独立宿主已无任何独有能力（`WebHostStartup` 被 `CJPlug.Server` 完全覆盖），留着只会被误起；DispatchServer/McpServer/ElsaApiServer 还承担"单面排查"价值，可等发版节奏允许时再收。

### Q.8 D2 = B 执行记录：回退形态一次全删（2026-10-30）

> 决策：**B** —— 旧的多进程独立形态全部下线，装配入口物理唯一。执行前先做了交叉核对，确认不是"砍 regulate 功能"。

#### Q.8.1 先分层：能不能删，取决于有没有人引用

| 项目 | 被 ProjectReference | 源码资产 | 处置 |
|---|---|---|---|
| `CJ.Plug.DispatchServer` | ✅ (CJPlug.Server) | MainHub / DispatcherApi / StationService | **类库化**（保留源码，删启动壳） |
| `CJ.Plug.McpServer` | ✅ | Tools/ Services/ Bridge/ | **类库化** |
| `CJ.Plug.HostWebServer` | ✅ | Agent / A2A / SystemConfig / razor 页面 | **类库化**（必须保留 `Sdk.Web`，含 razor） |
| `CJ.Plug.ElsaApiServer` | ❌ 0 引用 | 仅 Program.cs | **移出 sln**（不参与构建） |
| `CJ.Plug.ElsaStudio` | ❌ 0 引用 | 仅 Program.cs + Error.cshtml.cs | **移出 sln** |

#### Q.8.2 交叉核对结论（这是能动手的前提）

逐条比对 3 个旧 `Program.cs`（66 / 116 / 90 行）与 `CJPlug.Server\Program.cs`（569 行，由 ApiServer 的 553 行逐字搬迁而来）：

- **注册/管道 0 条真缺失** —— 每条都能在宿主侧找到对应位置，或被更上层扩展方法等价覆盖；
- **8 条语义差异**（融合时的刻意变更）：NSwag 双文档合并、`MapDefaultEndpoints` 位置、OTel 由 `AddSource("*")` 收窄到 ServiceDefaults、日志 sink 名 `Front`→`API`、**授权由全局改为按路径**（`NeedsIdentity`）、**MCP 路径 `/`→`/mcp` + 端口 3001 消亡**、工具刷新改为启动后异步；
- **2 条必须在删之前登记**：主机保护蜡 MCP 端口契约、前 3 秒 ListTools 空窗；
- 明确**不要**把旧 `Program.cs` 里的 `ConfigModuleDbConfigServices()` / `MapRemoteDesktopApi()` 补回 HostWebStartup —— 会造成 21 条 DbConfig 重复注册 + 端点名重复致全站 500（2026-10-30 上午已踩过）。

#### Q.8.3 处置清单

| # | 位置 | 改动 |
|---|---|---|
| 1 | 三个 csproj | 加 `<OutputType>Library</OutputType>` + 说明注释（HostWebServer 保留 `Sdk.Web`） |
| 2 | `DispatchServer/McpServer/HostWebServer/Program.cs` | **物理删除**（顶层语句在 Library 下是 CS8805 硬错误；源码其余全部保留） |
| 3 | `CJ.Plug-Aspire.sln` | 移除 `ElsaApiServer` / `ElsaStudio`（源码目录保留，可随时加回） |
| 4 | `05.Build/assemble-release.ps1` | 新增 `Assert-Absent`（**负向断言**）+ `Remove-StationStandaloneExe`；原 `McpServer.dll`/`DispatchServer.dll` 正向断言改为断言 **exe 不存在** |
| 5 | 4 处 3001 口径注释 | `CJPlug.Server/Program.cs`、`PlugPortGate.cs`、`PlugMcpBridgeEndpoints.cs`、`PlugMcpToolExecutor.cs` 全部更正为 **5066 `/mcp`** |
| 6 | `McpServer` / `HostWebServer` 的 launchSettings.json | profiles 置空（类库下 `dotnet run` 会失败，避免误读成"服务崩了"） |
| 7 | 输出目录残留 | `CJ.Plug.Elsa*`（40 个文件）移出共享输出目录，避免被 robocopy 搬进发布包 |

#### Q.8.4 验收（Release 配置，实机）

| 项 | 结果 |
|---|---|
| `dotnet build CJPlug.Server` | **0 错误** |
| 输出目录 exe | `CJ.Plug.DispatchServer.exe` / `McpServer.exe` / `HostWebServer.exe` **全部消失** |
| `assemble-release.ps1 -Target main` | 退出 **0**、0 错误、负向断言通过、图站独立 exe 剔除 2 个、main 载荷 857.8 MB（耗时 1m08s） |
| 发布包 Services 面 exe | 只剩 `CJPlug.Server.exe`（+ 遗留的 `CJ.Plug.AIChat.exe`，见 Q.7） |
| Release 实机八面 | `/` 200、`/three-role/login` 200、`/api/mcp/getTools` 200、`/api/plugbridge/tools` 200、`/api/pdz/getAllPDZ` 200、`/api/dispatch/getStations` 200、`/mcp` 200、`/health` 200 |
| 调度 Hub（原 DispatchServer 独立进程 8686） | `POST /mainHub/negotiate` **200** |
| Elsa 认证接缝（附录 O） | 无凭据 `GET /elsa/api/workflow-definitions` → **302 → /three-role/login**（行为不变）；资源契约仍由 Elsa 自己应答 JSON，未被 CJCore 拦成登录页 HTML |
| 数据 | 七张表与备份逐表 hash **全等**；`AuditLogs` 563（+2，本次冒烟产生，按合规保留） |

### Q.7 剩余待办（按优先级）

| # | 项 | 归属 |
|---|---|---|
| 1 | **`CJ.Plug.AIChat`** 仍是 exe 且不在任何清单里：它被 `CJ.Plug.AI` 引用但自身仍产出 exe，需确认其端点是否已被 `CJPlug.Server` 覆盖后再决定类库化还是保留 | 待你确认 |
| 2 | `CJ.Plug.ApiServer/PackageService.cs` 生成的 `start.bat` / `docker-compose` 仍按旧多进程写（`HostWebServer.exe`、`McpServer.exe`，端口 8687/8690）⇒ 交付物(zip) 用户拿到就坏；需改成单进程 `CJPlug.Server` | 建议下轮 |
| 3 | `寸金 - 快捷方式.lnk` 指向旧 exe，需人工重建（二进制，不可脚本化） | 人工 |
| 4 | 部署配置文件名仍为 `appsettings.CJ.Plug.ApiServer.json`（有意沿用，改名需同步 DefaultResources + 现场文件） | 独立迁移 |
| 5 | `_trash_20261030/`（1.5 GB + 40 个残留文件）确认后删除 | 由你确认 |
| 6 | 三个被类库化项目的 `appsettings.json` 仍会复制进共享输出目录（构建顺序决定谁赢，实测 `InternalApi` 段已被覆盖丢失）——改 `<Content Remove>` 会改变运行时读到的配置，风险需单独评估 | 建议单独立项 |
| 7 | P7.1a 退役 `CJ.Plug.SystemConfig`、P7.2 许可证、C3 IP 白名单 | 你说先不做 |

---

## 附录 R：配置归一（2026-10-30，P10）

### R.1 问题形态（为什么必须归一）

`02.Publish/Services/{Config}/net10.0/` 是**所有服务共享的输出目录**，归一前那里躺着 6 份配置，谁生效取决于构建顺序：

| 文件 | 内容归属 | 归一前状态 |
|---|---|---|
| `appsettings.json` | **工位机**风格（`main333.db`、Guacamole、MainServer 6660） | md5 与 Dispatch/Mcp/HostWeb 三份完全相同 —— 谁最后构建谁赢 |
| `appsettings.CJ.Plug.ApiServer.json` | 主服务权威配置（连接串 `../../main.db`、`InternalApi`、`ThreeRoleAdmin`） | 由 `Directory.Build.targets` 按项目名自动生成 |
| `appsettings.CJ.Plug.{DispatchServer,McpServer,HostWebServer}.json` | 与根配置内容相同 | 同上，类库化后纯冗余 |
| `appsettings.CJ.Plug.StationApiServer.json` | 工位机专属（`RemoteDesktop:RfbWindow`） | 唯一有存在必要的专属配置 |

**危害**：不经 AppHost、直接 `dotnet CJPlug.Server.dll` 时没有 `ASPNETCORE_CONFIG_PATH`，服务只读根 `appsettings.json` ⇒ 连到 `main333.db`（空库，会现场建库并跑种子）、`ThreeRoleAdmin` 段缺失 ⇒ 用默认 `123456` ⇒ **无任何报错**。这类"配置静默回退"是最难查的故障。

### R.2 方案（唯一权威文件 + 三条护栏）

| 决策 | 内容 |
|---|---|
| 唯一配置 | 主服务的配置只有 `{执行目录}/appsettings.json`，由 `src/Server/CJPlug.Server/appsettings.json` 提供（内容 = 原 ApiServer 专属配置，逐键核对 31 → 38 键、同键同值、零丢失） |
| 复制方式 | **不走 `<Content CopyToOutputDirectory>`**，改由 `Directory.Build.targets` 的 `CopySharedRootAppSettings` target 复制（`SkipUnchangedFiles=false`） |
| 类库闭嘴 | ApiServer / DispatchServer / McpServer / HostWebServer 的 `appsettings.json` 一律 `CopyToOutputDirectory=Never` |
| 工位机例外 | StationApiServer 的 `appsettings.json` 也设为 `Never`，专属文件由 `CopyAppSettingsWithProjectName` **白名单**生成（当前白名单只有它一个） |
| AppHost | `ConfigFile = "appsettings.json"`；并新增**旧名纠正**：现场清单若仍写 `appsettings.CJ.Plug.ApiServer.json` 等已下线名称，启动时自动改为 `appsettings.json` 并打印告警 |

### R.3 踩到的坑（必须记住）

> **`IncrementalClean` 会把主配置删掉。**

`<Content CopyToOutputDirectory="Always">` 看起来最直觉，但它把文件写进 `obj/<Config>/<项目>.csproj.FileListAbsolute.txt`，等于声明"这个输出归我管"。工位机项目历史上也复制过同名 `appsettings.json`，一旦把它改成 `Never` 再构建，`IncrementalClean` 就按"上一轮我写的、这一轮不再写"把它**删除** —— 实测构建后目录里只剩 `appsettings.Development.json`，主配置消失且无任何警告。

走 Target 复制的文件不进 `FileListAbsolute.txt`，任何项目重新构建都不会动它。构建顺序鲁棒性已实测（Station → Server → Station 三步，根配置全程存活）。

### R.4 验收

| 项 | 结果 |
|---|---|
| 构建 | Debug/Release 均 **0 错误**；`assemble-release.ps1 -Target main` 退出 0，main 载荷 858.2 MB |
| 输出形态 | `appsettings.json`（38 键，`PlugDb=../../main.db`）+ `appsettings.CJ.Plug.StationApiServer.json` + `appsettings.Development.json`，类库专属配置 **0 个** |
| 直启冒烟（不传任何配置环境变量） | 六面 `/`、`/three-role/login`、`getAllPDZ`、`getTools`、`plugbridge/tools`、`/health` **全 200**；`/elsa/api/workflow-definitions` 无凭据仍 **302**；admin 登录 200 |
| 连库判据 | **未产生 `main333.db`**，日志 `[SeedData] 已存在，跳过` ⇒ 连的是既有 `main.db` |
| 数据 | 七张业务表与 `_bak_20261030_pdzfix` 逐表 hash 全等 |
| 打包护栏 | 新增正向断言（`appsettings.json` + Station 专属必须存在）与负向断言（4 个类库专属配置不得出现） |

### R.5 剩余

| # | 项 | 说明 |
|---|---|---|
| 1 | `appsettings.Development.json` 仍在共享目录（HostWebServer 的） | 只在 Development 环境生效，而 Development 起不来（见 D2）；不动 |
| 2 | `ConnectionStrings:Sqlite = main333.db` 是历史遗留键 | CJPlug 主业务走 `PlugDb`/`DatabaseConfig`，但 CJCore 的 `DbConnectionProviderBase` 有 `GetConnectionString("Sqlite")` 兜底 ⇒ 保留（改动属另一议题） |
| 3 | 配置里的 `ThreeRoleAdmin:AdminPassword` 与库内实际密码可能不一致 | 配置只影响**种子初始化**；既有库改配置不会改密码。当前库内 admin 实测密码为 `Admin@2026` |
| 4 | 三员登录页底部提示"默认账号 admin / 123456"来自 CJCore 组件硬编码文案 | 接入方覆盖默认密码后提示就对不上，建议改为读配置或不显示 |
| 5 | `ThreeRoleAdmin:AdminPassword` 现值 `123456` | **用户手改，按用户值保留**（2026-10-30 确认）。它只影响种子初始化，不改既有账号密码 |

---

## 附录 S：工位机配置形态核验 + 载荷清理（2026-10-30，P11）

### S.1 问题

用户问："工位机的 `appsettings.json` 应该是独立的、和工位机程序在同目录对吧？"——归一后需确认工位机没有被主服务的归一动作误伤。

**结论：是，独立且同目录，本来就是。** 两套形态并存，互不冲突：

| 形态 | 位置 | 配置文件名 | 谁在读 |
|---|---|---|---|
| 开发态（单机编排） | `02.Publish\Services\<Cfg>\net10.0\` | 根 `appsettings.json` = 主服务；`appsettings.CJ.Plug.StationApiServer.json` = 工位机 | AppHost 用 `ASPNETCORE_CONFIG_PATH` 把工位机指向专属文件 |
| 交付态（独立发布） | `05.Release\station\ApiServer\` | `appsettings.json`（与 exe 同目录） | 工位机自己 |

`StationApiServer.csproj` 里的 `<Content Update="appsettings.json"><CopyToOutputDirectory>Never` **只作用于共享输出目录的 Build，不影响独立 `dotnet publish`**（实测 publish 仍产 `appsettings.json`，36 键全为工位机配置）。这是两套形态能不冲突的机制前提。

### S.2 清理项 1：`CJ.Plug.AIChat` 类库化

| 项 | 内容 |
|---|---|
| 现象 | `station\ApiServer\` 里混入 `CJ.Plug.AIChat.exe`（不在任何服务清单、不该被启动） |
| 传递链 | `StationApiServer → ModuleConfig → CJ.Plug.AI → CJ.Plug.AIChat`；后者 `OutputType` 默认 Exe ⇒ 被 publish 复制出来 |
| 定性 | 该项目本来就是 **UI 组件 + 服务注册库**（`StaticWebAssetProjectMode=Reference` 已是类库语义），`Program.cs` 只是开发者单机调试壳（Ollama `localhost:11434` / 端口 5143） |
| 修改 | `OutputType=Library`；删除 `Program.cs`（顶层语句在 Library 下是 CS8805 硬错误）；`launchSettings.json` profiles 置空；`Components/Routes.razor` 的 `typeof(Program).Assembly` → `typeof(Routes).Assembly`（入口类型随 Program 一起消失） |
| 功能等价性 | `ServiceCollectionExtensions.AddAIChatServices()` 与 Program 的服务注册**逐条等价**（唯一差异：`builder.Environment.WebRootPath` → `GlobalData.MainWebFileServer`），删壳不丢功能 |
| 验收 | 单项目构建 0 错误、不再产 exe；主服务构建 0 错误；主服务实机启动六面全 200 且 `/aichat` **200**（组件承载面正常）；station 载荷 exe 只剩 `CJ.Plug.StationApiServer.exe` |

### S.3 清理项 2：工位机配置里的死键

删掉 `src/PlugStation/CJ.Plug.StationApiServer/appsettings.json` 中三条指向 `../../main.db` 的残留：
`ConnectionStrings:PlugDb`、`ConnectionStrings:ElsaDb`、`DatabaseConfig` 整段。

依据（工位机代码里这些键**无读取方**）：

| 键 | 真实读取方 | 工位机是否走 |
|---|---|---|
| `ConnectionStrings:PlugDb` | `DbConnectionString.ReadFromConfig` → 仅 `WebHostStartup.cs:223`（主服务 `AddPlugWebHost`） | 否 |
| `ConnectionStrings:ElsaDb` | Elsa 集成 | 否（工位机不注册 Elsa） |
| `DatabaseConfig` 整段 | `DbConnectionString` / `ElsaExtensions.cs:248` / Desktop 配置页 | 否 |

工位机的本地库是**硬编码路径**、与连接串无关：`station_config.db`（`StationConfigHelper`）、`station_tasks.db`（`StationTaskStore`，`AppContext.BaseDirectory`）。工位机 `Program.cs` 全文只读三个配置键：`FileServer:ToolAgentServer`、`RemoteDesktop:RfbWindow`、`InternalApi:Key`。

顺带说明：`../../main.db` 在 `station\ApiServer\` 下会解析到 `05.Release\main.db`（不存在）——正因无读取方所以从未暴露，但留着会让后来者误以为工位机连主库。

### S.4 一个不是缺陷的现象：工位机连 `127.0.0.1:8686`

本机冒烟时工位机连 8686（旧调度端口，已并入 5066）而非 5066。取证结论：**本机环境残留，不是交付缺陷**。

- 主服务地址优先级：`%ProgramData%\CJStation\station_config.db` 的 `MainServerUrl` → 环境变量 `MAIN_SERVER_HOST` → `GlobalData.MainDispatcherServer`（默认已是 `http://localhost:5066`）。
- 本机 `C:\ProgramData\CJStation\station_config.db`（2026-10-07 遗留）里写着 `MainServerUrl=http://127.0.0.1:8686`，优先级最高，把正确默认值盖掉了。
- 交付到客户机（无该文件）时走默认 5066，行为正确。**本机联调需先把该文件里的值改成 5066 或删掉该文件。**
- 注意 `appsettings.json` 的 `MainServer:Url` / `Backend:Url` **不是**工位机主服务地址的来源（`Program.cs:107` 的 `GetSection("MainServer")` 已被注释）。

### S.5 验收

| 项 | 结果 |
|---|---|
| `-Target station` 实跑 | 退出 0，station 484.3 MB（比清理前少 0.1 MB ≈ AIChat.exe 体积） |
| `station\ApiServer\` | exe 仅 `CJ.Plug.StationApiServer.exe`；`appsettings.json` 顶层段 `Logging/AllowedHosts/ConnectionStrings/Backend/Guacamole/MainServer/FileServer/RemoteDesktop`；`ConnectionStrings` 只剩 `Sqlite/Sqlite2/RabbitMq`；无 `DatabaseConfig` |
| 主服务冒烟 | 构建 0 错误；启动 `Now listening http://[::]:5066`；六面 200、接缝 302 不变、`/aichat` 200 |
| 工位机冒烟 | 启动正常：`the service port is:7660` / `Now listening http://[::]:7660` / `Application started`，删配置未引发任何配置异常 |
| 缓存清理 | 共享输出目录残留的 `CJ.Plug.AIChat.exe` 与冒烟产生的 `StationLogs` 已移入 `_trash_20261030/aichat-lib/` |
| 防回归断言 | `assemble-release.ps1` 两个 target 各加一条 `Assert-Absent`：main 的 `Services\…\CJ.Plug.AIChat.exe`、station 的 `ApiServer\CJ.Plug.AIChat.exe`。理由同 P9.1——只做正向断言等于永远为真，某天有人把 `OutputType` 改回 Exe 就发现不了 |
| 打包复验 | `-Target station` 与 `-Target main` 均退出 0；main 三段构建 0 错误、858 MB；main 载荷内只剩 `CJ.Plug.AIChat.dll` + staticwebassets（主服务 AI UI 需要），无 exe |

### S.6 剩余

| # | 项 | 说明 |
|---|---|---|
| 1 | `MainServer:Url`（`localhost:6660`）/ `Backend:Url` / `Guacamole` 段在工位机配置里也是死键 | 本轮只清用户指定的三条 `../../main.db` 残留；这些是否一并清，待定 |
| 2 | `C:\ProgramData\CJStation\station_config.db` 的值仍是 8686 | 属本机环境数据，不擅自改；需人工更新或删除 |
| 3 | `DbConnectionString.FindConfigPath()` 绕过 `IConfiguration`，硬编码读仓库根的 `src\PlugApiServer\CJ.Plug.ApiServer\appsettings.json`，无 src 时回退硬编码 `../../main.db` | 配置归一的漏网点。当前只被主服务 `WebHostStartup.cs:223` 调用，属独立议题 |

---

## 附录 T：P6.4 模块启停实机验收（2026-10-30，P12）

### T.1 结论先行（12:0x 已更正 —— 见 T.7）

**P6.4 判据对"已声明 AccessKey 的模块"成立，对"未声明的模块"不成立。框架三面链路（菜单 / AppBar / 页面）全部已通，缺的是声明覆盖。**

> ⚠ **本节初判已证伪。** 11:55 首轮只测了 `plug-knowledge`（其菜单无 `AccessKey`）就得出"整条能力从未落地"，
> 12:0x 用 `plug-job-manage`（有 `AccessKey` + 有 feature 声明）复测，**菜单当场从 24 项变 23 项**，
> 用 `plug-llm-config` 复测页面，**正文变为「无权访问」**。初判错在把"单个样本缺声明"当成"框架缺能力"。
> 完整更正与三面矩阵见 **T.7**；下表与 T.3 保留首轮记录，仅作过程留痕，**不作结论引用**。

`IModuleCatalog` 实现同时充当 `CJCore.Framework.Abstractions.IModuleEnablementGate`，
该闸门已被 `CoreMenu`、`FrameworkLayout`（AppBar）、`ModuleAccessStrategy`（策略层）三处消费 —— **链路本就完整**。

### T.2 验收过程与逐项结果

构建 `dotnet build src/Server/CJPlug.Server/CJPlug.Server.csproj -c Release` → **0 错误 / 2189 警告**；
启动 `CJPlug.Server.exe`（`ASPNETCORE_URLS=http://*:5066`，CWD 在 `02.Publish/Services/Release/net10.0`）→ **15 s 后 `/health` = 200**；
登录 `POST /api/three-role/account/login`（`admin` / `Admin@2026`）→ **200**，Cookie `.ThreeRole.Auth` + `.ThreeRole.Token` 均下发。

| # | 判据 | 结果 | 证据 |
|---|---|---|---|
| 1 | `/api/modules/` 列出全部模块 | ✅ | **34 个模块**，`plug-*` 30 个 + 框架 4 个（`Framework` / `Framework UI` / `三员管理` / `Permission` / `系统配置` 等）+ 4 个强制模块（`plug-head` / `plug-shell` / `plug-runtime` / `module-admin`） |
| 2 | 关闭模块返回成功 | ✅ | `PUT /api/modules/plug-knowledge/enabled {"enabled":false}` → **200** `{"success":true}` |
| 3 | 状态落库并可回读 | ✅ | 复查 `GET /api/modules/` → `plug-knowledge.enabled = False`（对照 `plug-market = True` 未受影响） |
| 4 | **菜单项即时消失** | ❌ **不成立** | 关掉前后首页侧栏**同为 24 项**，`/KnowledgeManage │ 知识管理` 仍在（`html.unescape` 后统计，非实体误判） |
| 5 | **页面访问被拦** | ❌ **不成立** | 禁用后 `GET /KnowledgeManage` → **200 / 210 818 B**，正文「知识」命中 7 次，无「无权访问」、无 404 页 |
| 6 | 强制模块拒绝禁用 | （未触发） | 端点逻辑 `IsMandatory && !enabled → 403`，本轮未测 |
| 7 | 状态还原 | ✅ | `PUT ... {"enabled":true}` → 200，回读 `True`；验收后无脏状态 |

> 判据 4/5 的失败**与 P5 身份替换无关**：菜单 HTML 是 SSR 预渲染产出（`mud-nav-link-text` 内为 HTML 实体），两侧对比同源同法，排除"渲染方式差异"干扰。

### T.3 初判根因（**已证伪，仅留痕**）

| # | 根因 | 证据 |
|---|---|---|
| 1 | `IModuleCatalog`/`IModuleStore` **只被 ModuleAdmin 自己消费**，框架的菜单与路由装配链里没有任何引用 | `grep IModuleCatalog` 全仓：命中全在 `Modules/ModuleAdmin/**`；`Framework/**` 下 **0 命中** |
| 2 | 菜单可见性只做**权限**过滤，**不做启停**过滤 | `CJCore.Framework.UI/Components/MenuVisibilityRules.cs:27 IsVisibleAsync(...)`：入参只有 `item / user / userRoles / router`，**没有 catalog** |
| 3 | 启停的"下游"只有**插件门控**，且是**装配期（下次启动）**读文件，不是即时事件 | `DefaultModuleCatalog.SetEnabledAsync` → `_store.SetEnabledAsync` + `PublishEnablementSnapshot` + `Changed?.Invoke()`；而 `Changed` 事件全仓**零订阅方**；`module-enablement.json` 的读者只有 `AgentPluginGateFactory` 与 `ModulePluginGateFactory`（注释明写「管理页启停 → 下次启动装配期生效」） |

**推论**：`ModuleAdminEndpoints` 里那句注释「D2=B：即时生效，无需重启」（`ModuleAdminEndpoints.cs:34`）与实现不符 ——
它只对 `IModulePlugin`（Agent 插件 / 模块插件）在**重启后**成立，对 CJPlug 的 34 个 `IModule` 既不时也不延。

### T.4 顺带发现：10 个菜单类没有 `AccessKey`

启停要联动菜单，前提是菜单项能反查到所属模块；而 `AccessKey` 正是这条链上的关联键。现状覆盖不全：

```
NO-AccessKey: FMDModule/CJ.Plug.FMD/Menus/FMDMenu.cs
NO-AccessKey: HomeModule/CJ.Plug.HomePage/Menus/HomePageMenu.cs
NO-AccessKey: KnowledgeManageModule/.../Menus/KnowledgeManageMenu.cs
NO-AccessKey: ModelManageModule/.../Menus/OntologyManageMenu.cs
NO-AccessKey: PlugMarketModule/.../Menus/PlugMarketMenu.cs
NO-AccessKey: SkillsManageModule/.../Menus/SkillsManageMenu.cs
NO-AccessKey: SkillsModule/.../Menus/SkillsManageMenu.cs      ← 与上一行同名的遗留目录
NO-AccessKey: StationManageModule/.../Menus/StationManageMenu.cs
NO-AccessKey: ToolResourceModule/.../Menus/ToolResourceMenu.cs
NO-AccessKey: UnitTestModule/.../Menus/UnitTestMenu.cs
```

后果有两层：① 这些菜单项**不受权限矩阵控制**（走 `MenuItem.Roles` 回退，无 Roles 即对任何登录用户可见）；
② 将来做启停联动时**无法归属模块**。属 P6.3 的欠账（当时只补了 4 个模块的 `IModuleAccessDeclaration` 与 2 个菜单）。

### T.5 待拍板：D-P6-1 模块启停的生效口径

> ⚠ **本节前提已变**（T.7 证伪了"框架缺能力"）。现口径只剩"补齐声明到什么范围"，见 **T.7.5 的 D-P6-2**。
> 下表保留首轮选项，仅作决策过程留痕。

| 选项 | 做法 | 覆盖 | 成本 | 代价 |
|---|---|---|---|---|
| **A（推荐）** | **补齐 `AccessKey` 后做即时联动**：`MenuVisibilityRules` 注入 catalog，按 `AccessKey → moduleId` 判定；AppBar 同一规则；路由由 `ModuleAccessHandler` 侧拦截 | 菜单 + AppBar + 路由，**三面即时** | 中（框架 1 处 + 菜单 10 处 + 路由 1 处） | 框架行为变更，需先出方案评审；每次渲染多一次 catalog 查询（catalog 已是内存快照） |
| **B** | **改为"下次启动生效"**：装配期按 catalog 过滤 `IModule` 注册与菜单聚合，运行期不做判断 | 三面，但需重启 | 中低 | 与 D2=B 的"即时"承诺相反，管理页体验变差（关完没反应） |
| **C** | **收窄口径**：承认启停只对 `IModulePlugin` 生效；把本计划 P6.4 判据与端点注释改成"仅插件级、下次启动" | 无业务面 | 低（只改文档与注释） | "模块管理"页对 34 个模块形同虚设，等于放弃该能力 |

> **推荐 A**：`/api/modules` 已把每个模块的 `featureKey`/`accessKey` 统一成 `moduleId`（如 `plug-process-manage`），关联键现成；
> 补齐 `AccessKey` 本身是 P6.3 欠账，不补则权限矩阵对这 10 个页面也是漏的 —— 无论启停怎么做都得补。
> A 落地后 P6.4 才能真验收，本计划的"接入完成"才有完整出口。

### T.6 顺带发现（登记，不在本轮改）：`PackageService` 整条链已是死代码

| 事实 | 证据 |
|---|---|
| 4 个包生成端点**已全部退役**，统一返回 410 | `PackageApi.cs:14/20/26/32` → `RetiredPackageFeature()`；退役说明见同文件末注释（2026-10-07，改由 CJSuite 出包） |
| `PackageService`（**1280 行**）**零活调用方** | 只在 `PackageServiceCollectionExtensions.cs:11` 被 DI 注册，**从不解析**；4 个退役端点不注入它 |
| 生成的脚本/配置**全是旧多进程形态** | `CreateStartupScripts`：4 个 exe（`ApiServer`/`HostWebServer`/`ElsaApiServer`/`McpServer`）+ 端口 8687/8688/8689/8690；`CreateDockerConfiguration`：4 容器同套端口；`CreateConfigurationFiles`：另建 `config/appsettings.json`（与"配置必须与 exe 同目录"的新形态冲突） |
| 前端仍有 3 处**僵尸入口**打这些端点（点必 410） | `MainPageContent/Pages/DownloadPackage.razor:361/405/444/486`、`StationManage/Dialogs/StationDownloadDialog.razor:93`、`StationSettingUI/Services/StationApiService.cs:271/342/372/397` |

处置建议（待你定）：**整链物理删除**（`PackageService.cs` + `PackageProgress.cs` + `PackageServiceCollectionExtensions.cs` + `PackageApi.cs` + `Program.cs:257/491` 两行注册），
并把 `DownloadPackage.razor` 与 `StationDownloadDialog` 的下载入口一并下线（包分发已由 CJSuite 的 `cjplug`/`cjplug-desktop`/`station-settingui` 三个组件承担）。
理由：留着 1280 行旧形态代码，任何后来者读它都会得出"系统还是多进程"的错误结论。

---

## 附录 T.7：归因更正与三面/四面复验（2026-10-30 12:0x）

### T.7.1 结论反转

**框架侧零缺陷。** `IModuleCatalog` 的实现 `DefaultModuleCatalog` 同时实现 `IModuleEnablementGate`，该闸门已被三处消费：

| 消费点 | 位置 | 用法 |
|---|---|---|
| 菜单 | `CJCore.Framework.UI/Components/CoreMenu.razor:65` | 订阅 `Changed` 重算；条目过滤走 `MenuVisibilityRules` → `IPermissionStrategyRouter` |
| AppBar | `CJCore.Framework.UI/FrameworkLayout.razor:259` | `_gate.IsAccessKeyEnabled(item.AccessKey)` |
| 策略层 | `Permission.Api/Services/ModuleAccessStrategy.cs:65` | **`EvaluateAsync` 第 0 步**：`!_gate.IsAccessKeyEnabled(accessKey)` → 直接 Deny，**先于 AdminRoles 硬保护**（管理员同样受约束） |

DI 也是同一实例：`ModuleAdminModuleExtensions.cs:41` 用 `services.Replace(Singleton<IModuleEnablementGate>(sp => sp.GetRequiredService<DefaultModuleCatalog>()))`，
故 `Changed` 事件在 UI 与目录之间是通的。

### T.7.2 三组对照实验（同一进程、同一会话）

| # | 关闭的模块 | 该模块的声明情况 | 菜单 | 页面直达 | 判定 |
|---|---|---|---|---|---|
| 1 | `plug-knowledge` | 菜单**无** AccessKey、模块 **0 feature** | 24 项**不变** | `/KnowledgeManage` 200 正常渲染 | ❌ 不生效 |
| 2 | `plug-job-manage` | 菜单有 `AccessKey="plug-job-manage"`、有同名 feature | **24 → 23 项**，`/JobManageList│作业列表`**消失** | 200 正常渲染（该页无 `[ModuleAccess]`） | ✅ 菜单生效 |
| 3 | `plug-llm-config` | 菜单有 AccessKey、有 feature、页面有 `[ModuleAccess]` | 消失 | `/LlmConfig` **200 但正文「无权访问」**、内容不渲染（67 KB vs 正常 ~200 KB） | ✅ 菜单+页面双生效 |

> 三个样本的唯一变量就是"有没有声明"。第 1 组是**声明缺失**，不是框架失效。

### T.7.3 机制真相：`AccessKey` 是唯一开关，且必须"有归属"

`_accessKeyIndex` 的构建链（`DefaultModuleCatalog.cs`）：

```
CollectFeaturesAsync()                       ← 三个来源
  ① IModuleAccessDeclaration.AccessKey        Owner = declaration.ModuleName
  ② IMenuService 菜单项的 AccessKey           Owner = ① 里同键的 Owner（无则 null）
  ③ IAppBarService.KeyedAppBarItems.AccessKey Owner = null
        ↓
Scan() → BuildFeatures(moduleId, moduleName, features)
        仅当 IsSameModule(moduleId, moduleName, entry.Owner) 为真才收进该模块的 Features
        （Owner 为 null ⇒ 一律 false）
        ↓
Apply() → _accessKeyIndex 只从 module.Features 建
        ↓
IsAccessKeyEnabled(key)：索引里没有该键 ⇒ **返回 true（放行）**
```

**两条硬结论**：

1. **只给菜单加 `AccessKey` 而不注册 `IModuleAccessDeclaration` 是无效的** —— Owner 为 null，键进不了索引，仍放行。
2. **`ModuleName` 必须等于 `moduleId` 或模块显示名**（`IsSameModule` 只认这两个）。CJPlug 现有 10 个声明传的都是**中文显示名**（如 `"作业管理"`），恰好与 `IModule.Name` 相同，所以能命中 —— 这是个**脆弱的隐式约定**：一旦改模块显示名，门控就会静默失效。

### T.7.4 四面缺口清单（实测，非推断）

`GET /api/modules/` 显示 34 个模块，其中**只有 13 个有 feature（进了索引）**：

```
有 feature（受门控）: plug-process-manage / plug-job-manage / plug-tool-action / plug-execute /
                      plug-pdz / plug-ai(仅 plug-ai:keys) / plug-mcptools / plug-guacamole /
                      plug-llm-config / plug-license / 三员管理(3) / 系统配置(3) / plug-agent
无 feature（门控恒放行）: plug-home / plug-station / plug-tool-resource / plug-market /
                      plug-process-edit / plug-file-manage / plug-unit-test / plug-relation /
                      plug-fmd / plug-skills-manage / plug-knowledge / plug-model-manage /
                      plug-system-config-read
                      （另有 Framework / Framework UI / 个人中心 / Permission /
                        plug-head / plug-shell / plug-runtime / module-admin 属框架侧或强制模块）
```

逐面覆盖：

| 面 | 门控机制 | 声明总量 | 已覆盖 | 缺口 |
|---|---|---|---|---|
| 菜单 | `MenuItem.AccessKey` → `MenuVisibilityRules` | 23 个 `IMenuService` | **6** | 10 个菜单类无 `AccessKey`（T.4 已列） |
| AppBar | `FrameworkLayout:259` 直接查 gate | 2 项 | `plug-runtime`（强制，不需） | `plug-ai` 键**未入索引**（该模块只声明了 `plug-ai:keys`）⇒ AskAI 按钮不受控 |
| 页面 | `[ModuleAccess(key)]` → `ModuleAccessRouteView` | 153 个 `@page` | **11 页 / 7 键** | 142 页无特性 |
| API | `RequireModuleAccess(key)` | 33 个 `MapGroup` | **≈1**（另 1 处是注释） | 几乎全部业务端点未声明 |

### T.7.5 待拍板：D-P6-2 声明补齐的范围（**已定 A，见 T.7.7**）

| 选项 | 做法 | 覆盖 | 工作量 | 评价 |
|---|---|---|---|---|
| **A（推荐）** | **只补"入口面"**：13 个无 feature 的业务模块各补 1 条 `IModuleAccessDeclaration` + 对应菜单 `AccessKey`，并给 `plug-ai` 补一条模块级键 | 菜单全量 + AppBar + 启停可验收 | 约 15 处声明 + 10 处菜单 | 让 P6.4 判据**真实成立**；不动在用 API，风险最低 |
| **B** | A + **页面全量**（142 页补 `[ModuleAccess]`，键按模块归属） | 入口面 + 页面 | 约 150 处 | 工作量集中但机械；与 D9=A「全量矩阵」是同一件事，可合并 |
| **C** | A + B + **API 端点全量**（33 个 MapGroup 补 `RequireModuleAccess`） | 四面全 | 约 200+ 处 | **改动面最大**：任何一处键写错都会让在用 API 变 403，须逐端点回归 |

> **推荐 A**，理由：① P6.4 的判据原文只要求"菜单项与 AppBar 项消失"，A 即可闭环；
> ② 页面/API 的铺开与 **D9=A 全量权限矩阵**是同一批工作，混进来会让"启停验收"和"权限验收"互相污染，出问题难定位；
> ③ A 之后仍可单独开一批做 B/C，且那时有 A 的 13 个键作为样板，成本更低。

### T.7.6 若选 A，落地配方（三件套，缺一不生效）

以 `plug-knowledge` 为例：

```csharp
// ① 菜单：补 AccessKey（键 = moduleId，Q8 定案口径）
new() { Icon = ..., Href = "/KnowledgeManage", Text = "知识管理", AccessKey = "plug-knowledge" }

// ② 声明：ModuleName 必须等于 moduleId 或模块显示名（建议传 moduleId，去掉改名脆性）
services.AddSingleton<IModuleAccessDeclaration>(
    new ModuleAccessDeclaration("plug-knowledge", ["system_admin"], "plug-knowledge"));

// ③ 验收：关模块 → 菜单消失；开回来 → 菜单回来（不必重启）
```

> ⚠ 现有 10 条声明传的是中文显示名，**建议本轮顺手改成 `moduleId`** —— 否则将来改一个模块的中文名，门控会静默失效，且没有任何报警。

> **⚠ 该"顺手改"建议已于 T.7.7 作废**：`ModuleName` 被权限矩阵当作**分组显示名**消费，改传 `moduleId` 会让矩阵分组变成 `plug-job-manage` 这类机器串。改为「约定 + 验收断言」兜底。

### T.7.7 收口：D-P6-2 = A（2026-10-30 12:1x）

**用户拍板 A（只补入口面）。** 立项文档：`CJPlug/docs/（待评审）P6.4模块启停入口面补齐方案-20261030.md`。

**A 的精确范围（比 T.7.5 的粗估更小、更准）**

| 项 | T.7.5 粗估 | 核实后 | 差异原因 |
|---|---|---|---|
| 待补声明 | 13 条 | **9 条** | 13 个零 feature 业务模块中：4 个**无菜单入口**（`plug-file-manage`/`plug-process-edit`/`plug-relation`/`plug-system-config-read`）补了也没有任何键检查它 = 矩阵噪音，归 B；`plug-home` 是导航兜底入口不纳入；余 **8** 个各补 1 条 + `plug-ai` 补第 2 条（现只有 `plug-ai:keys`）= **9** |
| 待补菜单键 | 10 个 | **8 个** | 10 个无 `AccessKey` 的菜单文件中，1 个属 `plug-home`（不纳入）、1 个是**孤儿项目** `skillsModule/CJ.Plug.SkillsManage`（运行期不存在，与 `SkillsManageModule` 同名类同名扩展方法，仅后者被引用） |
| 框架改动 | — | **0 行** | 全部落在 CJPlug 侧 |

**T.7.6 配方的一处更正**

原写「建议 ModuleName 传 moduleId，去掉改名脆性」——**作废**，但理由按实测修正：矩阵分组名是 `ResolveGroupName = FirstNonEmpty(菜单 GroupName, 声明 ModuleName)`（`PermissionFunctionsService.cs:446`，2026-10-21 口径）；**有菜单的键**分组取菜单 `GroupName`，`ModuleName` 不参与显示（改了无效果也无收益）；**无菜单的纯声明键**才回落 `ModuleName`（实测 `plug-ai` 分组显示"AI 助手"即来自此），这类键改传 `moduleId` 会把分组变成机器串。故：`ModuleName` 一律保持模块中文名。

改名脆性实际也被兜住一半：`IsSameModule` 除逐字相等外还有**双向包含**（`moduleName.Contains(owner)`），所以"作业管理"→"作业管理中心"仍能命中，只有**彻底换词**才断。收口方式改为：① 声明注释里写死"`ModuleName` 必须与 `Module.Name` 逐字一致"；② 验收脚本断言"凡 `featureKey == moduleId` 的功能项，其 `name` 必须等于模块 `name`"，漂移自动暴露。

**唯一未定项**：D-P6-4 —— 新增 9 条声明的角色白名单用 `["*"]`（零可见性回归，推荐）还是 `["system_admin"]`（与既有 11 条一致，但非管理员将立即看不到这 8 个菜单 + AskAI）。详见立项文档 §5。

### T.8 P6.4 收口：D-P6-2=A / D-P6-4=A 已落地并验收通过（2026-10-30 12:3x）

**结论：P6.4 判据（关模块 → 菜单/AppBar 消失）现已真实成立。** 框架零改动，只补 CJPlug 侧 9 个文件 / 17 处新增。

| 项 | 内容 |
|---|---|
| 菜单补键 | `plug-fmd`(对象管理) / `plug-knowledge` / `plug-model-manage` / `plug-market` / `plug-skills-manage` / `plug-station` / `plug-tool-resource` / `plug-unit-test` |
| 补声明 | 上列 8 个 + `plug-ai`（第 2 条，管 AppBar AskAI；原只有 `plug-ai:keys` 管 `/AIAgent`） |
| 角色口径 | 全部 `Roles=["*"]`（D-P6-4=A）⇒ **零可见性回归**，只获得"可被模块启停关掉"的能力 |
| 验收 | **18/18 断言通过**：`/api/modules/` 有功能键模块 13→**21**；侧栏基线 24；逐个关闭 24→23→…→17；`plug-ai` 关后 AppBar「智能命令」1→0；全部还原 34/34 |

**三处探针口径修正（都是测量问题，不是缺陷）**

1. **图站管理菜单不可验**：`StationManageMenu` 在 `!status.IsActivated` 时直接返回空，本机 `/api/license/status` → `isActivated=false`，菜单**本就不渲染**。故该项只验"PUT 200 + 文字不在"，键归属由"9 条新键归属正确"证明。
2. **AppBar 探针词**：`AskAI` 是组件类名，HTML 中恒 0；实际渲染的是 `Placeholder="智能命令"` 的 `MudTextField`，改数该词才是有效探针。
3. **判据 7（非管理员不回归）未实证**：三员种子账号密码已非配置默认值（`sysadmin/autadmin/audadmin` + `123456` 全返回 `InvalidCredentials`），重置会改数据。仅静态证明：`ModuleAccessStrategy.Decide` 对 `Roles.Contains("*")` 直接 `Allow`（`ModuleAccessStrategy.cs:161-164`），`"*"` 有 CJOntology `HomeUiModule` 先例。

**清理**：启停持久化文件 `module-enablement.json`（34 项全 true）已从 Release 输出目录移入 `_trash_20261030/smoke/`，避免随交付物带出陈旧状态。

**仍在册（归 B 批次）**：`plug-file-manage` / `plug-process-edit` / `plug-relation` / `plug-system-config-read` 无入口键，管理页开关对它们仍无效；142 页无 `[ModuleAccess]`、33 个 MapGroup 几乎无 `RequireModuleAccess`。



---

## 附录 U：死代码清理 + 两处缺陷收口 + 内部密钥读侧实测（2026-10-30，P13）

> 决策来源：用户拍板 **「P7.1a 确定退役 `CJ.Plug.SystemConfig`」** + 「按推荐顺序执行」。
> 本附录记录**第 1~3 项**（死代码 / 缺陷 / 内部密钥读侧）；第 4 项（B 批次覆盖面）与第 5 项（P7.1a 退役）另开。

### U.1 第一项：死代码三件套（已完成）

**① 打包发布链整链删除**（`PackageService` 1280 行 + 周边）

| 动作 | 文件 |
|---|---|
| 删 | `PlugApiServer/CJ.Plug.ApiServer/Services/PackageService.cs`、`Services/PackageProgress.cs`、`Apis/PackageApi.cs`、`PackageServiceCollectionExtensions.cs` |
| 删 | `WebHost/CJ.Plug.MainPageContent/Pages/DownloadPackage.razor`、`modules/StationManageModule/.../Dialogs/StationDownloadDialog.razor` |
| 改 | `CJPlug.Server/Program.cs`：去掉 `AddPackageModuleServices()` / `AddPackageModuleApi()` 两行与失效注释 |
| 改 | `StationStatusList.razor`：「下载图站部署包」按钮 + `OpenStationDownloadDialog()` 方法 |
| 改 | `StationSettingUI/Services/StationApiService.cs`：整段「图站部署包下载」**210 行**（3 个方法 + `PackageProgressInfo` 模型） |

**删除理由（不是"看着像死的"）**：4 个生成端点自 2026-10-07 起固定返回 410，`PackageService` 只被 DI 注册、从不解析；`/api/package/progress`、`/api/package/download/{id}` 的数据源 `PackageProgressTracker` 只由 `PackageService` 写入 ⇒ 整组端点**永远无数据**。其内部模板仍是旧 4 进程形态（`HostWebServer.exe`/`McpServer.exe` + 8687/8690）。

**② 三个孤儿**

| 对象 | 处置 | 依据 |
|---|---|---|
| `src/modules/SkillsModule/CJ.Plug.SkillsManage` | **已删（整目录）** | 不在 `.sln`、零 `ProjectReference`；与在用的 `SkillsManageModule/CJ.Plug.SkillsManage` **同名类 + 同名扩展方法**，一旦被误引用直接编译冲突 |
| `MainPageContent/Shell/PlugGuardModule.cs` | **已删** | P5 已移除注册；文件自述"P5 接入真认证后删除本模块" |
| `src/WebHost/CJ.Plug.ElsaStudio` | **暂留** | 唯一引用只剩 `assemble-release.ps1` 注释。**它是 Elsa 原生 Studio（可视化工作流设计器）的唯一宿主**，而"设计器是否还要"尚未拍板 ⇒ 不预判删除，等答复后处理 |

**③ 配置死键**（`src/Server/CJPlug.Server/appsettings.json`）

| 键 | 处置 | 依据 |
|---|---|---|
| `MainServer:Url`（6660） | 删 | 唯一消费方是 `PackageService` 模板（已删）+ 一行被注释掉的工位机代码 |
| `Guacamole`（整段，含账号密码） | 删 | 全仓 0 消费方（`RemoteDesktop:*` 才是在用那一段） |
| `ConnectionStrings:Sqlite2`（`plug.db`） | 删 | 0 消费方 |
| `ConnectionStrings:RabbitMq` | 删 | 0 消费方；本机 5672 无监听，Elsa 侧也不读 |
| `ConnectionStrings:Sqlite`（`main333.db`） | **改**为 `../../main.db` | ⚠ **它不是死键，是兜底陷阱**：CJCore `DbConnectionProviderBase.ResolveConnectionString` 的 `_ =>` 分支取 `ConnectionStrings:Sqlite` ⇒ 任何回落都会**凭空造一个 `main333.db` 副库**。删掉只会退化成默认值 `cjontology.db`（同一颗雷），所以改成指向主库 |
| `ApiBaseUrl` | **保留** | 误判修正：被 CJCore `PersonalCenterApiClientRegistration` / `MessageCenterApiClientRegistration` 读作基址，而 `MapCJCorePersonalCenter()` **已注册** ⇒ 活键 |

### U.2 第二项：两处缺陷（已完成）

**① `DeleteByFilter` 子串匹配 → 精确列匹配**

```diff
- .Where(p => p.PDZId.Contains(filter.PlugDefinitionId))
+ if (filter is null || string.IsNullOrWhiteSpace(filter.PlugDefinitionId)) return false;   // 防空条件退化成 IS NULL 全删
+ .Where(p => p.PlugDefinitionId == filter.PlugDefinitionId)
+ if (pdz.Count == 0) return true;   // 幂等：该插头本来就没有 PDZ
```

`PDZId` 的真实构成是「用户名 + 流程定义ID」⇒ 传 `G8` 会把 `adminG8zBDesi` 一并删掉；而 `PlugDataZone` 本来就有 `PlugDefinitionId` 列，`GetByFilter` 也是按它精确匹配 —— 修完两处语义一致。**触发面**：`POST /api/pdz/deleteByFilter` 对外可达，`DeletePDZByDeletePlug`（删插头连带删 PDZ）走的就是它。

**② `IsDangerous` 恒 `false` → 发布侧显式声明，全链打通**

| 环节 | 改动 |
|---|---|
| 声明位 | `MCPTool.ToolSettingsJson` 里写 `"isDangerous": true`（**不加表列**，复用既有自由 JSON 列，避开 SchemaDiffer 变更与存量库兼容） |
| 投影 | `MCPToolsManageService.GetPublishedWorkflowsAsync` → `PublishedWorkflowDto.IsDangerous`（新增 `ReadIsDangerous`，容错：键缺失/JSON 坏 ⇒ false） |
| 桥 | `PlugMcpBridgeContracts.ToolSummary` 增 `IsDangerous`；`DynamicToolRegistry.IsDangerous(name)` 供查 |
| 执行器 | `PlugMcpToolExecutor` 构造函数收 `isDangerous`，`IsDangerous` 由"恒 false"改为透传（旧注释写着"按工具名前缀粗判"但**并无实现**） |
| 同步 | `PlugMcpToolSyncService.ToolSummary` 增字段并传入 |

**实测证据**（临时插一条 `ToolSettingsJson={"isDangerous":true}` 的工具 → 重启 → 抓桥 → 删行复原）：

```json
[{"name":"smoke_danger","description":"危险工具冒烟","parametersJson":"{\"type\":\"object\",\"properties\":{}}","isDangerous":true}]
```

消费端 `DefaultToolRegistry.IsDangerous(toolName)` 被 `LLMAgentBase.ToolLoop.cs:160` 与 `ToolInvocation.cs:42` 读取 ⇒ 危险工具回到二次确认。

> **仍缺**：发布 UI 上没有"危险操作"勾选框，当前只能通过 `POST /api/mcp/addTool` 的 `ToolSettingsJson` 声明。属 B 批次的小尾巴。

### U.3 第三项：内部密钥读侧实测（读侧无需补漏；EnforceWrite 仍待写路径观测）

**方法**：以 `InternalApi__Key=obs-…` 启动（`GateMode` 保持默认 `Observe`），用浏览器（注入登录 Cookie）把 10 个业务页各渲染一遍 —— 内部 `MainApiClient` 全部走 `BaseApiClient`/`AddHttpClient` 出站，自动带 `X-Internal-Key`；随后带密钥读 `/api/diagnostics/internal-key`。

| 维度 | 带密钥 | 缺密钥 | 覆盖率 |
|---|---|---|---|
| **读** | **156** | **0** | **100.00%** |
| 写 | 0 | 0 | n/a（10 个页面全为只读渲染，本轮无写请求样本） |

**结论**：P5.8 遗留的"读侧补漏"**实测为零漏网**，无需再补代码。`missingRead` / `missingWrite` 均为空。

**为什么仍不能开 `EnforceWrite`**：它拦的是**写**方法，而写侧样本数为 0 —— 未测过就不能宣称"100%"。开之前必须先跑一轮**真实写路径**（发布工作流 `addTool`、保存插头、保存 LLM 配置、工位机心跳上报），确认 `write.withoutKey == 0`。
**另**：密钥是部署期机密，`appsettings.json` 里的 `Key` 必须保持空（生产用 `InternalApi__Key` 环境变量或部署机配置注入），所以"收口"这一步**本质是部署动作**，代码侧本轮的产出是"读侧已证明无需补漏"。

### U.4 本轮附带发现（未处置，登记）

| # | 发现 | 影响 |
|---|---|---|
| 1 | **`/health` 根本不存在** | `app.MapDefaultEndpoints()`（Aspire）只在 `IsDevelopment()` 下映射 `/health`、`/alive`；而 Development 环境本就起不来（K.5 第 5 项）。交付态（Production）**任何健康探针都会落到 Blazor catch-all，拿到 200 HTML** ⇒ 编排/监控会误判"健康"。⚠ 本仓此前多次把"/health 200"当就绪判据，实为 catch-all，判据不严 |
| 2 | `McpServer/Tools/JobsTool.cs:19` 打 `http://localhost:6661/api/Job/getJobs` | 6661 是已退役端口，该工具**必然失败**（静默） |
| 3 | `ElsaIntegration/Services/ElsaEngineService.cs:36` 打 `http://localhost:5001/elsa/api` | 5001 是旧 Elsa 端口，同上 |
| 4 | 四个 csproj 里的 `<SkipBuild>true</SkipBuild>` | **死属性**：全仓无任何 targets/props 消费它（`grep SkipBuild` 只命中定义处）⇒ 不产生任何效果，但会让读者误以为项目被跳过。建议连同注释一起删 |
| 5 | 工位机 `src/PlugStation/CJ.Plug.StationApiServer/appsettings.json` 仍带 `Guacamole` / `MainServer` / `Sqlite2` / `RabbitMq` 同款死键 | 本轮**未动**：工位机是独立交付形态（7660），需单独起服验证后清 |

---

## 附录 V：P7.1a 退役 CJ.Plug.SystemConfig + ElsaStudio 退役（2026-10-30，已完成）

> 决策来源：用户三连拍板 —— **D-P7.1a-1 = B**（不迁移直接删，接受 `window → fullscreen` 回退）、**可视化工作流设计器不要了**（ElsaStudio 一并退役）、**B 批次缓做**。
> 详细方案、实际落地清单（10 项）、验收实测（读写删闭环）全部见 **`（已完成）P7.1a退役CJ.Plug.SystemConfig方案-20261030.md` §8~§10**，此处只留结论与索引。

### V.1 结论

| 项 | 结果 |
|---|---|
| `CJ.Plug.SystemConfig` 四项目（页面/Api/ApiClient/Model）+ `SystemConfigItems` 表 | **已退役**。表由 P4 迁移引擎启动时自动改名归档为 `_dropped_SystemConfigItems_20261030061040`（数据可回溯，零升级脚本） |
| 读侧（`ToolExecuteService` 读 `RemoteViewMode`） | 改走新抽象 `IRemoteViewModeProvider`（PlugBaseCore 纯加法）：主宿主注入 `CJCoreRemoteViewModeProvider` 读 CJCore 配置快照；未注册宿主按 `fullscreen` 兜底 |
| 写侧（`/system-config` 持久化） | `PlugSystemConfigStore` 重写为 CJCore Data 底座直读写，**复用 `TRA_SysConfigs`**（`Category="Plug"` 三边界隔离，零迁移；不换底座页面会退化为内存 Store 重启丢配置——方案 §4 未预估到的关键连锁） |
| `CJ.Plug.ElsaStudio`（Elsa 原生设计器宿主） | **已删除**（sln 外 0 引用孤儿，`RegisterCustomElsaStudioElements` 来自 ElsaIntegration 不受影响） |
| 验收 | 构建 0 错误；配置 PUT→GET→save(删) 闭环全通且 `TRA_SysConfigs` 的 TRA_ 行零污染；`/system-config` 200 + 「远程查看」页签在；`main_audit.db` 冒烟后复原（0 行差异） |
| 交付说明 | 现场"远程查看"将回退为整桌面 VNC；要单窗口去 `/system-config → 远程查看` 改回 `window` |

### V.2 B 批次（缓做，用户拍板）

4 模块入口键（`plug-job-manage` 等）+ 142 页 `[ModuleAccess]` + 33 个 MapGroup `RequireModuleAccess` —— **暂不执行**，待后续批次启动。

> **⚠ 2026-10-30 口径更正（见附录 W）**：①模块入口键的"4 个"应为 **3 个** —— `plug-system-config-read` 随本批 SystemConfig 退役一并消失；
> ②"142 页"的**统计口径有误** —— 实测 152 处 `@page` 中 **120 处是 Amis 官方示例页**，已在 P7.1b 构建期排除，
> 真实业务页 31 个、其中 11 个已有 `[ModuleAccess]` ⇒ **页面缺口实际为 ~14 页**，不是 142 页。

### V.3 本批新登记的附带发现

| # | 发现 | 处置 |
|---|---|---|
| 1 | `/api/systemconfig/*` 旧端点已随模块消失，但 curl 仍得 **200 + text/html**（catch-all 吞掉） | 无需处置；判活必须断言正文，与 U.4-1 `/health` 假 200 同源 |
| 2 | `MainApiClient.cs` 原有 `SystemConfigApiClient` 连续两行重复赋值（无害死代码） | 随本批删除一并消失 |

---

## 附录 W：P7.1b Amis 示例页构建期排除（2026-10-30，已完成）

> 决策来源：用户拍板 **「Amis 120 页的处置方向定下来：在构建期排除」**。
> 完整取证 / 实现 / 验收见 **`（已完成）P7.1b排除Amis示例页方案-20261030.md`**，此处只留结论与口径更正。

### W.1 结论

| 项 | 结果 |
|---|---|
| 问题 | `CJ.Plug.AmisIntegration/Components/Pages/` 下 **120 个 Amis 官方示例页**（`/CRUD/*`、`/Form/*`、`/Event/*` …）随程序集被 `PlugBaseCore` 引用，**全部自动注册进唯一宿主路由且零门控** ⇒ 交付物自带 120 个匿名演示页 |
| 处置 | csproj 加 1 个 `ItemGroup`：`Content Remove` + `RazorComponent Remove` 排除 `Components/Pages/**`。**源码留仓库**（Amis 用法参照），`-p:IncludeAmisDemoPages=true` 可临时编回 |
| 为什么不是加 `[ModuleAccess]` | 只是上锁，120 页体积照旧出厂，且会给 B-2 添维护面 |
| 为什么不是删源码 | 改 `Amis.razor` 渲染 / 排查 Amis 行为时需查用法；排除可逆 |
| 功能影响 | **零**。业务只消费 `Components/Amis.razor`；`amisLoad` 定义在 `PlugMainPageContent/Shell/PlugHeadModule.cs:69`，均不在被排除目录 |
| 验收 | 构建 0 错误；dll 26,112 B、`Components.Pages` 字符串 0 命中而 `Components.Amis` 1 命中；19 条抽样 demo 路由全部落 catch-all（200 +「404 - 页面未找到」）；8 个业务页 200；6 个 Amis 静态资源 200；三页死链 0；`main_audit.db` 复原 |

### W.2 口径更正（本章节起以此为准）

| 原口径 | 实测更正 | 来源 |
|---|---|---|
| "153 / 142 页无 `[ModuleAccess]`" | `@page` 共 **152 处 / 151 文件**；其中 **120 文件是 Amis 示例页**（P7.1b 已排除）；**真实业务页 31 个**，11 个已覆盖，6 个不该门控 ⇒ **页面缺口 ≈ 14 页** | `@page` 全量扫描 + 逐页核 `ModuleAccess` |
| "4 个模块无入口键" | **3 个**：`plug-file-manage` / `plug-process-edit` / `plug-relation`；`plug-system-config-read` 随 P7.1a 退役消失 | `grep "plug-system-config-read"` 0 命中 |
| "33 个 MapGroup" | **34 个**，`RequireModuleAccess` 实调用 **1 处**（`/api/plug-agent/tools`，另 1 处是注释） | `grep "MapGroup("` / `grep "RequireModuleAccess"` |

### W.3 遗留

| # | 项 | 说明 |
|---|---|---|
| 1 | `Components/Layout/NavMenu.razor` | 演示页专用导航菜单，Pages 排除后成**死代码**（全仓 0 引用），可后续一并排除 |
| 2 | `wwwroot/test.html`、`wwwroot/photo/`、`img/` | Amis 演示资源，随 `wwwroot/**` 进交付物，属"出厂垃圾"，建议单独清理 |
| 3 | B-2 可直接按 **14 页**排期 | 与 D9=A 全量权限矩阵合并推进成本更低 |
