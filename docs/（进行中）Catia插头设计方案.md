---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 56ab3647c00a3b3fdf8fade204e7a8ee_d3f56fa5a62811f199d2525400287e28
    ReservedCode1: 9vvpRL6NLEe7jttA+Z+oVAQODnoTcEjV+ETK9CEo3OssckAFQXnAFPKeL1bk1spfLPy/MfN96rEbtnM4CZgCnf7Teo0S4FqGKg8r3s5+iiIow3r0GMtHpdfFrk+jwwENvUzLn4q4Rqbuez8TJ1vaSvK+JP7tQyDkUwLYt6lNdLg6GK8Fk6Wr/gxQ93I=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 56ab3647c00a3b3fdf8fade204e7a8ee_d3f56fa5a62811f199d2525400287e28
    ReservedCode2: 9vvpRL6NLEe7jttA+Z+oVAQODnoTcEjV+ETK9CEo3OssckAFQXnAFPKeL1bk1spfLPy/MfN96rEbtnM4CZgCnf7Teo0S4FqGKg8r3s5+iiIow3r0GMtHpdfFrk+jwwENvUzLn4q4Rqbuez8TJ1vaSvK+JP7tQyDkUwLYt6lNdLg6GK8Fk6Wr/gxQ93I=
---

# （进行中）Catia 插头设计方案

> 调研基线：CJPlug `src` 全量代码（以 NX 插头为唯一已落地参照物）+ CATIA V5 COM 自动化能力。
> 状态：进行中 —— 5 个决策点已于 2026-09-01 全部收口（V5 / Late-binding / 自启会话 / 4 动作 / STL Binary）。
> 约束：本文档只做方案设计，不含编码实现。

---

## 0. 结论先行（一句话版）

Catia 插头**完全照搬 NX 插头的架构范式**即可落地：新建 `PlugLibrary/CatiaPlug`（Razor SDK，继承 `StationPlugExecuteService` 走「下载→图站执行」链路）+ `PlugToolIntegrations/CatiaXxx`（net4.8 控制台，改 NX 的 `Session.GetSession()` 为 CATIA 的 COM `Activator.CreateInstance`）驱动 CATIA 读参数 / 改参数 / 导出 STP / 导出 STL。

最大风险不是架构，而是两个**物理前提**必须先行确认：(1) 目标图站的 CATIA 版本形态（V5 vs 3DEXPERIENCE），决定 COM ProgID 与 API；(2) **`ToolName` / `CommandParameter` 占位符必须与 NX 现网不一致的地方对齐**，否则工具反查返回 `null` 导致执行静默失败。

---

## 1. 背景与目标

| 项 | 内容 |
|----|------|
| 需求 | CJPlug 新增「Catia 插头」，支持：① 读取 CATIA 模型参数；② 更新模型参数；③ 导出 STP；④ 导出 STL |
| 同类参照 | `PlugLibrary/NXPlug`（读参数 / 改参数 / 转 STL）—— 架构、目录、DI、种子、COM 物理层全部可复用 |
| 运行环境 | Windows 图站（CATIA 仅 Windows；与 NX 一致，非 Linux 部署） |
| 交付形态 | 主插头 + 4 个系统初始化动作（读参数 / 改参数 / 导出STP / 导出STL），STP/STL 结果可复用现有 `StlViewerPlug` 预览 |

---

## 2. CATIA 集成技术调研（可行性）

### 2.1 CATIA 自动化本质：COM（对比 NX 的 NXOpen）

| 维度 | NX（现有参照） | CATIA（本方案） |
|------|----------------|------------------|
| 自动化接口 | `NXOpen`（托管互操作程序集，早期绑定） | COM (`CATIA.Application` ProgID) |
| 取得会话 | `Session.GetSession()` —— **必须图站已开 NX** | `Activator.CreateInstance(Type.GetTypeFromProgID("CATIA.Application"))` 自启，或 `Marshal.GetActiveObject("CATIA.Application")` 挂载运行中实例 |
| 进程形态 | net4.8 控制台 `Session.GetSession()` | net4.8 控制台 + COM 互操作 |
| 参数来源 | `workPart.Expressions` | `part.Parameters` / `product.Parameters` |
| 结果回传 | `Console.WriteLine` → 图站捕获为 ResultString | 同左 |

> **推荐**：Catia 控制台**自启 CATIA 会话**（`Activator.CreateInstance` + `application.Visible = false` 后台运行），比 NX 的「挂接已运行会话」更稳健，适合无人值守图站批处理。代价是每次执行拉起一个 CATIA 进程（启动较慢，约数秒~数十秒），需评估超时。

### 2.2 四个动作 → CATIA API 映射

| 动作 | CATIA API 调用 | 关键参数 | 输出 |
|------|----------------|----------|------|
| **读取参数** | `doc = app.Documents.Open(path)` → `part.Parameters`（或 `product.Parameters`）遍历 | 递归读取 `Parameters` 集合（含 Length/Angle/Mass/用户参数） | `name=value` 列表序列化为 JSON，写 stdout |
| **更新参数** | 同上打开 → 按 `key=value` 解析 → `param.Value = v` → `part.Update()` → `part.Save()` | 参数字符串（同 NX `NewParameterString`） | 成功/失败消息 |
| **导出 STP** | `part.ExportData(stpPath, "stp")`（`Document.ExportData`） | 输出路径、格式 `"stp"` | 生成 .stp 文件 |
| **导出 STL** | `part.Shape.STLExport(stlPath)` 取 `STLExport` 对象 → 设 `Sag`/`RelativeSegments`/`Mode`/`OutputFormat` → `.Execute()` | 弦高偏差 `Sag`、ASCII/Binary、单位 | 生成 .stl 文件 |

> ⚠️ **STL 导出属性名需按目标 CATIA 类型库校正**：`STLExport` 的 `Sag`（弦高偏差）、`RelativeSegments`（bool）、`Mode`（0=曲面 / 1=实体）、`OutputFormat`（0=ASCII / 1=Binary）、`Units` 等属性名以实际安装版本的类型库为准，设计阶段先按 V5 R2x 常用命名，编码前用 OLEView 或 `CATIA.exe` 导入的互操作程序集核对。

### 2.3 物理前提（决定能否落地，非架构问题）

1. **CATIA 已安装且 COM 注册**：安装时注册 `CATIA.Application` ProgID；图站需有 CATIA。
2. **License**：CATIA 自动化消耗交互式 License（与 NX 同理），图站需有可用 License。
3. **位宽一致**：CATIA V5 现多为 64 位；控制台 AnyCPU/64 位宿主即可，COM 跨位宽不兼容（32 位 CATIA 需 32 位宿主）。
4. **版本形态**：见第 6 节决策点 #1（V5 vs 3DEXPERIENCE 决定 API 入口）。

---

## 3. CJPlug 插头架构（以 NX 为基准，已核对代码）

### 3.1 关键接口与基类（命名空间 / 文件）

| 角色 | 类型 | 文件 |
|------|------|------|
| 设置内容 | `IPlugCommonSettingContent` | `Core/CJ.Plug.PlugBaseCore/Contracts/IPlugCommonSettingContent.cs` |
| 执行 | `IPlugCommonExecute` | `Core/CJ.Plug.PlugBaseCore/Contracts/IPlugCommonExecute.cs` |
| 动作设置 | `IPlugActionSettingContent` | `Core/CJ.Plug.PlugBaseCore/Contracts/IPlugActionSettingContent.cs` |
| 工具执行 | `IToolExecuteService` | `Core/CJ.Plug.PlugBaseCore/Contracts/IToolExecuteService.cs` |
| 主插头基类 | `BasePlugExecuteService` | `Core/CJ.Plug.PlugBaseCore/Services/BasePlugExecuteService.cs` |
| **桌面类母基类** | `StationPlugExecuteService` | `Core/CJ.Plug.PlugBaseCore/Services/StationPlugExecuteService.cs` |
| 工具执行链 | `ToolExecuteService` | `Core/CJ.Plug.PlugBaseCore/Services/ToolExecuteService.cs` |
| 能力声明 | `IPluginCapability` | `Core/CJ.Plug.Models/MCPTools/IPluginCapability.cs` |
| 工具模型 | `Tool` | `Core/CJ.Plug.Models/Station/Tool.cs` |
| 工具种子 | `ToolSeedDataProvider` | `modules/ToolResourceModule/CJ.Plug.ToolResourceApi/Services/ToolSeedDataProvider.cs` |
| 工具反查 | `ToolManageService.GetByDisplayNameAsync` | `modules/ToolResourceModule/CJ.Plug.ToolResourceApi/Services/ToolManageService.cs:135` |

### 3.2 `StationPlugExecuteService` 两阶段状态机（Catia 动作直接继承）

```
提交(JobSubStatus.提交) ──▶ SubmitAsync(executionRequest)          // 子类填 InputVariables
                                                          │
                                                          ▼
                                       ToolExecuteService.ExecuteToolAsync()  // 下载→检测入口→执行
                                                          │
                                                          ▼
图站执行完成(JobSubStatus.图站执行完成) ──▶ WriteResultStringToPDZ()           // 写回 PDZ
                                          └─▶ OnStationCompletedAsync()         // 可选：文件回写
```

子类必填抽象成员（已核对 `StationPlugExecuteService.cs:23-67`）：

```csharp
protected abstract string ToolName { get; }              // 必须 == 种子 Tool.ToolName（见 6.2 坑）
protected abstract string ToolVersion { get; }
protected abstract string[]? DataPrepareVariableNames { get; }
protected abstract Task<ExecuteResultData?> SubmitAsync(PlugExecutionRequest r);
protected virtual string ResultStringVariableName => "ResultString";   // 可重写
protected virtual Task OnStationCompletedAsync(...) => Task.CompletedTask;
```

### 3.3 工具下载→检测入口→执行链（`ToolExecuteService.ExecuteToolAsync`，已核对）

1. 解析 Tool：`ResolvedTool` 优先，否则 `GetToolByDisplayNameAsync($"{ToolName}({ToolVersion})")`；
2. 解析 Station：`GetStationToUseByTool(...)`；
3. 文件型变量下载到图站 → 变量值替换为本地路径；
4. 按 `StationConfigTable/ToolConfig` 覆盖 `Tool.ToolPath`；
5. 下载工具包（除非 `SkipDownloadToStation=true`），`CheckFileExistsAsync` **检测入口 exe 存在**；
6. `EvalCommandLine` 把 `[ToolPath]`/`[VarName]` 替换为实际值；
7. `SubmitNewToolExecute` 在图站拉起进程，**控制台输出即 ResultString**。

> Catia 集成 exe 走**完全一致**的链路，无需改 `ToolExecuteService`。

### 3.4 NX 现网接线点（Catia 照抄）

- DI 注册：`PlugLibrary/PlugsBundle/ServiceCollectionExtensions.cs` 中 `AddPlugsBundle()`→`AddNX()`、`AddPlugsExecutebundle()`→`AddNXExecute()`。**Catia 加 `AddCatia()` / `AddCatiaExecute()`。**
- 物理集成工程：`src/PlugToolIntegrations/NXGetParameters|NXUpdateParameters|NXToStl`（net4.8 控制台）。**Catia 加 `CatiaGetParameters|CatiaSetParameters|CatiaExportStp|CatiaExportStl`。**
- STL 预览：`PlugLibrary/CAD/StlViewerPlug` + `PlugLibrary/CAD/ThreeJsIntegration`（NXToStl 已复用，Catia 导出 STL 直接复用）。

---

## 4. Catia 插头设计方案

### 4.1 项目结构（仿 `PlugLibrary/NXPlug`）

```
src/PlugLibrary/CatiaPlug/
├── CatiaPlug.csproj              # Razor SDK, net10.0, 引 PlugBaseCore + CAD/ThreeJsIntegration
├── PlugKeySetting.cs             # TypeKey / ExecuteKey 常量
├── InitVariableNames.cs          # 变量枚举（CatiaFile / ModelParameters / CatiaParameters /
│                                 #   StpOutputPath / StlOutputPath / Sag / ...）
├── Capabilities/                 # IPluginCapability：CatiaGetParameters / CatiaSetParameters /
│                                 #   CatiaExportStp / CatiaExportStl
├── Services/
│   ├── CatiaPlugCommonSettingContent.cs        # 主插头设置（含 GetDefaultChildPlugs 4 动作）
│   ├── CatiaPlugCommonExecuteService.cs        # : BasePlugExecuteService，读 ModelParameters 派发改参
│   ├── CatiaGetParametersCommonExecuteService.cs   # : StationPlugExecuteService
│   ├── CatiaSetParametersCommonExecuteService.cs  # : StationPlugExecuteService
│   ├── CatiaExportStpCommonExecuteService.cs       # : StationPlugExecuteService
│   └── CatiaExportStlCommonExecuteService.cs       # : StationPlugExecuteService
├── Pages/                        # *.razor 设置页（主插头 + 4 动作）
├── Extensions/ServiceCollectionExtensions.cs  # AddCatia / AddCatiaExecute
└── wwwroot/CatiaPlug.ico

src/PlugToolIntegrations/
├── CatiaGetParameters/Program.cs     # net4.8, COM 读参数 → JSON stdout
├── CatiaSetParameters/Program.cs     # net4.8, COM 改参数 → Save
├── CatiaExportStp/Program.cs         # net4.8, ExportData(path,"stp")
└── CatiaExportStl/Program.cs         # net4.8, STLExport.Execute()
```

### 4.2 动作拆分（推荐：镜像 NX = 4 动作）

| 动作 | PlugTypeKey | 类别 | ToolName(种子) | 变量 |
|------|-------------|------|----------------|------|
| 读参数 | `CatiaGetParameters` | 桌面类动作 | `获取Catia模型参数` | `ModelFilePath`, `ResultString` |
| 改参数 | `CatiaSetParameters` | 桌面类动作 | `设置Catia模型参数` | `ModelFilePath`, `NewParameterString`, `ResultString` |
| 导出STP | `CatiaExportStp` | 桌面类动作 | `Catia模型转STP` | `ModelFilePath`, `StpOutputPath`, `ResultString` |
| 导出STL | `CatiaExportStl` | 桌面类动作 | `Catia模型转STL` | `ModelFilePath`, `StlOutputPath`, `Sag`, `ResultString` |

主插头 `CatiaPlug`（类别 `桌面类`，`Group=工具集成`）`GetDefaultChildPlugs()` 返回上述 4 个 `Plug`（仿 `NXPlugCommonSettingContent.cs:85-108`）。

> 变量命名**统一用真实占位符名**：ExecuteService 注入的 `InputVariables.Name` 必须与种子 `CommandParameter` 的 `[...]` 完全一致（如 `ModelFilePath`⇔`[ModelFilePath]`）。**这是 NX 现网已踩的坑（见 6.2）。**

### 4.3 执行服务骨架（以 `CatiaExportStl` 为例，仿 `NXToStlCommonExecuteService.cs`）

```csharp
public class CatiaExportStlCommonExecuteService : StationPlugExecuteService
{
    protected override string ToolName => "Catia模型转STL";      // == 种子 Tool.ToolName
    protected override string ToolVersion => "1.0";
    protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(CatiaExportStlVariables));
    protected override string ResultStringVariableName => CatiaExportStlVariables.ResultString.ToString();
    public override bool IsThisPlugTypeKey(string? k) => k == PlugKeySetting.CatiaExportStl.CommonExecuteKey;

    protected override async Task<ExecuteResultData?> SubmitAsync(PlugExecutionRequest r)
    {
        // Standalone：从 r.InputVariables 取；Normal：从 PDZ 取（仿 NX SubmitStandalone/SubmitNormal）
        // 构造 InputVariables(ModelFilePath, StlOutputPath, Sag) → ToolExecuteService!.ExecuteToolAsync(r)
    }
}
```

### 4.4 COM 集成程序骨架（以 `CatiaExportStp/Program.cs` 为例，net4.8）

```csharp
// 推荐 late-binding（dynamic），版本无感；或用 TlbImp 从图站 CATIA.exe 生成 Interop.CATIA.dll
var catiaType = Type.GetTypeFromProgID("CATIA.Application");
dynamic app = Activator.CreateInstance(catiaType);
app.Visible = false;                       // 后台运行
dynamic doc = app.Documents.Open(args[0]); // CATPart / CATProduct
dynamic part = doc.Product ?? ((dynamic)doc).GetType().Name == "PartDocument" ? ((dynamic)doc).Part : null;
part.ExportData(args[1], "stp");           // 导出 STP
Console.WriteLine("处理完成");             // → ResultString
```

> 读参数遍历 `part.Parameters`；改参数 `param.Value = v` 后 `part.Update()` + `part.Save()`；STL 用 `part.Shape.STLExport(path).Execute()`（属性名见 2.2 注）。

### 4.5 工具种子（仿 `ToolSeedDataProvider.DefaultTools`，追加 5 条）

```csharp
new() { ToolName="获取Catia模型参数", ToolVersion="1.0", ToolCompany="CJ",
        ToolPath=@"Tools\0System\CatiaGetParameters.exe",
        CommandParameter="[ToolPath] [ModelFilePath]" },
new() { ToolName="设置Catia模型参数", ToolVersion="1.0",
        ToolPath=@"Tools\0System\CatiaSetParameters.exe",
        CommandParameter="[ToolPath] [ModelFilePath] [NewParameterString]" },
new() { ToolName="Catia模型转STP", ToolVersion="1.0",
        ToolPath=@"Tools\0System\CatiaExportStp.exe",
        CommandParameter="[ToolPath] [ModelFilePath] [StpOutputPath]" },
new() { ToolName="Catia模型转STL", ToolVersion="1.0",
        ToolPath=@"Tools\0System\CatiaExportStl.exe",
        CommandParameter="[ToolPath] [ModelFilePath] [StlOutputPath] [Sag]" },
new() { ToolName="Catia", ToolVersion="1.0", SkipDownloadToStation=true,
        ToolPath=@"C:\Program Files\Dassault Systemes\...\CATIA.exe",
        ToolDescription="CATIA（图站本地已装，SkipDownloadToStation）" }
```

### 4.6 接入点（两处 DI）

- `PlugLibrary/PlugsBundle/ServiceCollectionExtensions.cs`：`AddPlugsBundle()` 加 `services.AddCatia();`，`AddPlugsExecutebundle()` 加 `services.AddCatiaExecute();`
- `CatiaPlug/Extensions/ServiceCollectionExtensions.cs`：`AddCatia`（注册设置/动作内容）+ `AddCatiaExecute`（注册 5 个 `IPlugCommonExecute`）。

### 4.7 能力声明（`IPluginCapability`，仿 `NXToStlPluginCapability.cs`）

4 个能力类，声明 `Inputs/Outputs/Tags`，供 AI/Agent 编排（MCP 路径 `ExecuteMode=Standalone` 用 `InputVariables` 直传）。

---

## 5. 与现有链路的对齐（无需改框架）

| 环节 | 复用现状 | Catia 需做 |
|------|----------|-----------|
| 变量解析 | `BasePlugExecuteService.DataPrepare` + PDZ | 声明 `InitVariableNames` 枚举即可 |
| 下载/检测入口/执行 | `ToolExecuteService` | 仅填对 `ToolName`/`CommandParameter` |
| 结果写回 | `WriteResultStringToPDZ` | 控制台 stdout 即 ResultString |
| STL 预览 | `StlViewerPlug` / `ThreeJsIntegration` | 导出 STL 后直接复用，无需新前端 |
| 主插头派发 | `NXPlugCommonExecuteService` 模式 | 仿写 `CatiaPlugCommonExecuteService` |

---

## 6. 关键技术风险与决策点（均给推荐答案）

### 6.1 决策点 #1：目标 CATIA 版本形态 ⭐ 最高优先级

| 选项 | 说明 | 推荐 |
|------|------|------|
| **CATIA V5**（传统桌面，COM `CATIA.Application`） | 本方案全部 API 适用 | ✅ 默认按 V5 设计 |
| CATIA 3DEXPERIENCE | 原生 App 不暴露 V5 COM；需经 3DEXPERIENCE 自动化入口（URL/不同 ProgID），API 差异大 | 暂不纳入首版 |
| 两者都要 | 抽象一层 `ICatiaAutomation` 按版本切换 | 二期 |

> 需你确认：**图站装的是 V5 还是 3DEXPERIENCE？具体版本号（如 V5 R2022）？** 这决定 ProgID 与 `STLExport` 属性名。

### 6.2 风险 #2（硬坑）：`ToolName` / `CommandParameter` 三处必须逐字一致

已核对代码：`GetByDisplayNameAsync` 用 `Tool.ToolName`（DB 列）**精确**匹配（`ToolManageService.cs:142`）。NX 现网已不一致：

| 项 | NX 现网（有问题） | Catia 必须 |
|----|-------------------|-----------|
| ExecuteService.ToolName | `"NXToStl"` | 与种子 `Tool.ToolName` 完全相同 |
| 种子 Tool.ToolName | `"NX模型转STL"` | 同上 |
| CommandParameter 占位符 | `[ModelFilePath]/[StlFilePath]` | 与 ExecuteService 注入的变量名一致（如 `[ModelFilePath]/[StlOutputPath]`） |

**结论**：Catia 落地时，`CatiaXxxCommonExecuteService.ToolName` ≡ 种子 `Tool.ToolName` ≡ `CommandParameter` 的 `[...]`，三处一次对齐，避免工具反查返回 `null` 静默失败。

### 6.3 决策点 #2：COM 绑定方式

| 选项 | 说明 | 推荐 |
|------|------|------|
| **Late-binding（`dynamic` + `Type.GetTypeFromProgID`）** | 版本无感，无需 Interop DLL，部署简单 | ✅ 首版采用 |
| 早期绑定（`Interop.CATIA.dll`，TlbImp 从图站 CATIA.exe 生成） | 有 IntelliSense、类型安全 | 版本升级需重生成，二期可选 |

### 6.4 决策点 #3：CATIA 会话模式

| 选项 | 说明 | 推荐 |
|------|------|------|
| **自启会话**（`Activator.CreateInstance`，`Visible=false`） | 无人值守稳健，每次拉起进程 | ✅ 默认 |
| 挂载运行中（`Marshal.GetActiveObject`） | 复用已开 CATIA，省启动；但要求图站常开 CATIA | 备选 |

> 代价：自启每次约数秒~数十秒启动；需在图站执行超时/队列层面评估。

### 6.5 风险 #4：参数类型与单位

CATIA 参数含 Length / Angle / Mass / 用户参数，且带单位。改参时需按参数实际类型赋值（字符串/数值），`part.Update()` 后才生效。**建议读参输出包含参数名+值+单位**，改参 Strict 校验类型，避免 `Update()` 失败。

### 6.6 风险 #5：License 与图站并发

CATIA 自动化占交互 License；多任务并发会抢 License。建议图站级串行或 License 池管控（复用 NX 图站调度机制）。

---

## 7. 实施步骤清单（落地顺序）

1. **确认 6.1 版本形态** → 校正 ProgID / `STLExport` 属性名。
2. `PlugLibrary/CatiaPlug/` 建项目（仿 `NXPlug.csproj`）+ `PlugKeySetting.cs` / `InitVariableNames.cs`。
3. 写 5 个执行服务（主插头 `BasePlugExecuteService` + 4 动作 `StationPlugExecuteService`）。
4. 写设置内容类（主插头 `IPlugCommonSettingContent` + 动作 `IPlugActionSettingContent`）+ `Pages/*.razor`。
5. 写 4 个 `IPluginCapability` 能力类。
6. `PlugToolIntegrations/` 建 4 个 net4.8 控制台（COM 驱动）。
7. `ToolSeedDataProvider.DefaultTools` 追加 5 条工具种子（**严格对齐 6.2**）。
8. `PlugsBundle/ServiceCollectionExtensions.cs` 接 `AddCatia` / `AddCatiaExecute`。
9. 在 `Core/CJ.Plug.Models/Plug/PlugGlobalEnum.cs` 追加 `CatiaXxx` 段（若主插头派发沿用 `PlugGlobalEnum` 模式）。
10. 图站部署集成 exe 至 `Tools\0System\` + 确认 CATIA 安装路径（`Catia` 本体种子 `ToolPath`）。
11. 联调：读参 → 改参 → 导出 STP/STL → STL 预览（复用 `StlViewerPlug`）。

---

## 附录 A：关键文件索引（已核对）

| 用途 | 文件 |
|------|------|
| 桌面类母基类 | `src/Core/CJ.Plug.PlugBaseCore/Services/StationPlugExecuteService.cs` |
| 工具执行链 | `src/Core/CJ.Plug.PlugBaseCore/Services/ToolExecuteService.cs` |
| 工具反查（精确匹配 ToolName） | `src/modules/ToolResourceModule/CJ.Plug.ToolResourceApi/Services/ToolManageService.cs:135` |
| 工具种子 | `src/modules/ToolResourceModule/CJ.Plug.ToolResourceApi/Services/ToolSeedDataProvider.cs` |
| NX 参照（设置/执行） | `src/PlugLibrary/NXPlug/Services/*` |
| NX 参照（COM 物理层） | `src/PlugToolIntegrations/NXGetParameters|NXUpdateParameters|NXToStl/Program.cs` |
| NX 参照（DI 接入） | `src/PlugLibrary/PlugsBundle/ServiceCollectionExtensions.cs` |
| NX 参照（能力声明） | `src/PlugLibrary/NXPlug/Capabilities/NXToStlPluginCapability.cs` |
| STL 预览复用 | `src/PlugLibrary/CAD/StlViewerPlug`、`src/PlugLibrary/CAD/ThreeJsIntegration` |
| 变量枚举参照 | `src/PlugLibrary/NXPlug/InitVariableNames.cs`、`PlugKeySetting.cs` |
| 全局枚举参照 | `src/Core/CJ.Plug.Models/Plug/PlugGlobalEnum.cs` |

---

## 附录 B：决策登记（已收口 2026-09-01）

| # | 决策点 | 结论 | 状态 |
|---|--------|------|------|
| 1 | 目标 CATIA 版本形态 | CATIA V5（COM `CATIA.Application`） | ✅ 已决 |
| 2 | COM 绑定方式 | Late-binding（`dynamic` + `Type.GetTypeFromProgID`） | ✅ 已决 |
| 3 | CATIA 会话模式 | 自启会话（`Activator.CreateInstance`，`Visible=false`） | ✅ 已决 |
| 4 | 动作粒度 | 4 动作（读/改/STP/STL） | ✅ 已决 |
| 5 | STL 默认格式 | Binary（默认；执行时 `STLExport.OutputFormat` 可被变量覆盖） | ✅ 已决 |
*（内容由AI生成，仅供参考）*
