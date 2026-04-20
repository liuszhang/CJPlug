# CJ.Plug-Aspire 项目架构文档

## 项目概述

这是一个基于 **.NET 10.0 + Microsoft Aspire** 的企业级**插件化工作流执行平台**，主要用于工业自动化场景，支持多种工具集成（NX CAD、Python、MATLAB、Word 等），提供可视化流程设计和工作流引擎驱动。

## 技术栈

- **框架**: .NET 10.0 + Microsoft Aspire（分布式编排）
- **前端**: Blazor + MudBlazor 组件库
- **工作流**: Elsa Workflows
- **ORM**: Entity Framework Core (SQLite/PostgreSQL)
- **实时通信**: SignalR
- **日志**: Serilog

## 核心架构层次

| 层级 | 目录 | 说明 |
|------|------|------|
| **核心层** | `src/Core/` | 模型定义、插件基类、API客户端、共享组件 |
| **服务层** | `src/PlugApiServer/` | API服务、调度服务、工作流引擎、MCP服务 |
| **托管层** | `src/PlugWebHost/` | 一体化服务器、Elsa设计器、WASM托管 |
| **工作站层** | `src/PlugStation/` | 工作站代理，执行实际工具命令 |
| **插件库** | `src/PlugLibrary/` | 各种插件实现（NX、Python、Word等） |
| **业务模块** | `src/modules/` | 插件执行、流程管理、用户管理等模块 |

## 目录结构

```
src/
├── Core/                           # 核心基础层
│   ├── CJ.Plug.Models/             # 领域模型和接口定义
│   ├── CJ.Plug.PlugBaseCore/       # 插件执行核心
│   ├── CJ.Plug.ApiClient/          # API 客户端
│   ├── CJ.Plug.SharedPages/        # 共享 UI 组件
│   ├── CJ.Plug.VariableUIHandler/  # 变量 UI 处理
│   └── Integrations/               # 核心集成（Elsa, AMIS, WorkflowCore）
│
├── Framework/                      # 框架层
│   └── CJ.Plug.AspireHost.ServiceDefaults/  # Aspire 服务默认配置
│
├── PlugApiServer/                  # API 服务层
│   ├── CJ.Plug.ApiServer/          # 主 API 服务器
│   ├── CJ.Plug.DispatchServer/     # 调度服务器（SignalR Hub）
│   ├── CJ.Plug.ElsaApiServer/      # Elsa 工作流引擎服务器
│   └── CJ.Plug.McpServer/          # MCP 服务器
│
├── PlugWebHost/                    # Web 托管层
│   ├── CJ.Plug.AllInOneServer/     # 一体化服务器
│   ├── CJ.Plug.ElsaStudio/         # Elsa 设计器
│   ├── CJ.Plug.HostWasm/           # WebAssembly 托管
│   └── CJ.Plug.MainPageContent/    # 主页面内容
│
├── PlugStation/                    # 工作站代理层
│   ├── CJ.Plug.StationApiServer/   # 工作站 API 服务
│   ├── CJ.Plug.StationAgent/       # 工作站代理（命令行）
│   └── StationSettingUI/           # 工作站设置 UI
│
├── PlugLibrary/                    # 插件库
│   ├── PlugsBundle/                # 插件捆绑包
│   ├── NXPlug/                     # NX CAD 插件
│   ├── PythonPlug/                 # Python 执行插件
│   ├── CMDPlug/                    # 命令行插件
│   ├── WordPlug/                   # Word 文档插件
│   ├── ExcelPlug/                  # Excel 插件
│   ├── AIAgentPlug/                # AI 代理插件
│   ├── RESTPlug/                   # REST API 插件
│   ├── Loop/                       # 循环控制插件
│   └── ...                         # 其他插件
│
├── PlugToolIntegrations/           # 工具集成
│   ├── NXGetParameters/            # NX 参数获取
│   ├── NXToStl/                    # NX 转 STL
│   ├── WordToPdf/                  # Word 转 PDF
│   └── ...                         # 其他工具
│
└── modules/                        # 功能模块
    ├── PlugExecuteModule/          # 插件执行模块
    ├── ProcessManageModule/        # 流程管理模块
    ├── PDZManageModule/            # 数据空间管理模块
    ├── PlugMarketModule/           # 插件市场模块
    ├── JobManageModule/            # 作业管理模块
    ├── ToolActionSettingModule/    # 工具动作设置模块
    ├── LoginModule/                # 登录模块
    ├── UserManageModule/           # 用户管理模块
    └── ...                         # 其他模块
```

## 核心功能

### 1. 插件执行系统

核心接口: `src/Core/CJ.Plug.PlugBaseCore/Contracts/IPlugCommonExecute.cs`

```csharp
public interface IPlugCommonExecute
{
    bool IsThisPlugTypeKey(string? PlugTypeKey);
    Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context);
}
```

基类实现: `BasePlugExecuteService` 提供执行结果上报、错误报告、完成报告等方法。

### 2. 调度服务系统

- `MainHub` - SignalR Hub，处理工作站状态同步、日志广播、执行状态通知
- `StationService` - 工作站选择和负载分发

### 3. 数据空间管理 (PDZ)

PlugDataZone（插件数据空间）是存储插件执行数据、变量、流程图数据的核心数据结构。

### 4. Elsa 工作流集成

- `ElsaEngineService` - 工作流引擎服务
- `ElsaApiClient` - Elsa API 客户端封装
- `CommonCorePlugActivity` - 自定义活动基类

### 5. 模块化架构

每个业务模块采用三层架构:
- **Models 层** - 数据模型定义
- **Api 层** - API 端点和服务实现
- **ApiClient 层** - 客户端调用封装
- **UI 层** - Blazor 组件（可选）

## 支持的插件类型

- NX CAD（三维建模）
- Python 脚本执行
- MATLAB 计算
- Word/Excel 文档处理
- CMD 命令行
- REST API 调用
- AI 代理
- 循环控制

## 执行流程

```
用户请求 → API Server → 调度服务选择工作站 → 插件执行器 → StationAgent执行工具 → 结果回报 → 触发后续流程
```

详细步骤:
1. 用户发起执行请求
2. API Server 接收 PlugExecutionRequest
3. PlugExecuteService.StartExecutePlug() 获取插件数据和 PDZ 数据
4. 具体插件执行器执行 PlugCommonExecute()
5. StationAgent (工作站代理) 执行实际工具
6. 执行结果通过 SignalR 回报
7. ReportExecuteResult 处理后续动作，触发下一个动作或通知 Elsa 引擎

## 关键设计模式

1. **插件模式** - 通过接口定义插件契约，策略模式路由执行
2. **模块化架构** - 功能模块独立封装，统一初始化
3. **API 客户端聚合** - Partial Class 聚合各模块 API 客户端
4. **实时通信** - SignalR Hub 实现状态同步和执行通知

## 配置文件位置

| 配置项 | 路径 |
|--------|------|
| 解决方案文件 | `CJ.Plug-Aspire.sln` |
| 用户插件配置 | `PlugConfig/UserPlugs.xml` |
| 各服务配置 | 各服务项目的 `appsettings.json` |
