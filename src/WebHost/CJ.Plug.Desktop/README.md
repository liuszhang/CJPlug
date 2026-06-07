# CJ.Plug.Desktop

CJ.Plug 桌面启动器 —— 基于 WPF + WebView2 的 Windows 桌面 Shell 程序，内嵌 CJ.Plug Aspire Dashboard 及管理页面，提供原生窗口体验。

## 技术栈

| 技术 | 说明 |
|---|---|
| .NET 10 | 目标框架 `net10.0-windows` |
| WPF | 桌面窗口框架 |
| WebView2 | 内嵌 Chromium 浏览器控件 |
| CommunityToolkit.Mvvm | MVVM 工具包 |
| MaterialDesignThemes | UI 主题 |
| Serilog | 结构化日志 |
| Microsoft.Extensions.Hosting | 通用主机 / DI 容器 |
| System.Windows.Forms | 系统托盘 NotifyIcon |

## 运行方式

本程序依赖 AppHost 提供后端 Dashboard，启动前需确保 AppHost 已发布：

### 1. 发布 AppHost

```bash
cd D:\Pro\CJ.Plug-Aspire
dotnet publish src/Framework/CJ.Plug.AspireHost.AppHost -c Debug -o 02.Publish/CJ.Plug.AspireHost.AppHost/Debug/net10.0
```

### 2. 启动 Desktop Shell

```bash
cd src/WebHost/CJ.Plug.Desktop
dotnet run
```

程序启动后会自动拉起 AppHost 进程并轮询 `http://localhost:15288` 直到 Dashboard 就绪（超时 30 秒），随后显示主窗口。

### 3. 发布单文件（可选）

```bash
dotnet publish src/WebHost/CJ.Plug.Desktop -c Release -o publish/desktop
```

## 菜单功能

主窗口左侧导航菜单提供以下入口：

| 菜单 | 地址 | 说明 |
|---|---|---|
| 插头管理 | `/TAS` | 插头配置与管理 |
| 流程管理 | `/ProcessManageList` | 流程编排与监控 |
| MCP Tool管理 | `/MCPToolManage` | MCP 工具注册与配置 |
| 服务管理 | `/` | Aspire Dashboard 总览 |

## 窗口特性

- **窗口状态记忆**：关闭时自动保存窗口位置和大小到 `window-state.json`，下次启动恢复。
- **系统托盘**：关闭窗口时最小化到系统托盘，右键菜单可显示主窗口或退出程序；双击托盘图标恢复窗口。

## 依赖说明

- **WebView2 Runtime**：必须安装 [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)，Windows 11 已内置。
- **.NET 10 SDK**：编译和运行需要 .NET 10.0 SDK。

## 项目结构

```
CJ.Plug.Desktop/
├── App.xaml.cs          # 应用入口 + DI + 托盘图标
├── MainWindow.xaml      # 主窗口布局（菜单栏 + WebView2）
├── MainWindow.xaml.cs   # 窗口逻辑（状态记忆 / 托盘交互）
├── Views/
│   └── MenuBarView.xaml # 左侧导航菜单
├── ViewModels/
│   └── MainViewModel.cs # 主窗口数据绑定
├── Services/
│   └── AppHostLauncher.cs # AppHost 进程管理
└── CJ.Plug.Desktop.csproj
```
