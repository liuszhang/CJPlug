# CJPlug Aspire 编排壳迁移 CJCore 方案

> 状态：已完成（步骤 1~4 全部落地，见附录 A / B）
> 日期：2026-10-29
> 关联：`（已评审）CJPlug服务架构梳理与简化方案-20261029.md`、`（已评审）CJPlug单端口收敛与双宿主合并方案-20261029.md`

---

## 0. 结论先行

**可行，且不存在循环依赖。** 但迁移的前提不是"搬项目"，而是先把两处"相对路径推导"改成"显式声明"。

| 问题 | 结论 |
|---|---|
| 会不会形成循环依赖 | **不会**。MSBuild 依赖图的节点是 csproj，不是仓库 |
| AppHost 运行时有项目依赖吗 | **没有**。`AddExecutable` 是纯路径拼接，实测输出目录零 `CJ.Plug.*` 程序集 |
| 那 6 个 `ProjectReference` 是什么 | 纯**构建顺序触发器**，其中 2 个在代码里完全没用到 |
| 能搬吗 | **能**。删掉 6 个引用后 AppHost 即为产品中立的路径启动器 |
| 搬了就能"一并管理 CJOntology"吗 | **不能**。服务目录仍靠 `BaseDirectory/../../..` 推导，跨产品必错 |
| 真正的技术前提 | 把 `servicesDir` 从"推导"改为"声明"，把服务清单从代码改为配置 |

**建议分 4 步，每步可独立验收、独立回退。** 第 1、2 步与"搬不搬"无关，但必须先做——否则搬完仍是单产品壳。

---

## 1. 需求与目标

### 1.1 需求来源

CJPlug 主服务端在 2026-10-29 两轮简化后，进程数 6 → 1（单端口 5066）。此时 AppHost 实际只编排 **2 个资源**（`apiservice` + `stationapi`，外加一个默认关闭的 `mcpInspector`），却仍是一个产品私有项目。

目标：把 Aspire 的**服务包**（ServiceDefaults）与**编排壳**（AppHost）上移到 CJCore 框架层，为后续用一个 Aspire 实例统一编排 CJPlug / CJOntology 等产品做准备。

### 1.2 验收标准

| # | 判据 |
|---|---|
| V1 | `CJCore/src` 下存在 Aspire 两个项目，`dotnet build CJCore.sln` 通过且**不要求 CJPlug 源码在场** |
| V2 | CJPlug 全部 7 个服务继续用相对路径引用 ServiceDefaults，构建通过 |
| V3 | Desktop 能从发布根定位并拉起 AppHost，Dashboard 就绪，服务全部启动 |
| V4 | `assemble-release.ps1` 产出的 `main/` 目录结构与现在一致（路径可改，内容不可缺） |
| V5 | AppHost 的 `servicesDir` 可通过配置指定，不再只能靠目录上溯 |

---

## 2. 现状盘点

### 2.1 两个项目的构成

| 项目 | 位置 | 行数 | 产品耦合 |
|---|---|---|---|
| `CJ.Plug.AspireHost.ServiceDefaults` | `CJPlug/src/Framework/` | 180 | **零**。只引用 Aspire 包；命名空间为微软模板的 `Microsoft.Extensions.Hosting` |
| `CJ.Plug.AspireHost.AppHost` | `CJPlug/src/Framework/` | 308 | csproj 有 6 个 `ProjectReference`；代码里零 `using CJ` |

> 另有一个空目录 `CJPlug/src/Framework/CJ.Plug-Aspire.ServiceDefaults/`（无 csproj，历史遗留）。

### 2.2 依赖关系的三层分离（核心事实）

这是本方案的关键判断依据：**"依赖"必须分三层看，只有第三层是真的。**

| 层 | 是否存在项目依赖 | 依据 |
|---|---|---|
| **运行时** | 否 | `AddExecutable("stationapi", "dotnet", servicesDir, dllName)` 只拼出 `dotnet <abs>/xxx.dll`，进程启动不读 csproj |
| **输出目录** | 否 | 实测 AppHost 输出 `230MB`，构成为 `aspire/` 186MB（自带 DCP + Dashboard）+ `Aspire.Hosting` 自身依赖（`KubernetesClient` 11M、`Hex1b` 7.1M…），**无一个 `CJ.Plug.*` 程序集** |
| **构建顺序** | 是 | 6 个 `ProjectReference` 唯一作用：build AppHost 时顺带把服务构建到共享目录 |

### 2.3 六个 ProjectReference 逐个定性

`AppHost.csproj:19-25`

| 引用 | 声明方式 | 代码里用到了吗 | 真实用途 |
|---|---|---|---|
| `CJ.Plug.Models` | 默认引用 | **否**（全文零 `using CJ`，`GlobalData` 零出现） | 历史遗留 |
| `CJ.Plug.ApiServer` | 默认引用 | **否**（`Projects.CJ_Plug_ApiServer` 仅出现在第 225、226 行注释中） | 历史遗留 |
| `CJ.Plug.McpServer` | `ReferenceOutputAssembly="false"` | 否 | 构建顺序 |
| `CJ.Plug.ElsaApiServer` | 同上 | 否 | 构建顺序 |
| `CJ.Plug.StationAgent` | 同上 | 否 | 构建顺序 |
| `CJ.Plug.StationApiServer` | 同上 | 否 | 构建顺序 |

**"构建顺序"这件事已有替代保证**，脚本自己写明了：

```
assemble-release.ps1:193-194
「main：必须整解构建。AppHost 用 AddExecutable 拉起服务，而 DispatchServer / HostWebServer /
 ElsaStudio 都不是 AppHost 的 ProjectReference ⇒ 必须整解构建」
```

即：大部分服务本来就不靠 `ProjectReference` 保证顺序，靠的是整解构建。

### 2.4 服务端共享输出目录机制

所有服务声明**同一个** `BaseOutputPath`：

```xml
<!-- CJ.Plug.ApiServer / McpServer / ElsaApiServer / StationApiServer / StationAgent / HostWebServer -->
<BaseOutputPath>..\..\..\02.Publish\Services</BaseOutputPath>
```

各项目都在 `CJPlug/src/{分组}/{项目}/`，上溯 3 级均为 `CJPlug/`，因此产物汇聚到：

```
CJPlug/02.Publish/Services/{Debug|Release}/net10.0/
CJPlug/02.Publish/Services/{Debug|Release}/net10.0-windows/
```

AppHost 的输出独立在 `CJPlug/02.Publish/CJ.Plug.AspireHost.AppHost/`，靠 `BaseDirectory/../../..` 回到 `CJPlug/02.Publish/`，再拼 `Services/{Configuration}/net10.0` 得到 `servicesDir`（`AppHost.cs:132-137`）。

**这个机制是本案第一处必须改的耦合点。**

### 2.5 外部定位点（改名/换目录必须同步）

| 位置 | 硬编码内容 | 处数 |
|---|---|---|
| `Desktop/Services/AppHostLauncher.cs:334-355` | 5 个候选路径，全部含 `"CJ.Plug.AspireHost.AppHost"` + `"Debug"/"Release"` + `"net10.0"` | 5 |
| `Desktop/Services/AppHostLauncher.cs:60` | 错误提示文本 | 1 |
| `05.Build/assemble-release.ps1` | `Copy-Tree` 源/目标 + `Assert-Paths` 4 条（第 218、227-229 行） | 6 |

### 2.6 CJOntology 现状（目标接入方）

| 项 | 现状 |
|---|---|
| Aspire 引用 | **零** |
| 启动方式 | VS F5，`launchSettings.json` 单 profile |
| 端口 | `5007`（主）+ `15003` |
| 必需环境变量 | `ASPNETCORE_ENVIRONMENT`、`AgentKernel__RuntimeRoot`（指向 CJWork 的 DSH 运行时）、`AgentKernel__DshHome` |
| 产物目录 | `CJOntology/artifacts`（**无** `Services/{Configuration}` 结构） |

结论：CJOntology 的目录结构与 CJPlug 完全不同，**`servicesDir` 的上溯公式对它必然失效**。

---

## 3. 迁移的三个真实动因

| # | 动因 | 说明 |
|---|---|---|
| 1 | **服务包归位** | `ServiceDefaults` 零产品依赖，CJOntology 接入后立即获得 OTel / 健康检查 / 服务发现 / `ASPNETCORE_CONFIG_PATH` 支持 |
| 2 | **CJOntology 目前不可观测** | 纯 VS F5 启动，无 Dashboard、无统一日志汇聚、无启停开关；3 个环境变量靠手填 `launchSettings` |
| 3 | **AppHost 的能力过剩** | 单端口收敛后它只编排 2 个资源，却背着"6 服务"的历史代码；把它变成通用壳，投入产出比更高 |

---

## 4. 迁移的三个真障碍

| # | 障碍 | 位置 | 为什么必须改 |
|---|---|---|---|
| B1 | `BaseOutputPath` 跨仓相对路径 | `AppHost.csproj:11` | 搬到 `CJCore/src/Apps/` 后上溯 3 级落在 `CJCore/`，那里没有 `02.Publish` |
| B2 | `servicesDir` 靠目录上溯推导 | `AppHost.cs:132-137` | 认定"AppHost 输出与服务目录在同一发布根、固定相隔 3 级"。CJPlug 满足，CJOntology 不满足 |
| B3 | 3 类外部定位点 | 见 §2.5 | 改名或换目录时，任一处漏改表现为"桌面端起不来，日志只说找不到 dll" |

**B2 与"搬不搬"完全正交**：即使 AppHost 留在 CJPlug，它今天也管不了 CJOntology。反之 B2 改好之后，AppHost 放哪都能管。

---

## 5. 方案：四步

### 5.1 步骤 1 —— 去项目引用（不搬家）

**目标**：让 AppHost 成为产品中立的路径启动器。

| 改动 | 文件 | 内容 |
|---|---|---|
| 删 6 个 `ProjectReference` | `AppHost.csproj:18-26` | 全部移除，含 2 个历史遗留 + 4 个构建顺序触发器 |
| 保留 `appsettings.json` 拷贝声明 | 同文件 | 不变 |
| 保留 `CopyShippedAspireOrchestration` 目标 | 同文件 | 不变（产品无关） |

**验收**：`dotnet build CJPlug/02.Publish/.../CJ.Plug.AspireHost.AppHost.csproj` 通过；AppHost 输出目录仍是 230MB（自带 DCP/Dashboard 不变），但不再触发任何 CJPlug 项目构建。

**为什么先做这步**：它是步骤 3 的前置条件。搬完再删，等于在两个地方同时改启动链；先删再搬，问题可定位。

**风险**：失去"单独 build AppHost 就送到服务"的便利。缓解——CJPlug 已有整解构建流程（`assemble-release.ps1`、VS F5），并需在 AppHost 启动前加一条存在性检查（见 §7 D-3）。

### 5.2 步骤 2 —— 服务目录与清单配置化（不搬家）

**目标**：把"推导"改成"声明"。

`appsettings.json` 新增：

```json
{
  "ServicesDirectory": "",          // 空 = 回落上溯推导（保持向后兼容）
  "Resources": [
    {
      "name": "apiservice",
      "dll": "CJ.Plug.ApiServer.dll",
      "workingDirectory": "",        // 空 = 用 ServicesDirectory
      "url": "http://localhost:5066",
      "listeningUrls": "http://*:5066",
      "environment": {
        "ASPNETCORE_CONFIG_PATH": "appsettings.CJ.Plug.ApiServer.json"
      }
    }
  ]
}
```

保留现有 `ResourceStrings` 段作为兼容入口（已有 `ServiceDll()` 从它读取，见 `AppHost.cs:140-141`）。

**代码改动**（`AppHost.cs`）：

| 现状 | 改后 |
|---|---|
| `publishDir = BaseDirectory/../../..`（第 134 行） | `servicesDir = config["ServicesDirectory"] ?? 推导值` |
| 每个资源 `.WithEnvironment(键, 值)` 手写 3 次 | 遍历 `Resources[]`，按 `environment` 字典批量注入 |
| 端口硬编码在 3 处 `WithUrl` + 2 处 `WithEnvironment` | 从资源项读取 |

**验收**：`ServicesDirectory` 指向 CJPlug 的 `Services/{Cfg}/net10.0`，Dashboard 与服务启动行为与改动前完全一致；置空则回落推导，行为不变。

### 5.3 步骤 3 —— 物理迁移

| 源 | 目标 | 类型 |
|---|---|---|
| `CJPlug/src/Framework/CJ.Plug.AspireHost.ServiceDefaults` | `CJCore/src/Framework/CJCore.AspireHost.ServiceDefaults` | 库 |
| `CJPlug/src/Framework/CJ.Plug.AspireHost.AppHost` | `CJCore/src/Apps/CJCore.AspireHost.AppHost` | 可执行 |

`BaseOutputPath` 建议保持指向 CJPlug 的唯一发布根，避免出现两个 `02.Publish`：

```xml
<!-- CJCore/src/Apps/CJCore.AspireHost.AppHost/CJCore.AspireHost.AppHost.csproj -->
<BaseOutputPath>$(MSBuildThisFileDirectory)..\..\..\..\CJPlug\02.Publish\$(MSBuildProjectName)</BaseOutputPath>
```

理由：① `02.Publish` 仍是唯一发布根，打包脚本只需改变量；② 上溯公式作为 `ServicesDirectory` 的兜底仍然算得对。

代价：该值是跨仓相对路径，CJPlug 目录布局调整时需同步。已在 §7 D-2 登记。

同时删除空目录 `CJPlug/src/Framework/CJ.Plug-Aspire.ServiceDefaults/`。

### 5.4 步骤 4 —— 外部定位同步

| 位置 | 改动 |
|---|---|
| 7 个 CJPlug 服务的 csproj | `..\..\Framework\CJ.Plug.AspireHost.ServiceDefaults\...` → `..\..\..\..\CJCore\src\Framework\CJCore.AspireHost.ServiceDefaults\...` |
| `AppHostLauncher.cs:334-355` | 5 个候选的项目名段改为 `CJCore.AspireHost.AppHost`；新增 1 个候选指向 `CJCore/02.Publish/...`（因 `BaseOutputPath` 未变，实际路径同名不同项目名） |
| `AppHostLauncher.cs:60` | 错误提示文本 |
| `assemble-release.ps1:218,227-229` | 6 处项目名段 |
| `CJPlug/CJ.Plug-Aspire.sln` | 移除 2 个条目（第 258、260 行） |
| `CJCore/CJCore.sln` | 新增 2 个条目，`ServiceDefaults` 归入 `Framework` 解决方案文件夹，`AppHost` 归入 `Apps` |

---

## 6. 目标形态

```
CJCore/
  src/
    Framework/
      CJCore.AspireHost.ServiceDefaults/     ← 服务包（零产品依赖）
    Apps/
      CJCore.AspireHost.AppHost/             ← 编排壳（零产品依赖）
        appsettings.json
          ├─ ServicesDirectory: ../CJPlug/02.Publish/Services/Release/net10.0
          └─ Resources[]
               ├─ apiservice   → CJ.Plug.ApiServer.dll        :5066
               └─ stationapi   → CJ.Plug.StationApiServer.dll :7660

  （未来接入 CJOntology 时只改配置，不改代码）
  appsettings.ontology.json
    ├─ ServicesDirectory: ../CJOntology/artifacts/Release
    └─ Resources[]
         └─ ontology-web → CJOntology.Web.dll :5007
              environment:
                AgentKernel__RuntimeRoot: ...
                AgentKernel__DshHome: ...
```

关键：`CJCore.AppHost` 对 CJPlug 与 CJOntology **均无编译期引用**，全部经 `appsettings` 声明。

---

## 7. 改动清单

| # | 文件 | 改动 | 步骤 |
|---|---|---|---|
| D-1 | `AppHost.csproj` | 删 6 个 `ProjectReference`；改 `BaseOutputPath` | 1 / 3 |
| D-2 | `AppHost.cs` | `servicesDir` 改为配置优先；资源清单遍历化 | 2 |
| D-3 | `AppHost.cs` | 启动前校验 `servicesDir` 存在且含至少 1 个目标 DLL，缺失时**显式报错并列出预期路径**（避免 Dashboard 只显示"资源启动失败"） | 1 |
| D-4 | `appsettings.json` | 新增 `ServicesDirectory` + `Resources[]` | 2 |
| D-5 | 7 个服务 csproj | ServiceDefaults 引用路径 | 4 |
| D-6 | `AppHostLauncher.cs` | 5 个候选路径 + 1 处提示文本 | 4 |
| D-7 | `assemble-release.ps1` | 6 处路径 | 4 |
| D-8 | `CJPlug/CJ.Plug-Aspire.sln` | 移除 2 条目 | 4 |
| D-9 | `CJCore/CJCore.sln` | 新增 2 条目 | 4 |
| D-10 | `CJCore/src/.../ServiceDefaults/Extensions.cs` | 注释里"CJPlug 服务架构简化"改为中性表述（逻辑不变） | 3 |
| D-11 | `CJPlug/src/Framework/CJ.Plug-Aspire.ServiceDefaults/`（空目录） | 删除 | 3 |

---

## 8. 风险与回退

### 8.1 风险

| # | 风险 | 影响 | 缓解 |
|---|---|---|---|
| R1 | 删 `ProjectReference` 后，单独构建 AppHost 时服务 DLL 缺失 | Dashboard 显示资源启动失败，真实原因需翻日志 | D-3 的存在性检查 |
| R2 | `BaseOutputPath` 跨仓相对路径 | CJPlug 目录调整时静默失效 | 文档登记 + `AppHost` 启动时打印 `servicesDir` 实际值（已有） |
| R3 | Desktop 5 个候选路径漏改 | 桌面端报"找不到 AppHost 项目"，产品起不来 | 改后逐个候选打日志（`_logger.LogDebug` 已有），启动验证时检查 |
| R4 | CJCore 继承 `Directory.Build.props` 的 `EnableRequestDelegateGenerator=true` | AppHost 非 Web 项目，多引入一个源生成器 | 构建验证；若告警增多，在 AppHost 项目内 `<EnableRequestDelegateGenerator>false</EnableRequestDelegateGenerator>` |
| R5 | CJCore 的 props 无 `RuntimeIdentifiers=win-x64` | AppHost 输出可能多带跨平台原生库 | 构建后比对输出目录体积（当前 230MB） |

### 8.2 回退

每步独立可回退：

| 步骤 | 回退方式 |
|---|---|
| 1 | 恢复 6 个 `ProjectReference` |
| 2 | `ServicesDirectory` 置空即回到推导逻辑（代码保留两条分支） |
| 3 | 两个项目目录移回 `CJPlug/src/Framework/`，`BaseOutputPath` 恢复 |
| 4 | 路径字符串回改；两个 sln 条目回改 |

第 3、4 步建议在同一个提交里完成（否则中间态无法构建）。

---

## 9. 明确不做的事

| 不做什么 | 理由 |
|---|---|
| 不动 CJPlug 服务的业务代码 | 本方案只改启动链，不碰服务实现 |
| 不动端口契约 | 5066/7660 等对外契约保持不变 |
| 不接入 CJOntology | 本方案只准备条件；真正接入是后续独立任务（需先确定 CJOntology 的产物目录规范） |
| 不统一认证模型（P5 / ThreeRoleAdmin） | 与本方案正交，绑在一起会把两件未定的事锁死 |
| 不改 `Program.cs` 的 `AddServiceDefaults()` 调用点 | 引用路径变、方法名不变 |

---

## 10. 决策点

### D1（本轮必决）：迁移落点

| 档 | 落点 | 优点 | 代价 |
|---|---|---|---|
| **A（推荐）** | `ServiceDefaults → CJCore/src/Framework/`；`AppHost → CJCore/src/Apps/` | 与 CJCore 现有分层一致（库进 Framework，可执行进 Apps）；语义清晰 | 需改 2 个 sln |
| B | 两个都放 `CJCore/src/Framework/` | 配套项目相邻，好找 | 可执行项目放在 Framework 层，与 `CJCore.AgentDashboard` 等先例不符 |
| C | 新建根级 `CJSuite.AppHost/`（独立于所有产品仓） | 最中立，未来可管任意产品 | 新增一个仓/目录，打包链要多处理一层；与"移到 CJCore 框架层"的原始意图不符 |

### D2（可延后）：步骤 1+2 是否与 3+4 同批交付

| 档 | 说明 |
|---|---|
| **甲（推荐）** | 分两批：先 1+2（CJPlug 原地，零外部影响），验收后再 3+4。任一步出问题范围可控 |
| 乙 | 一次做完。收益是少一轮构建验证，代价是出问题时无法区分是"配置化"还是"搬家"引入 |

### D3（后续独立议题）：CJOntology 接入

需先确定其产物目录规范（当前为 `artifacts/`，与 CJPlug 的 `02.Publish/Services/{Cfg}/net10.0` 不同构）。本方案只保证 `CJCore.AppHost` 有能力通过配置指向任意目录，不承诺目录规范统一。

---

## 附录 A：步骤 1+2 实施记录（2026-10-29）

> 范围：只做 §5.1 与 §5.2，**均在 CJPlug 原地完成**，不涉及任何外部定位点改动。
> 步骤 3（物理迁移）与步骤 4（外部同步）尚未执行。

### A.1 改动清单

| 文件 | 改动 |
|---|---|
| `CJ.Plug.AspireHost.AppHost.csproj` | 删除 **全部 6 个** `ProjectReference`（含 2 个历史遗留 + 4 个构建顺序触发器），改为零项目引用；原位置改为说明性注释 |
| `AppHost.cs` | ① `servicesDir` 改为「配置优先，缺失回落推导」；② 新增 `ResolveWorkingDirectory()`；③ 新增 `DefaultResources()` 内置兜底清单；④ 新增缺件预检（D-3）；⑤ 两个手写 `AddExecutable` 改为遍历 `Resources[]`；⑥ 删除 `configure` / `ServiceDll` / 6 个 `*DllName` 变量；⑦ 删除 `AspNetCoreUrlsEnvKey` / `ApiServerContractUrls` 常量；⑧ 文件末尾新增 `ServiceResource` 配置模型 |
| `appsettings.json` | `ResourceStrings`（6 项 DLL 名）替换为 `ServicesDirectory`（空=推导）+ `Resources[]`（2 项，含各自 `Environment`） |

### A.2 三个技术决策

| # | 决策 | 理由 |
|---|---|---|
| 1 | **`stationapi` 沿用 `Kestrel__EndPoints__Http__Url`，不改 `ASPNETCORE_URLS`** | `StationApiServer/Program.cs:130` 用自己的 `UseUrls($"http://*:{servicePort}")` 绑定（`servicePort` 来自命令行 → SQLite → 默认 7660），`Kestrel__EndPoints` 本就是冗余注入。换键会改变既有行为，本步只做"搬进配置"，不动语义 |
| 2 | **保留 `DefaultResources()` 兜底** | `appsettings.json` 被替换/丢失时仍能起来，行为与改造前完全一致。两套来源（配置/内置）共用同一遍历逻辑，不是两条分支 |
| 3 | **配置文件按 DLL 名推导** | `CJ.Plug.ApiServer.dll` → `appsettings.CJ.Plug.ApiServer.json`，与各服务 `BaseOutputPath` 汇聚同一目录的既有约定一致；`ConfigFile` 字段只在需要偏离约定时才写 |

### A.3 实测验收

| 判据 | 结果 |
|---|---|
| 构建（AppHost 单项目） | ✅ **0 错误**，1 警告（`ASPIRE010`，Aspire 13.x 既有提示，与本次无关），耗时 **7.5s** |
| 构建是否触发服务项目 | ✅ **否**。输出目录仅 7 个 AppHost 自身文件被重建 |
| 输出目录是否含 CJPlug 程序集 | ✅ **仅 `CJ.Plug.AspireHost.AppHost.dll`**，无其他 `CJ.Plug.*` |
| 输出目录体积 | ✅ **230MB 不变**（186MB 自带 DCP+Dashboard + Aspire.Hosting 自身依赖） |
| 推导分支 | ✅ `Services directory: ...\Services\Debug\net10.0 （推导）` |
| 配置分支 | ✅ 注入 `ServicesDirectory=no-such-dir` → `（配置: no-such-dir）` |
| D-3 缺件预检 | ✅ 打印 2 条预期路径 + `ServicesDirectory` 实际值 + 修复建议 |
| `Resources[]` 遍历 | ✅ `apiservice`（5066 → 200）与 `stationapi`（7660 监听）均由配置拉起 |
| Dashboard | ✅ 15288 就绪（302 登录跳转），自带 DCP/Dashboard 解析成功 |
| 子进程随 AppHost 退出 | ✅ 停止 AppHost 后 5066/7660/15288 全部释放 |

### A.4 顺带修掉的缺陷

| 缺陷 | 原状 | 现状 |
|---|---|---|
| apiservice 的 Dashboard 链接指向已下线端口 | `.WithUrl("http://localhost:8687")` —— 单端口收敛后 8687 已无监听，点开是死链 | `.WithUrl("http://localhost:5066")`，由 `Resources[].Url` 提供 |
| 注释过时 | 「主宿主 = 主 API(8687) + 调度 Hub(8686) + Elsa 引擎(5001) + MCP 面(3001) 四端点一个进程」 | 已按单端口事实改写 |
| 回退路径隐患 | 注释块引用 `dispatchServerDllName` 等变量，变量删除后取消注释会编译失败 | 注释块内改为 DLL 名字面量，取消注释即可用 |

### A.5 步骤 3+4 待办（已执行，见附录 B）

| # | 待办 | 阻塞点 |
|---|---|---|
| 1 | 两个项目物理迁移到 CJCore | 需先确认 D1 落点（已定 A：`Framework/` + `Apps/`） |
| 2 | 7 个服务 csproj 的 ServiceDefaults 相对路径 | 随迁移一起改 |
| 3 | `AppHostLauncher.cs` 5 个候选 + `assemble-release.ps1` 6 处 + 2 个 sln | 随迁移一起改 |
| 4 | 删除空目录 `CJPlug/src/Framework/CJ.Plug-Aspire.ServiceDefaults/` | 随迁移一起做 |

### A.6 回退方式

| 步骤 | 回退 |
|---|---|
| 1 | 恢复 6 个 `ProjectReference`（csproj 顶部注释保留了删除原因，可照原样恢复） |
| 2 | `ServicesDirectory` 置空即回落推导；`Resources[]` 删除即用 `DefaultResources()`；两者都不用动代码 |

---

## 附录 B：步骤 3+4 实施记录（2026-10-29，同日完成）

> 范围：§5.3 物理迁移 + §5.4 外部定位同步。D1 = **A**（`ServiceDefaults → CJCore/src/Framework/`、`AppHost → CJCore/src/Apps/`）。
> 至此四步全部落地，本方案状态转为**已完成**。

### B.1 验收标准对照（§1.2 V1~V5）

| # | 判据 | 结果 | 证据 |
|---|---|---|---|
| V1 | CJCore/src 下存在 Aspire 两项目，`dotnet build CJCore.sln` 通过且**不要求 CJPlug 源码在场** | ✅ | `CJCore.sln` **0 错误**（1m03s，152 个项目）；AppHost 的 csproj **零 `ProjectReference`**，MSBuild 依赖图不指向 CJPlug 任何 csproj |
| V2 | CJPlug 全部服务继续相对路径引用 ServiceDefaults，构建通过 | ✅ | `CJ.Plug-Aspire.sln` **0 错误**（2m05s）；7 个 csproj 改为 `..\..\..\..\CJCore\src\Framework\...` |
| V3 | Desktop 能从发布根定位并拉起 AppHost，Dashboard 就绪，服务全部启动 | ✅ | 新输出目录启动后：`Dashboard 15288 → 302`；`5066 → 200`；`7660` 监听中；`Services directory … （推导）` 分支仍成立 |
| V4 | `assemble-release.ps1` 产出的 `main/` 结构与现在一致（路径可改，内容不可缺） | ✅（静态核对） | 脚本内 4 处项目名已改；Release 构建后 3 条 `Assert-Paths`（dll / dcp.exe / Aspire.Dashboard.exe）实测**全部存在**。完整打包未执行（见 B.5） |
| V5 | `servicesDir` 可通过配置指定 | ✅（上轮已完成） | 本轮迁移后仍打印 `（推导）`；注入 `ServicesDirectory` 即覆盖 |

### B.2 改动清单

| # | 文件 / 目录 | 改动 |
|---|---|---|
| 1 | `CJPlug/src/Framework/CJ.Plug.AspireHost.ServiceDefaults/` → `CJCore/src/Framework/CJCore.AspireHost.ServiceDefaults/` | `Extensions.cs` 迁移 + csproj 重写（项目改名） |
| 2 | `CJPlug/src/Framework/CJ.Plug.AspireHost.AppHost/` → `CJCore/src/Apps/CJCore.AspireHost.AppHost/` | `AppHost.cs` / `appsettings.json` / `Properties/launchSettings.json` 迁移 + csproj 重写 |
| 3 | 新 AppHost csproj | `BaseOutputPath` 改跨仓指向 `CJPlug/02.Publish/$(MSBuildProjectName)`；**补 `RuntimeIdentifiers=win-x64`**；删除指向仓库外的失效 `Compile Remove`（`D:\Pro\CJ.Plug-Aspire\01.Scripts\Utf8ConsoleInit.cs`）；`CopyShippedAspireOrchestration` 目标原样保留 |
| 4 | 7 个服务 csproj（ApiServer / DispatchServer / ElsaApiServer / McpServer / StationApiServer / ElsaStudio / HostWebServer） | ServiceDefaults 引用路径 → `..\..\..\..\CJCore\src\Framework\CJCore.AspireHost.ServiceDefaults\...` |
| 5 | `Desktop/Services/AppHostLauncher.cs` | `DllName` → `CJCore.AspireHost.AppHost.dll`；错误提示文本；5 个探测候选（0~2 只改项目名，3~4 改为跨到兄弟目录 `../CJCore/src/Apps/...`） |
| 6 | `05.Build/assemble-release.ps1` | 头部产出说明 1 处 + `Copy-Tree` 2 处 + `Assert-Paths` 3 处 |
| 7 | `CJSetup/packages.json` | `install.exe` + 2 条 `verify-entry` 路径；顺带把描述里「编排 8 个服务（:8687/:8686/:3001）」改成单端口事实 |
| 8 | `CJPlug/CJ.Plug-Aspire.sln` | 移除 2 个项目条目 + 24 行配置 + 2 行嵌套（2584 → 2558 行，项目数 162） |
| 9 | `CJCore/CJCore.sln` | 新增 2 个条目：ServiceDefaults 归 `Framework` 文件夹、AppHost 归 `Apps` 文件夹（项目数 152） |
| 10 | `AppHost.cs` | 删掉步骤 2 遗留的未使用常量 `KestrelUrlEnvKey`（CS0219 警告），注释块内改用字面量，取消注释仍可直接编译 |
| 11 | 文档同步 | `CJPlug/CLAUDE.md`（编排器位置、目录树、配置文件表）、`CJPlug/README.md`（启动命令、入门项目）、`Desktop/README.md`（publish 命令）、仓库根 `CLAUDE.md` |
| 12 | 共享输出目录清理 | 删除 Services 下 5 个**旧程序集名**残留（`CJ.Plug.AspireHost.ServiceDefaults.dll/.pdb/.deps.json`，Debug + Release），否则会被打进发布包 |

### B.3 三个技术决策

| # | 决策 | 理由 |
|---|---|---|
| 1 | **`BaseOutputPath` 跨仓指向 CJPlug 发布根**，而非在 CJCore 下新建 `02.Publish` | AppHost 运行时的兜底公式是 `BaseDirectory/../../..` + `Services/{Cfg}/net10.0`，前提是「输出目录与服务目录同在一个发布根、相距固定 3 级」。保持输出落点不变 ⇒ 该公式继续算得对，Desktop 的 2 条 `02.Publish` 候选、`assemble-release` 的拷贝源都只需改项目名。代价是跨仓相对路径（§7 D-2 已登记） |
| 2 | **显式补 `RuntimeIdentifiers=win-x64`** | 迁移前该属性由 `CJPlug/Directory.Build.props` 提供，CJCore 那份**没有**。补齐后：① 原生库筛选口径与迁移前一致；② 消除 `$(DcpDir)` / `$(AspireDashboardDir)` 在切仓后解析不到的变数（这两个属性由 RID 相关的 Orchestration / Dashboard 包 targets 给出，`CopyShippedAspireOrchestration` 全靠它们） |
| 3 | **Desktop 的 bin 回退候选改为跨仓路径** | 项目搬走后 `src/Framework/...` 不再存在；但 `02.Publish` 优先级 1/2 已能命中（开发仓常态），跨仓 bin 只是最后的兜底 |

另：CJCore 的 `Directory.Build.props` 带 `EnableRequestDelegateGenerator=true`（风险 R4）。实测**零新增警告**，故未按 R4 追加 `false` 覆盖 —— 该生成器对非 Web 项目无产出。

### B.4 实测验收

| 判据 | 结果 |
|---|---|
| AppHost 单项目构建 | ✅ **0 错误**，8.2s；1 警告（`ASPIRE010`，迁移前即存在） |
| 输出落点 | ✅ `CJPlug/02.Publish/CJCore.AspireHost.AppHost/Debug/net10.0`（跨仓 BaseOutputPath 生效） |
| 自带编排件 | ✅ `aspire/dcp/dcp.exe`、`aspire/dashboard/Aspire.Dashboard.exe` 均在（RID 解析正常） |
| 输出体积 | ✅ **229MB**（迁移前 230MB，无突变） |
| 输出是否混入产品程序集 | ✅ **零个 `CJ.Plug.*.dll`** |
| 服务目录推导 | ✅ `Services directory: …\02.Publish\Services\Debug\net10.0 （推导）` |
| Dashboard | ✅ 15288 → 302（登录跳转） |
| 主服务端 | ✅ `5066/` 200、`/api/mcp/getPublishedWorkflows` 200、`/api/dispatch/GetApiServer` 200、`/api/plugbridge/tools` 200 |
| 工位机服务 | ✅ 7660 监听中（`/` 404 属该服务无根路由的既有行为） |
| 子进程随 AppHost 退出 | ✅ 停止 AppHost 后 5066/7660/15288 全部释放 |
| CJPlug 全量构建 | ✅ **0 错误**，2m05s |
| CJCore 全量构建 | ✅ **0 错误**，1m03s |
| Release 打包断言 | ✅ 3 条 `Assert-Paths` 全部存在 |
| sln 可解析 | ✅ `dotnet sln list`：CJPlug 162 项、CJCore 152 项；新项目嵌套在 `Framework` / `Apps` 下 |

### B.5 未做（明确留给后续）

| # | 未做 | 原因 |
|---|---|---|
| 1 | 未执行完整 `assemble-release.ps1` 打包 | 需整解 Release 构建 + 约 1.25GB 拷贝；本轮以「Release 产物 + 断言路径全部存在」静态核对替代 |
| 2 | 未实机启动 Desktop GUI | Desktop 是 WPF 程序，需人工点击；已用路径存在性验证候选 1（`02.Publish/CJCore.AspireHost.AppHost/Debug/net10.0/CJCore.AspireHost.AppHost.dll` ✓） |
| 3 | 未删除旧输出目录 `CJPlug/02.Publish/CJ.Plug.AspireHost.AppHost`（459MB） | 属历史构建产物，保留可回退；确认无回退需要后可手动删除 |
| 4 | 未接入 CJOntology | §10 D3 独立议题，需先统一其产物目录规范 |
| 5 | `CJSetup/dist/`、`.docs/（已完成）…20261007.md`、`02.Publish`/`05.Release` 内旧名 | 历史产物与历史文档，按「已完成文档不改写」惯例保留 |

### B.6 回退方式（步骤 3+4）

| 动作 | 回退 |
|---|---|
| 项目位置 | 两个目录移回 `CJPlug/src/Framework/`，项目名改回 `CJ.Plug.AspireHost.*` |
| `BaseOutputPath` | 恢复 `..\..\..\02.Publish\$(MSBuildProjectName)`；删除显式 `RuntimeIdentifiers` |
| 服务引用 | 7 个 csproj 路径回改 `..\..\Framework\CJ.Plug.AspireHost.ServiceDefaults\...` |
| 外部定位 | `AppHostLauncher.cs` 3 处、`assemble-release.ps1` 4 处、`packages.json` 3 处、两个 sln 条目回改 |

⚠ 步骤 3+4 必须整批回退：中间态（项目已搬、引用未改）无法构建。

