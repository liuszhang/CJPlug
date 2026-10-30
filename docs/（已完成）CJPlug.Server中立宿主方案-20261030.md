# CJPlug.Server 中立宿主方案（已完成）

> 状态：**已完成（2026-10-30 收口）** ｜ 立项：P9 ｜ 提出：2026-10-30（用户提议）｜ 取证：2026-10-30
> 收口判据：D1=A 已落地（新建 `src/Server/CJPlug.Server`，ApiServer 降级类库），全解构建 0 错误、五面 + Elsa 链实测全绿、数据零残留（见 §9）。
> 前置：P8 供给侧已收口（附录 O），D2/D7 已完成，当前是"1 进程 5066"的合并形态。
> 纪律：本文件只定 **D1 一个决策点**；D2~D5 仅登记为待议，不展开实施方案。

---

## 0. 结论先行

**这件事的本质不是"合并"（合并已经做完了），而是"正名 + 装配归一"。**

当前状态：进程数已经是 1、端口已经是 1（5066），`CJ.Plug.ApiServer` **事实上就是唯一宿主** —— 但它：

1. 名字叫 ApiServer，却承载了全站（Blazor UI / 调度 Hub / Elsa 引擎 / MCP 协议面）；
2. 装配入口分裂在**两个项目的两个文件**里（`ApiServer/Program.cs` 553 行 + `HostWebStartup.cs` 552 行），分工只靠注释约定；
3. 部署链（AppHost 清单 / 打包断言 / 启动脚本 / 安装器）**全部停在融合前的旧形态** —— 今天上午的整串事故（4 进程双启 ⇒ 模型 29 vs 80 表 ⇒ 迁移判"整表回退"）就是这么造成的。

因此 P9 的真实收益是：**让"谁负责装配"只有一个答案，让部署链与架构自动对齐**。代价主要在 6 个耦合点的同步改造，不在写代码。

---

## 1. 现状取证

### 1.1 事实基线

| 事实 | 证据 |
|---|---|
| 唯一宿主 = `CJ.Plug.ApiServer`，端口契约写死 5066 | `ApiServer/Program.cs:87 const string ContractUrls = "http://*:5066"` |
| 它通过 3 个扩展方法吞下整个 Web 侧 | `Program.cs:286 builder.AddPlugWebHost()`、`:465 app.MapPlugWebHost()`、`MapPlugWebRazorComponents()` |
| 它已吞下调度面与 MCP 面 | `Program.cs:475 MapDispatchServiceEndpoints()`、`:485 MapPlugMcpBridge()`、`:486 MapMcp("/mcp")` |
| 它已吞下 Elsa 引擎 | `Program.cs:275 AddElsaServicesForApi()`、`:479 UseElsaEndpoints()` |
| 它引用 DispatchServer / McpServer / HostWebServer 三个项目 | `CJ.Plug.ApiServer.csproj` 的 7 条 ProjectReference 中 3 条 |
| `HostWebServer/Program.cs` 只是**回退薄壳** | `Program.cs:9-12` 原文："合并后…不再被 AppHost 启动，但保留下来 = **回退路径**" |
| Web 侧全部注册与管道在 `WebHostStartup.cs`（552 行） | `AddPlugWebHost:108` / `MapPlugWebHost:444` / `MapPlugWebRazorComponents:543` |

### 1.2 当前装配拓扑

```
                    ┌──────────────────────────────────────────────┐
                    │  CJ.Plug.ApiServer.exe  (唯一宿主, :5066)      │
                    │  Program.cs 553 行 = 装配编排 + 管道顺序        │
                    └──────────────────────────────────────────────┘
                        │ 调扩展方法                        │ 自己持有
        ┌───────────────┼───────────────┬───────────────┐    │
        ▼               ▼               ▼               ▼    ▼
   HostWebServer   DispatchServer   McpServer   ElsaIntegration  ApiServer 自有
   (552+90 行)     (Hub + /api/     (MCP 协议)  (引擎 + 客户端)   PackageApi /
   3 个扩展方法      dispatch/*)                                  PackageService /
                                                                 Hubs / Infra
                                                                 ≈1780 行真业务
```

**关键结构事实**：`CJ.Plug.ApiServer` 项目 2333 行 / 8 个文件，其中 `Program.cs` 553 行是纯装配，其余 **≈1780 行是业务**（`Apis/PackageApi.cs`、`Services/PackageService.cs`、`Hubs/SignalRMcpToolChangeNotifier.cs`、`Infrastructure/`）。也就是说"宿主"与"打包发布业务模块"目前共用一个 csproj。

### 1.3 六个耦合点（改 dll 名 / 项目结构必须同步的清单）

| # | 位置 | 当前内容 | 性质 |
|---|---|---|---|
| 1 | `CJCore/src/Apps/CJCore.AspireHost.AppHost/AppHost.cs:233` | 内置默认清单 `Dll = "CJ.Plug.ApiServer.dll"`、`Port = 5066` | 代码 |
| 2 | `02.Publish/CJ.Plug.AspireHost.AppHost/*/appsettings.json` 与 `appsettings.CJ.Plug.AspireHost.AppHost.json` | `Resources[0].Dll = "CJ.Plug.ApiServer.dll"` | **配置（现场可改，最容易被忽略）** |
| 3 | `05.Build/assemble-release.ps1:266-268` | 断言 `Services\$Configuration\net10.0\CJ.Plug.ApiServer.dll` + McpServer + DispatchServer 三个 dll 必须存在 | 打包护栏 |
| 4 | `src/Framework/CJ.Plug.Setup/CJ.Plug.Setup.csproj:17` + `Program.cs:24-25` | ProjectReference + 硬编码 publish 路径 `src/PlugApiServer/CJ.Plug.ApiServer` | 安装器 |
| 5 | `02.Publish/start-services.cmd` | 起 **4 个**进程（8686/8687/3001/5066），注释明写 `manual, no Aspire` | 部署脚本（旧形态） |
| 6 | `02.Publish/寸金 - 快捷方式.lnk`、`05.Release/main/**` | 已有产物目录与快捷方式按旧 exe 布局 | 现场产物 |

> ⚠ **隐藏雷（#2）**：AppHost 的 `Resources[]` 会从配置文件读，**配置优先于内置默认清单**。现场如果改过 `appsettings.json`（或管理壳写过 `apphost.user.json`），仅改代码里的 `AppHost.cs` **不会生效**，服务仍按旧 dll 名拉起 ⇒ 表现是"Aspire 启动了但服务不在"。这是 A 方案最容易翻车的地方。

### 1.4 三类问题归因

| 问题 | 现状 | 后果（已实际发生） |
|---|---|---|
| **名实不符** | exe 叫 `ApiServer`，实为全站宿主 | 部署脚本 / 运维文档 / 新人认知全部按"前后端分离"理解 |
| **装配双头** | `ApiServer/Program.cs` + `WebHostStartup.cs` 各自负责一段，边界只写在注释里 | 2026-10-30 上午：补丁放错位置（放共享方法 ⇒ 重复注册；放错档位 ⇒ 17677 条 ESTABLISHED） |
| **部署链漂移** | 代码已 1 进程，脚本/清单/断言仍 4 进程 | 双宿主同时拉起 ⇒ 模型集 29 vs 80 ⇒ 迁移判整表回退 ⇒ 5066 起不来 |

---

## 2. 形态候选

### A. 中立宿主 `CJPlug.Server`（用户提议，搬迁式）

新建可执行项目 `CJPlug.Server`（`Microsoft.NET.Sdk.Web`），把 `ApiServer/Program.cs` 的装配编排整体搬进去；`CJ.Plug.ApiServer` **降级为类库**（去 exe，只留 PackageApi/PackageService/Hubs/Infrastructure + 两个扩展方法）。

```
CJPlug.Server.exe (:5066)   ← 唯一宿主，Program.cs 只做"装配编排 + 管道顺序 + 端口契约"
  ├─ CJ.Plug.ApiServer      (类库)  AddCJPlugPackagingServices / MapCJPlugPackagingEndpoints
  ├─ CJ.Plug.HostWebServer  (类库)  AddPlugWebHost / MapPlugWebHost / MapPlugWebRazorComponents
  ├─ CJ.Plug.DispatchServer (类库)  AddDispatchApiService / MapDispatchServiceEndpoints
  ├─ CJ.Plug.McpServer      (类库)  MapPlugMcpBridge
  └─ CJ.Plug.ElsaIntegration(类库)  AddElsaServicesForApi / UseElsaEndpoints
```

- ✅ 名实相符；装配入口唯一（新 Program.cs 就是"编排表"）
- ✅ 原来的 ApiServer 项目语义归位（只负责打包发布业务）
- ⚠ 6 个耦合点全要改；现场配置 #2 必须给迁移脚本
- ⚠ 旧 exe 消失 ⇒ 现场升级若只覆盖文件，会留下孤儿 `CJ.Plug.ApiServer.dll`（需清理断言）

### B. 装配归一（不新建项目，只提炼编排类）

`CJ.Plug.ApiServer` 保留为 exe，把 `Program.cs` 的 553 行提炼成 `CJPlugServerAssembly.cs`（静态装配类，`AddCJPlugServer(host)` / `UseCJPlugServer(app)`），`Program.cs` 收敛到 ~25 行；同时修 `start-services.cmd`。

- ✅ 解决"装配双头"这个真痛点；装配入口物理唯一
- ✅ **零 dll 名变更** ⇒ 6 个耦合点全不用动，现场零迁移
- ❌ exe 名仍叫 ApiServer（语义问题只解决一半，靠文档/标题补）

### C. 只修部署脚本（最小动作）

`start-services.cmd` 改为只起 `CJ.Plug.ApiServer`；`AppHost.cs` 注释同步。

- ✅ 立刻消除"双启造回退形态"这个事故源（成本 ~10 分钟）
- ❌ 装配双头仍在；名实不符仍在

### 对比

| 维度 | A 搬迁式中立宿主 | B 装配归一 | C 只修脚本 |
|---|---|---|---|
| 名实相符 | ✅ 完全 | ⚠ 一半（exe 名不变） | ❌ |
| 装配入口唯一 | ✅ | ✅ | ❌ |
| 部署链改造量 | **6 个耦合点 + 现场迁移** | 0（只改脚本） | 1 |
| 现场升级风险 | 中（孤儿 dll / 配置漂移） | 极低 | 无 |
| 可回退性 | 需保留旧 exe 一个版本 | 天然可回退 | 天然 |
| 预估改动 | 新建 1 项目 + 改 5 文件 + 打包链 + 现场脚本 | 新增 1 类 + 改 2 文件 + 脚本 | 改 1 文件 |
| 对其它产品影响 | AppHost 在 CJCore 仓（CJWork/CJOntology 不引用 CJ.Plug.ApiServer，仅 Setup 引用） | 无 | 无 |

---

## 3. D1 决策点（本文件唯一待拍板项）

**P9 的形态选哪一个？**

| 选项 | 含义 | 关键代价 |
|---|---|---|
| **A** | 新建 `CJPlug.Server` 为唯一宿主，`CJ.Plug.ApiServer` 降级为类库 | 6 个耦合点 + 现场配置迁移；需一次完整回归 |
| **B** | 不新建项目，只把装配提炼成单一编排类 + 修脚本 | exe 名仍叫 ApiServer |
| **C** | 只修部署脚本，装配问题另议 | 装配双头保留 |
| **D** | A 分两阶段：先 B（本次），下个版本再做 A 的改名与迁移 | 两轮工作量，但每轮风险最低 |

> 推荐 **D**：B 的收益（装配入口唯一）占 A 的 80%，成本不到 A 的 20%，且不动现场产物；等现场有一次正常升级窗口时再做 A 的改名。
> 若你更看重"立刻正名"（避免运维继续按前后端分离理解），则选 **A**，我会先出**现场迁移脚本**再动代码。

---

## 4. 待议（D1 定稿后逐枝展开，本文件不实施）

| 决策点 | 问题 | 预置选项 |
|---|---|---|
| **D2 回退能力** | `HostWebServer` 独立形态（5066 单独起）留不留？ | A 留（但明确"仅排障用"，且禁止与主宿主同时跑同一 main.db）；B 删（回退靠上一版本产物）；C 留但加互斥锁（检测端口/文件锁后拒绝启动） |
| **D3 旧名兼容** | 现场已有快捷方式/文档/脚本引用 `CJ.Plug.ApiServer.exe` | A 保留同名 shim exe 一个版本；B 直接换名 + 迁移脚本；C 只改内部，对外仍叫 ApiServer |
| **D4 部署链改造范围** | AppHost 清单（代码 + 配置）、打包断言、Setup、start-services.cmd、.lnk | A 全改；B 只改代码侧，配置交给现场；C 引入"清单版本号"做自动迁移 |
| **D5 验收口径** | 怎样算 P9 完成 | 建议：① 全量构建 0 错误 ② 单进程 5066 起得来且 MASS-ARCHIVE 0 次 ③ 首页/登录/getTools/Elsa 保存/plugbridge 调用五面绿 ④ 打包产物目录结构断言通过 ⑤ 现场模拟升级（旧产物 + 新产物覆盖）不出现孤儿进程 |

---

## 5. 若选 A：实施步骤（草案，D1 拍板后细化）

| # | 步骤 | 产出 |
|---|---|---|
| 1 | 新建 `src/Server/CJPlug.Server/CJPlug.Server.csproj`（Sdk.Web，`BaseOutputPath=02.Publish\Services`，端口契约同源） | 项目骨架 |
| 2 | 把 `ApiServer/Program.cs` 的装配段整体迁入新 `Program.cs`（**逐段搬，不重写**） | 新宿主入口 |
| 3 | `CJ.Plug.ApiServer` 改 `Sdk`（去 Web/exe），新增 `AddCJPlugPackagingServices()` / `MapCJPlugPackagingEndpoints()` | 类库化 |
| 4 | `CJ.Plug.Setup` 改引用新项目 + 改 publish 路径 | 安装器 |
| 5 | `AppHost.cs` 默认清单改 `Dll = "CJPlug.Server.dll"` | 编排器 |
| 6 | 两份 AppHost `appsettings*.json` 改 Dll（**并加"旧名自动纠正"兜底**） | 现场迁移 |
| 7 | `assemble-release.ps1` 断言改新 dll（保留对旧 dll 的"孤儿检测"） | 打包护栏 |
| 8 | `start-services.cmd` 改为只起 1 个进程 | 部署脚本 |
| 9 | 全量构建 + 单进程启动 + 五面回归 | 验收 |
| 10 | 文档：实施计划加附录 P；旧方案文档标注"已被 P9 取代" | 文档 |

---

## 6. 风险清单

| 风险 | 触发条件 | 缓解 |
|---|---|---|
| 现场配置未迁移，Aspire 起不来服务 | 现场改过 `appsettings.json` / 有 `apphost.user.json` | 代码里做"旧 dll 名 ⇒ 新 dll 名"自动纠正 + 启动日志显式提示 |
| 升级后孤儿进程 | 旧 `CJ.Plug.ApiServer.exe` 仍在运行 / 目录残留 | 打包断言 + 启动脚本先 `taskkill` 旧名 |
| 双宿主再次同跑同一 main.db | 回退形态被误启 | D2 决策：加互斥（端口/文件锁） |
| 搬迁 Program.cs 时丢注册 | 553 行里有注释掉的"死码"与顺序敏感段 | 逐段搬 + 对照清单勾选；构建后跑五面回归 |
| 影响 CJCore 仓的 AppHost | AppHost 源码在 CJCore 仓 | 改动走 CJCore 仓的提交，与 CJPlug 侧同版发布 |

---

## 7. 与既有结论的关系

- 本方案取代 `（已评审）CJPlug单端口收敛与双宿主合并方案-20261029.md` 的"宿主归属"部分；该文档的"1 进程 1 端口"结论**仍然有效**，不推翻。
- `02.Publish/start-services.cmd` 的修正（附录 N.6）在本方案里升格为 D4 的一部分，不再单独处理。
- `IAuthSchemeOverrideSelector`（附录 O）不受影响：它注册在 `AddElsaServicesForApi` 内部，随项目搬迁自动跟随。

---

## 8. D1 决策记录（2026-10-30）

**决策：A —— 直接新建 `CJPlug.Server`，`CJ.Plug.ApiServer` 降级为类库。**

理由（用户拍板时未提异议，按方案对比表执行）：接受 6 个耦合点的同步改造，换"名实完全相符 + 装配入口物理唯一"。

**同时新增一条决策（用户在本轮中途提出）：`CJ.Plug.Setup` 安装器直接废弃，交付统一走 CJSuite 打包与安装器。**
经取证：它**不在 sln 里、不在打包脚本里（`assemble-release.ps1` 明写"不收 CJ.Plug.Setup"）、无任何项目引用它** —— 事实上早已弃用，只有目录残留。已物理删除 `src/Framework/CJ.Plug.Setup`（2 个文件）。

---

## 9. 实施记录（2026-10-30，已完成）

### 9.1 改动清单

| # | 文件 | 改动 |
|---|---|---|
| 1 | `src/Server/CJPlug.Server/CJPlug.Server.csproj`（新增） | `Microsoft.NET.Sdk.Web`、`AssemblyName=CJPlug.Server`、`BaseOutputPath=02.Publish\Services`、10 条 PackageReference、9 条 ProjectReference |
| 2 | `src/Server/CJPlug.Server/Program.cs`（新增） | **从 `CJ.Plug.ApiServer/Program.cs` 原样搬迁**（用 `cp` 保留 BOM 与编码，逐字未改逻辑），仅重写文件头注释为"唯一宿主"定位 |
| 3 | `src/PlugApiServer/CJ.Plug.ApiServer/Program.cs` | **删除**（已迁走） |
| 4 | `src/PlugApiServer/CJ.Plug.ApiServer/CJ.Plug.ApiServer.csproj` | 加 `<OutputType>Library</OutputType>` + 注释（保留 Sdk.Web：仍直接用 ASP.NET 类型，换 SDK 收益为零风险不为零） |
| 5 | `src/PlugApiServer/CJ.Plug.ApiServer/Infrastructure/PlugMcpToolRefreshService.cs` | `internal sealed class` → `public sealed class`（跨程序集注册 `AddHostedService<>` 需要） |
| 6 | `CJ.Plug-Aspire.sln` | 加 `CJPlug.Server`；`dotnet sln add` 顺带把 `CJCore.AspireHost.ServiceDefaults`（跨仓项目）塞进了 sln，**已移除**，保持跨仓隔离 |
| 7 | `CJCore/src/Apps/CJCore.AspireHost.AppHost/AppHost.cs` | 默认清单 `Dll = "CJPlug.Server.dll"` + 显式 `ConfigFile`；新增**旧名自动纠正**（见 9.2） |
| 8 | `CJCore/.../AppHost/appsettings.json`（出厂配置） | `Resources[0]`：`Dll` 改新名 + 加 `ConfigFile` |
| 9 | `02.Publish/**/appsettings*.json`（4 份产物配置） | 同步改 `Dll` + 补 `ConfigFile` |
| 10 | `05.Build/assemble-release.ps1` | 断言 `CJ.Plug.ApiServer.dll` → `CJPlug.Server.dll`，并注明"断言必须落在可执行宿主上，否则恒为真、失去护栏意义" |
| 11 | `02.Publish/start-services.cmd` | 4 进程 → **1 进程**（`CJPlug.Server.dll`），保留原文件为 `start-services.cmd.bak4proc`；前置检查新 dll 是否存在 |
| 12 | `src/Framework/CJ.Plug.Setup/**` | **物理删除**（用户决策：安装器统一走 CJSuite） |

### 9.2 两个"沉默失败"的收口（本方案最关键的两处非机械改动）

| 风险 | 表现 | 处理 |
|---|---|---|
| **配置文件推导漂移** | `ServiceRuntime` 按 `Dll` 名推导 `appsettings.{项目名}.json` ⇒ 改名为 `CJPlug.Server.dll` 后会去找 `appsettings.CJPlug.Server.json`（**不存在**）⇒ 静默退回根 `appsettings.json`。而部署配置（`PlugDb=../../main.db`、`InternalApi`、三员种子密码）全在 `appsettings.CJ.Plug.ApiServer.json` 里 ⇒ **连错库且无报错** | 默认清单显式 `ConfigFile = "appsettings.CJ.Plug.ApiServer.json"`（沿用历史名，现场零迁移）；并注释说明"改名留作后续独立迁移" |
| **现场配置优先于代码默认清单** | `Resources[]` 来自 `appsettings.json` / `apphost.user.json`；现场没手改就会继续按旧 dll 名拉起 ⇒ 新包里旧 dll 已不存在 ⇒ 100% 起不来，且 Dashboard 只显示"启动失败" | `AppHost.cs` 增加**单向**旧名纠正（`CJ.Plug.ApiServer.dll` → `CJPlug.Server.dll`）并打印告警；`ConfigFile` 为空时同步兜底为历史名 |

### 9.3 实测（Debug，单进程）

| 项 | 结果 |
|---|---|
| 构建 `CJPlug.Server` | **0 错误**（1 条既有警告） |
| **全解构建**（`dotnet build CJ.Plug-Aspire.sln`） | **0 错误**（139 条既有警告），证明 ApiServer 类库化 + Setup 删除未破坏任何项目 |
| 启动 | `Now listening on http://[::]:5066`、`Application started`、`MASS-ARCHIVE 拦截 0 次` |
| `Module Db EntityConfig` 加载行数 | **42 行** = 21 模块 × 迁移引擎两次建模（与附录 O 的合并形态同值，证明模型集未变） |
| `/`（Blazor 首页） | 200 |
| `/three-role/login` | 200 |
| `/api/mcp/getTools` | 200 |
| `/api/plugbridge/tools` | 200 |
| `/api/pdz/getAllPDZ` | 200 |
| `POST /elsa/api/identity/login` | 200（D7=A 的效果在新宿主上保持） |
| `GET /elsa/api/...`（无凭据） | 302 → `/three-role/login`（未命中 selector 的路径行为不变） |
| **Elsa 保存 + 执行链** | `addTool`(Id=4) → `plugbridge/call` **200 / 1.13s / 实例 ID `a86ac1f5cdff5205`** → `main-elsa.db` 出现 `G8zB` → `deleteTool/4` 200 → `getTools=[]` |
| 异常/拦截行数 | 0 |

> 结论：新宿主与搬迁前行为**完全等价**（同一份 Program.cs），五面 + Elsa 链全绿。

### 9.4 数据零残留

清理后与 `_bak_20261030_P8/main.db` 逐表 hash 全等（Plugs 1 / MCPTools 0 / PDZ 2 / PlugDatas 1 / Jobs 0 / PVD 0）；`main-elsa.db` 回到 6 条定义。

### 9.5 遗留

| 项 | 说明 |
|---|---|
| 部署配置文件名 | 仍叫 `appsettings.CJ.Plug.ApiServer.json`（有意沿用）。后续要改名，需同时改 DefaultResources + 现场文件名，属独立迁移 |
| `02.Publish/CJ.Plug.AspireHost.AppHost`（含旧 Release） | 是**融合前**的多进程产物（`ResourceStrings` 旧格式）⇒ **不可用于部署**。2026-10-30 已整目录移入 `CJPlug/_trash_20261030/` |
| `02.Publish/CJ.Plug.Setup` | 历史构建产物目录（1.0 GB），随安装器废弃一并移入 `CJPlug/_trash_20261030/` |
| `寸金 - 快捷方式.lnk` | 二进制快捷方式，可能指向旧 exe，需人工重建 |
| `CJ.Plug.DispatchServer` / `CJ.Plug.McpServer` / `CJ.Plug.HostWebServer` / `CJ.Plug.ElsaApiServer` / `CJ.Plug.ElsaStudio` 的独立 exe | ✅ **已全部下线（2026-10-30，D2 = B，见实施计划附录 Q.8）**：前三个类库化（源码保留、删启动壳），后两个移出 sln；包体 Service 面只剩 `CJPlug.Server.exe` |
| 05.Release/main | ✅ 2026-10-30 已实跑 `assemble-release.ps1 -Target main` 重建（0 错误、断言通过、清单 2 服务），见实施计划附录 Q.5 |
