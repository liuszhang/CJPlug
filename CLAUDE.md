# CJ.Plug-Aspire 项目架构文档

## 项目概述

这是一个基于 **.NET 10.0 + Microsoft Aspire** 的企业级**插件化工作流执行平台**，用于工业自动化场景。核心能力：**将任意类型工具（exe、脚本、API、NX CAD、Python 等）快速封装为标准化插件，并通过 MCP (Model Context Protocol) 协议暴露为 AI Agent 可调用的 Tool**。

## 技术栈

- **框架**: .NET 10.0 + Microsoft Aspire（分布式编排）
- **前端**: Blazor + MudBlazor 组件库
- **工作流**: Elsa Workflows（可视化流程设计、执行引擎）
- **ORM**: Entity Framework Core (SQLite/PostgreSQL)
- **实时通信**: SignalR
- **MCP 协议**: `ModelContextProtocol.AspNetCore` v1.4.0 + `ModelContextProtocol` v0.3.0
- **AI 集成**: Ollama (本地) + OpenRouter API (云端)，支持 DeepSeek/Qwen/Llama
- **日志**: Serilog + SignalR Sink

## 核心架构层次

| 层级 | 目录 | 说明 |
|------|------|------|
| **核心层** | `src/Core/` | 领域模型、插件基类、API客户端、MCP模型、共享组件 |
| **框架层** | `src/Framework/` | Aspire AppHost 编排器、ServiceDefaults（OTel/服务发现/弹性） |
| **服务层** | `src/PlugApiServer/` | API服务、调度服务(SignalR Hub)、Elsa引擎、**MCP服务** |
| **托管层** | `src/PlugWebHost/` | AllInOneServer、Elsa设计器、WASM托管、HostWebServer |
| **工作站层** | `src/PlugStation/` | 工作站API服务、StationAgent(命令行执行器)、VNC远程桌面 |
| **插件库** | `src/PlugLibrary/` | 20+ 插件实现，统一通过 PlugsBundle 注册 |
| **工具集成** | `src/PlugToolIntegrations/` | 独立可执行工具（NX、Word、PythonSDK、ToolMng等） |
| **业务模块** | `src/modules/` | 27个功能模块（三层架构：Api/ApiClient/UI） |

---

# 核心主题：将任意工具快速变为 MCP TOOL

这是本项目最重要的能力。一个工具从诞生到被 AI Agent 通过 MCP 协议调用的完整路径如下：

## 概览：三条路径，由快到深

| 路径 | 适用场景 | 改动量 | 说明 |
|------|---------|--------|------|
| **路径A：发布已有流程/插件** | 已有工作流或插件，想暴露给 AI | 零代码 | 在 MCP 管理界面一键发布 |
| **路径B：添加 `IPluginCapability`** | 新插件想让 AI 理解其参数语义 | 1个类 ~40行 | 声明输入输出，AI Workflow Builder 可自动编排 |
| **路径C：添加静态 `[McpServerTool]`** | 纯代码工具（无需工作流） | 1个类 + 方法 | 编译时注册，适合系统级操作 |

---

## 路径A：零代码发布（管理员/用户操作）

**适用**: 已有 Elsa 工作流或已注册插件，想立刻让 AI 调用。

**操作**：在 MCP 管理界面（`/MCPToolManage`）点击"发布"，选择目标工作流或插件。

**背后发生了什么**（自动化链路）：

```
MCPToolsManageService.PublishToolAsync()
  │
  ├─ 1. 保存 MCPTool 记录到数据库 (ToolType="Workflow"|"Plugin")
  ├─ 2. 创建 Use0 PDZ（参数模板）：从 Design PDZ 克隆，标记可浏览变量为 IsInput=true
  └─ 3. 通知 McpServer 刷新工具缓存
        │
        ▼
HttpMcpToolChangeNotifier.NotifyAsync()
  └─ POST → DispatchServer /api/dispatch/notifyMcpToolUpdated
        │
        ▼
DispatchServer 通过 SignalR Hub 广播 "MCPToolUpdated" 事件
        │
        ▼
McpServer 的 DynamicToolRegistry 接收事件
  ├─ 调用 ApiServer GET /api/mcptools/getPublishedWorkflows
  ├─ 获取 PublishedWorkflowDto 列表（含 EntryVariables）
  ├─ McpSchemaGenerator.GenerateInputSchema() 生成 JSON Schema
  │   └─ McpTypeMapper: String→"string", Int→"integer", Float→"number", Bool→"boolean", File→"string"
  ├─ ToolNameSanitizer.Sanitize() 确保名称符合 MCP 规范 ^[A-Za-z0-9_.-]{1,128}$
  └─ 构建 DynamicWorkflowTool 实例，Tool 出现在 MCP 客户端 tools/list 中
```

**效果**：MCP 客户端（Claude、Cursor 等）立即可见该 Tool，包含完整的类型化 InputSchema。

**关键文件**:
- `src/modules/MCPToolsModule/CJ.Plug.MCPToolsManageApi/Services/MCPToolsManageService.cs` — PublishToolAsync()
- `src/modules/MCPToolsModule/CJ.Plug.MCPToolsManageApi/Services/HttpMcpToolChangeNotifier.cs` — 通知链路
- `src/PlugApiServer/CJ.Plug.McpServer/Services/DynamicToolRegistry.cs` — 动态工具注册中心
- `src/PlugApiServer/CJ.Plug.McpServer/Services/DynamicWorkflowTool.cs` — MCP Tool 适配器
- `src/Core/CJ.Plug.Models/MCPTools/McpSchemaGenerator.cs` — JSON Schema 生成器

---

## 路径B：注册插件能力（开发者的常规路径）

**适用**: 开发了新插件，想让 AI 理解它并自动编排到工作流中。

### 步骤1：实现 IPluginCapability（~40行代码）

```csharp
// 位置: src/PlugLibrary/YourPlug/Capabilities/YourPluginCapability.cs
using CJ.Plug.Models.MCPTools;

public class YourPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "YourPlug";       // 与 IPlugCommonExecute.IsThisPlugTypeKey() 一致
    public string Name => "你的工具名称";              // AI 可读名称
    public string Description => "这个工具的功能描述，会被注入 LLM System Prompt";  // 越详细越好
    public string[] Tags => new[] { "标签1", "标签2" }; // AI 语义匹配用

    public List<CapabilityParameter> Inputs => new()
    {
        new() { Name = "inputFile", Type = "File", Description = "输入文件路径", IsRequired = true },
        new() { Name = "quality", Type = "Int", Description = "质量等级(1-100)", IsRequired = false, Value = "80" },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new() { Name = "outputFile", Type = "File", Description = "输出文件路径" },
    };
}
```

### 步骤2：注册到 CapabilityRegistry

```csharp
// 在 src/PlugApiServer/CJ.Plug.ApiServer/Program.cs 的 startup 中：
var capRegistry = app.Services.GetRequiredService<CapabilityRegistry>();
capRegistry.Register(new YourPluginCapability());
```

### 步骤3（可选）：DI 注册插件执行器

```csharp
// 在 PlugsBundle/ServiceCollectionExtensions.cs 中：
services.AddScoped<IPlugCommonExecute, YourPlugCommonExecuteService>();
services.AddScoped<IPlugCommonSettingContent, YourPlugCommonSettingContent>();
```

### 完成后的效果

1. **AI Workflow Builder** (`AiWorkflowBuilderService`) 的 System Prompt 自动包含你的插件描述和参数
2. 用户说"帮我把所有输入文件转换后压缩"→ AI 自动编排含你插件的流程
3. **MCP Tool 发布**时，如果 ToolType 是 "Plugin"，`GetPublishedWorkflowsAsync()` 会从 `CapabilityRegistry` 读取参数定义，自动生成 InputSchema

**关键文件**:
- `src/Core/CJ.Plug.Models/MCPTools/IPluginCapability.cs` — 接口定义
- `src/Core/CJ.Plug.Models/MCPTools/CapabilityRegistry.cs` — 注册中心 + `ToPromptContext()` 生成 LLM Prompt
- `src/modules/ProcessManageModule/CJ.Plug.ProcessManageApi/Services/AiWorkflowBuilderService.cs` — AI 流程生成
- `src/modules/ProcessManageModule/CJ.Plug.ProcessManageApi/Services/WorkflowTranslator.cs` — 流程翻译存储

---

## 路径C：静态 MCP Tool（纯代码工具）

**适用**: 不需要工作流引擎的系统级操作（如列出所有流程、查询状态）。

### 步骤：注解一个静态方法

```csharp
// 位置: src/PlugApiServer/CJ.Plug.McpServer/Services/YourTools.cs
using ModelContextProtocol.AspNetCore;

[McpServerToolType]
public static class YourTools
{
    [McpServerTool]
    public static string YourTool(
        [Description("参数说明")] string param1,
        [Description("可选参数")] int param2 = 10)
    {
        // 工具逻辑
        return System.Text.Json.JsonSerializer.Serialize(new { result = "ok" });
    }
}
```

**自动生效**：`.WithToolsFromAssembly()` 在 McpServer 启动时扫描所有 `[McpServerToolType]` 类。

**内置静态工具参考**: `src/PlugApiServer/CJ.Plug.McpServer/Services/WorkflowTools.cs`
- `ListPublishedWorkflows` — 列出所有已发布 MCP 工具
- `ExecutePublishedWorkflow` — 按 ID 执行工作流
- `GetWorkflowSchema` — 获取工作流 JSON Schema
- `GetExecutionStatus` — 查询执行状态

---

## MCP 工具调用执行流程（端到端）

```
MCP Client (Claude/Cursor)
  │  tools/call { name: "my_tool", arguments: { inputFile: "...", quality: 80 } }
  ▼
McpServer (port 3001)
  │ DynamicWorkflowTool.InvokeAsync(args)
  │ ├─ 构建 McpToolExecutionRequest
  │ └─ POST → ApiServer /api/plug/executeMcpTool
  ▼
ApiServer (port 8687)
  │ PlugExecuteService.ExecuteMcpTool()
  │ └─ PlugExecutionEngine.StartExecutePlug()
  │     │
  │     └─ ExecuteFlowSourcePathAsync() 路由:
  │         ├─ ToolType="Workflow" → McpWorkflowRunner (Use0 PDZ → Job PDZ → Elsa)
  │         └─ ToolType="Plugin"   → ExecuteMcpPluginPathAsync (解析Handler → 注入变量 → 执行)
  ▼
工作站 (StationApiServer + StationAgent)
  │ DefaultStationExecuteService
  │ └─ cmd.exe /c {generated_command}
  │     └─ stdout 逐行回报 SignalR Hub
  ▼
SignalR Hub 广播结果 → ApiServer 回报 → 触发后续流程节点
```

---

## 完整的服务体系

### Aspire 编排（8个服务）

| 服务 | 端口 | 角色 |
|------|------|------|
| `sds` (DispatchServer) | 8686 | SignalR Hub 中枢，实时消息广播 |
| `apiservice` (ApiServer) | 8687 | 主 REST API，所有业务逻辑入口 |
| `stationapi` (StationApi) | 7660 | 工作站 API，接收执行指令 |
| `elsaapiserver` (ElsaApi) | 5001 | Elsa 工作流引擎 API |
| `elsastudio` (ElsaStudio) | 5010 | Elsa 可视化流程设计器 |
| `webfrontend` (HostWebServer) | 5066 | Blazor 前端入口 |
| `mcpserver` (McpServer) | 3001 | MCP 协议服务器 |
| `mcpInspector` | 6274 | MCP 调试工具 (`npx @modelcontextprotocol/inspector`) |

编排器位置: `src/Framework/CJ.Plug.AspireHost.AppHost/AppHost.cs`

### 信号中枢：SignalR Hub 拓扑

```
DispatchServer:8686/mainHub
  ├── StationApiServer ─── 上报工作站状态、在线心跳
  ├── ToolMng ───── 上报工具代理状态 (toolAgentStatus hub)
  ├── AllInOneServer ── 收集在线工作站 IP (HubConnectionManagerService)
  ├── McpServer ─── 接收 MCPToolUpdated 事件，实时刷新工具列表
  └── WebFrontend ── 前端实时日志/状态展示
```

---

## 目录结构（完整版）

```
src/
├── Core/                                    # 核心基础层
│   ├── CJ.Plug.Models/                      # 领域模型
│   │   ├── Plug/                            #   插件模型 (PlugSettings, PlugExecutionRequest)
│   │   ├── MCPTools/                        #   ★ MCP 工具模型 (MCPTool, CapabilityRegistry,
│   │   │                                    #     McpSchemaGenerator, IPluginCapability, ...)
│   │   ├── PlugMarket/                      #   插件市场模型
│   │   ├── ApiClient/                       #   StationApiClient
│   │   └── ...
│   ├── CJ.Plug.PlugBaseCore/                # 插件执行核心
│   │   ├── Contracts/                       #   IPlugCommonExecute, IPlugCommonSettingContent,
│   │   │                                    #   IToolExecuteService, IPlugExecuteHandlerService
│   │   └── Services/                        #   BasePlugExecuteService, ToolExecuteService
│   ├── CJ.Plug.ApiClient/                   # API 客户端 (Partial Class 聚合各模块)
│   ├── CJ.Plug.SharedPages/                 # 共享 Blazor UI 组件
│   └── Integrations/                        # 引擎集成
│       ├── CJ.Plug.ElsaIntegration/         #   Elsa 集成 (ActivityProvider, ApiClient, EngineService)
│       ├── CJ.Plug.WorkflowCoreIntegration/ #   WorkflowCore 集成
│       └── CJ.Plug.AmisIntegration/         #   Amis UI 集成
│
├── Framework/                               # 框架层
│   ├── CJ.Plug.AspireHost.AppHost/          # Aspire 编排器入口 (AppHost.cs)
│   └── CJ.Plug.AspireHost.ServiceDefaults/  # OTel / 服务发现 / HTTP 弹性
│
├── PlugApiServer/                           # API 服务层
│   ├── CJ.Plug.ApiServer/                   # 主 API 服务器 (port 8687)
│   ├── CJ.Plug.DispatchServer/              # 调度服务器 + SignalR Hub (port 8686)
│   ├── CJ.Plug.ElsaApiServer/              # Elsa 工作流引擎 (port 5001)
│   └── CJ.Plug.McpServer/                   # ★ MCP 协议服务器 (port 3001)
│       ├── Program.cs                       #   AddMcpServer() + 自定义 Handler
│       ├── Services/DynamicToolRegistry.cs  #   动态工具注册（SignalR 实时刷新）
│       ├── Services/DynamicWorkflowTool.cs  #   MCP Tool 适配器（Dto→Tool→Invoke）
│       ├── Services/WorkflowTools.cs        #   静态 MCP Tools (列表/执行/状态查询)
│       └── Tools/ToolNameSanitizer.cs       #   工具名规范化
│
├── PlugWebHost/                             # Web 托管层
│   ├── CJ.Plug.AllInOneServer/              # 一体化服务器 (Elsa + SignalR + PlugsBundle)
│   ├── CJ.Plug.ElsaStudio/                  # Elsa 可视化设计器 (port 5010)
│   ├── CJ.Plug.HostWebServer/               # 主 Web 前端入口 (port 5066)
│   └── CJ.Plug.MainPageContent/             # 主页面内容组件
│
├── PlugStation/                             # 工作站层
│   ├── CJ.Plug.StationApiServer/            # 工作站 API 服务 (port 7660)
│   │   ├── Services/DefaultStationExecuteService.cs  # 工具进程启动/管理
│   │   ├── Services/StationHubService.cs             # SignalR 连接/心跳
│   │   └── Services/UltraVncService.cs               # 远程桌面
│   ├── CJ.Plug.StationAgent/                # 命令行执行器
│   │   └── ToolAgents/DefaultCmdExecute.cs  # cmd.exe 启动 + stdout 捕获
│   ├── CJ.Plug.StationApiClients/           # Station HTTP 客户端库
│   ├── CJ.Plug.StationSetup/                # 工作站安装器 (WPF)
│   └── StationSettingUI/                    # 工作站设置界面
│
├── PlugLibrary/                             # 插件库（20+）
│   ├── PlugsBundle/                         # 插件注册中心 (ServiceCollectionExtensions)
│   │   ├── ServiceCollectionExtensions.cs   #   AddPlugsBundle() / AddPlugsExecutebundle()
│   │   └── SystemInitTools/                 #   系统内置工具定义 (WordToPdf等)
│   ├── NXPlug/                              # NX CAD (含 Capability)
│   ├── PythonPlug/                          # Python (含 Capability)
│   ├── CMDPlug/                             # Shell (含 Capability)
│   ├── RESTPlug/                            # HTTP REST (含 Capability)
│   ├── AIAgentPlug/                         # AI Agent (含 Capability, 设置页)
│   ├── WordPlug/ ExcelPlug/ PPtPlug/        # Office 文档处理
│   ├── MATLABPlug/ JavaPlug/ CSharpPlug/    # 编程语言插件
│   ├── Loop/                                # 循环控制 (For/While/If/And)
│   ├── DllLoader/ StlViewer/ Pause/ Patran/ # 其他专用插件
│   └── ...
│
├── PlugToolIntegrations/                    # 独立工具可执行程序
│   ├── NXGetParameters/ NXUpdateParameters/ NXRunJournal/ NXToStl/ NXUtils/
│   ├── WordToPdf/ WordConvertToPdf/ WordInsertData/ WordIntegration_Aspose/
│   ├── PythonSDK/                           # Python SDK (pip 包)
│   ├── DotnetCoreBridgeToDotnetFramework/   # Roslyn C# 桥接
│   └── ToolMng/                             # ★ 工具代理服务 (SignalR + HTTP API + 文件管理)
│
└── modules/                                 # 业务模块（27个，各含 Api/ApiClient/UI）
    ├── MCPToolsModule/                      # ★ MCP 工具管理模块
    │   ├── CJ.Plug.MCPToolsManageApi/       #   API：CRUD + PublishTool + GetPublishedWorkflows
    │   ├── CJ.Plug.MCPToolApiClient/        #   HTTP 客户端
    │   └── CJ.Plug.MCPToolsManage/          #   Blazor UI：MCPTool管理页面
    ├── AIModule/                            # ★ AI 模块
    │   ├── CJ.Plug.AI/                      #   模块注册、AskAI 入口
    │   ├── CJ.Plug.AIChat/                  #   Blazor 聊天 UI + RAG
    │   └── CJ.Plug.DeepSeekIn/              #   LLM 服务 (Ollama + OpenRouter, 支持 MCP Tools)
    ├── ProcessManageModule/                 # ★ 流程管理 + AI Workflow Builder
    │   └── CJ.Plug.ProcessManageApi/
    │       └── Services/
    │           ├── AiWorkflowBuilderService.cs  # 自然语言 → 工作流 JSON
    │           └── WorkflowTranslator.cs        # 工作流 JSON → Process 实体
    ├── PlugExecuteModule/                   # 插件执行引擎
    │   └── CJ.Plug.ExecuteApi/Services/
    │       ├── PlugExecutionEngine.cs           # 主执行分发器
    │       ├── PlugExecutionEngine.McpPath.cs   # MCP 类型执行路径
    │       ├── PlugExecutionEngine.FlowSource.cs# 流程源路由
    │       └── McpWorkflowRunner.cs             # MCP 工作流运行器
    ├── ProcessEditModule/                   # 流程编辑器 UI
    ├── KnowledgeManageModule/               # 知识库管理
    ├── LlmConfigModule/                     # LLM 配置管理
    ├── ModelManageModule/                   # AI 模型管理
    ├── StationAndToolModule/                # 工作站 & 工具管理
    ├── PlugMarketModule/                    # 插件市场
    ├── PDZManageModule/                     # 数据空间 (PDZ) 管理
    ├── JobManageModule/                     # 作业管理
    ├── ToolActionSettingModule/             # 工具动作设置
    ├── FileManageModule/                    # 文件管理
    ├── GuacamoleModule/                     # 远程桌面 (Guacamole/noVNC)
    ├── FMDModule/                           # 文件元数据管理
    ├── LoginModule/ AuthModule/             # 登录 & 认证
    ├── UserManageModule/                    # 用户管理
    ├── AuditModule/                         # 审计日志
    ├── HomeModule/                          # 首页/仪表盘
    ├── SkillsModule/ SkillsManageModule/    # 技能管理
    ├── RelationManageModule/                # 关系管理
    ├── ProcessToExternalModule/             # 流程外部导出
    └── UnitTestModule/                      # 单元测试工具
```

---

## 插件系统核心接口

### IPlugCommonExecute — 执行入口

```csharp
// src/Core/CJ.Plug.PlugBaseCore/Contracts/IPlugCommonExecute.cs
public interface IPlugCommonExecute
{
    bool IsThisPlugTypeKey(string? PlugTypeKey);                // 匹配策略
    Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context);  // 执行逻辑
}
```

### IPlugCommonSettingContent — 设置 UI

```csharp
// src/Core/CJ.Plug.PlugBaseCore/Contracts/IPlugCommonSettingContent.cs
public interface IPlugCommonSettingContent
{
    Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context);  // Blazor 设置页
    Task<PlugSettings?> GetPlugBaseSetting();       // 默认元数据（ToolPath, CommandLineShema等）
    Task<List<Plug>?> GetDefaultChildPlugs();       // 可选子插件
}
```

### IPluginCapability — AI 能力声明（★ MCP 相关）

```csharp
// src/Core/CJ.Plug.Models/MCPTools/IPluginCapability.cs
public interface IPluginCapability
{
    string PluginTypeKey { get; }              // 匹配插件的 IsThisPlugTypeKey()
    string Name { get; }                       // AI 可读名称
    string Description { get; }                // 注入 LLM System Prompt
    List<CapabilityParameter> Inputs { get; }  // 输入参数定义
    List<CapabilityParameter> Outputs { get; } // 输出参数定义
    string[] Tags { get; }                     // 语义标签
}
```

### 插件分类体系

```csharp
// 类别 (PlugCategorys)
public enum PlugCategorys { 桌面类, 桌面类动作, 接口类, 接口类动作, 脚本类, 脚本类动作, 数据库类, 容器类, 流程, 流程控制组件, 循环控制组件 }
// 分组 (PlugGroupEnum)
public enum PlugGroupEnum { 工具集成, 接口集成, 脚本执行, 数据库, 流程控制, 我的流程 }
```

三种大类对应三种内置 Fallback Handler：

| 类别 | 说明 | 典型插件 |
|------|------|---------|
| **桌面类** | 调用外部 exe 工具 | NX CAD, WordToPdf, Python |
| **接口类** | 调用 HTTP API | RESTPlug |
| **脚本类** | 执行脚本文本 | Python, CSharp, Java, JavaScript |

---

## AI 能力全景

### AI Workflow Builder（自然语言 → 工作流）

```
用户输入: "读取NX模型的参数，如果壁厚<2mm就把壁厚改为2mm，然后导出STL"
     │
     ▼
AiWorkflowBuilderService.GenerateAsync()
  ├─ CapabilityRegistry.ToPromptContext() → 注入所有已注册插件能力到 System Prompt
  ├─ DeepSeekService.Ask() → 调用 LLM (本地 Ollama 或 OpenRouter)
  ├─ ParseWorkflowResponse() → 提取 JSON
  └─ Validate() → 验证每个 Activity 的 Plugin 在 Registry 中存在
     │
     ▼
WorkflowTranslator.Translate()
  ├─ 创建 Process 实体 + PlugVariable 列表
  └─ 序列化为 Elsa ActivityJsonData (CommonCorePlugActivity 节点)
     │
     ▼
保存在数据库 → 在 Elsa 设计器中可视化编辑
```

### AI Agent 插件

`AIAgentPlug` 可在工作流中作为节点插入 AI 调用：
- 支持 Ollama 本地模型 (qwen3, deepseek-r1, llama3.2)
- 支持 OpenRouter 云端 API
- 支持动态输入参数 `[paramName]` 模板语法
- 输出绑定到下游插件变量

### DeepSeekService 集成

位置: `src/modules/AIModule/CJ.Plug.DeepSeekIn/`
- 统一封装 Ollama 和 OpenRouter
- 支持流式/非流式、MCP Tools 注入
- `OllamaSharp.ModelContextProtocol` 桥接

---

## 关键设计模式

1. **插件策略模式** — `IPlugCommonExecute.IsThisPlugTypeKey()` 做策略匹配，`PlugExecuteHandlerService` 从 DI 容器收集所有 Handler
2. **三级 Fallback 路由** — PlugTypeKey 精确匹配 → Category 大类匹配 → Root Plug 回溯匹配
3. **模块三层架构** — 每个模块分 Api（服务层）/ ApiClient（客户端封装）/ UI（Blazor组件，可选）
4. **API 客户端 Partial Class 聚合** — `MainApiClient` 通过 partial class 聚合各模块的 HTTP 调用
5. **SignalR 实时通知链** — MCP Tool 发布 → HTTP → DispatchServer → SignalR Broadcast → McpServer 实时刷新
6. **Capability 驱动 AI 编排** — `IPluginCapability` → `CapabilityRegistry` → LLM Prompt → 自动生成工作流
7. **PDZ 变量模板** — Use0 PDZ 作为 MCP Tool 的参数模板，自动生成 JSON Schema

---

## 配置文件位置

| 配置项 | 路径 |
|--------|------|
| 解决方案文件 | `CJ.Plug-Aspire.sln` |
| Aspire 编排配置 | `src/Framework/CJ.Plug.AspireHost.AppHost/appsettings.json` |
| 用户插件配置 | `PlugConfig/UserPlugs.xml` |
| NuGet 中央版本 | `Directory.Packages.props` |
| MCP 包版本 | `ModelContextProtocol` v0.3.0, `ModelContextProtocol.AspNetCore` v1.4.0 |
| 各服务配置 | 各服务项目的 `appsettings.json` |
| MCP Server 入口 | `src/PlugApiServer/CJ.Plug.McpServer/Program.cs` |
