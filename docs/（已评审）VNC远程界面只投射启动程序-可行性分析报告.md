# （待评审）VNC 远程界面只投射启动程序 —— 可行性分析报告

> 项目：CJPlug（寸金插座平台）
> 日期：2026-08-07
> 状态：待评审（本文档仅作现状分析与可行性论证，未涉及编码实施）
> 涉及范围：`GuacamoleModule`、`CJ.Plug.StationApiServer`、`CJ.Plug.PlugBaseCore`、`CJ.Plug.Execute` 等

---

## 1. 背景与目标

### 1.1 现状痛点

当前 CJPlug 的"远程界面"功能本质是 **VNC 整桌面投射**：工具在图站（远端 Windows/Linux 机）上启动后，用户通过浏览器看到的是 **图站整个桌面**（任务栏、其他窗口、桌面图标全部可见），而非仅该工具程序本身。

### 1.2 目标

> 能否只投射**本次启动的程序**（其窗口/弹窗），而不是整个桌面？

隐含需求：
- **画面**：只显示目标程序相关内容（主窗口 + 其弹窗/子窗口），屏蔽图站其他内容；
- **交互**：保留现有 VNC 的"可操作"体验（鼠标/键盘），不是只读监控；
- **链路**：与现有"工具执行 → 自动打开远程界面"流程衔接，尽量复用现有通道。

---

## 2. 现状梳理（代码级）

当前远程界面存在 **三条并存路径**，均落在 `GuacamoleModule` + 图站 `StationApiServer`：

### 2.1 路径一：整桌面交互 VNC（当前主链路）

| 环节 | 位置 | 说明 |
|---|---|---|
| 图站 VNC 服务 | `src\PlugStation\CJ.Plug.StationApiServer\Services\UltraVncService.cs` | 部署/启动 `uvnc-portable\winvnc.exe`（UltraVNC portable），监听 **5900**，`-autoreconnect -run`，Loopback 仅本机访问 |
| VNC 自启动 | `...\Services\VncAutoStartService.cs` | StationApiServer 启动时自动部署并拉起 UltraVNC（`RemoteDesktop:AutoStartVnc`，默认 true） |
| 状态/启停 | `...\Services\RemoteDesktopService.cs` | VNC 委托 UltraVNC；Linux 降级 vncserver/x11vnc |
| 图站 API | `...\Apis\RemoteDesktopApi.cs` | `POST /api/station/remote/vnc/start`、`/stop`、`/status`、`/ssh/*`、`/uvnc/*` |
| 服务端 WS 代理 | `src\modules\GuacamoleModule\CJ.Plug.GuacamoleApi\Services\VncWebSocketProxy.cs` | 浏览器 WebSocket ↔ 图站 TCP 5900 双向透传 |
| 服务端 API | `...\Apis\RemoteDesktopApi.cs` | `WS /api/remote/vnc?host={ip}&port=5900` |
| 前端查看器 | `src\modules\GuacamoleModule\CJ.Plug.GuacamoleUI\Pages\VncViewer.razor`（`/remote/vnc/{ip}`） | noVNC（`RFB`）+ `noVncInterop.js`，完整交互（键盘/鼠标/剪贴板/缩放） |
| 自动打开入口 | `src\modules\PlugExecuteModule\CJ.Plug.Execute\Pages\PlugExecuteStandalone.razor`(L366) | 工具执行时 `window.open("/remote/{ip}/{protocol}")` |
| 执行通知 | `src\Core\CJ.Plug.PlugBaseCore\Services\ToolExecuteService.cs`(L322-342) | 工具执行前，若插头 `SupportRemoteView=true` → `StatusReporter.ReportStationExecuting(plugDefId, stationIp, pdzId, protocol="vnc")` → Serilog `CLog` → `SignalRLogSink` → `DispatchServer\Hub\MainHub.StationExecuting` → 前端 |

**特征**：整桌面、可交互、开箱即用。**缺陷**：图站全部内容暴露。

### 2.2 路径二：单窗口只读捕获（已有雏形，但只读）

| 环节 | 位置 | 说明 |
|---|---|---|
| 窗口枚举/捕获 | `src\PlugStation\CJ.Plug.StationApiServer\Services\WindowCaptureService.cs` | Win32 `PrintWindow`(PW_RENDERFULLCONTENT) 抓**指定进程主窗口** → JPEG 帧 → WebSocket 推送；支持 fps 控制、pause/resume；`GetCapturableWindows()` 列可见窗口 |
| 图站 API | `...\Apis\RemoteDesktopApi.cs` | `GET /api/station/remote/capture/windows`、`WS /api/station/remote/capture?processName={name}&fps={n}` |
| 服务端 WS 代理 | `src\modules\GuacamoleModule\CJ.Plug.GuacamoleApi\Services\CaptureWebSocketProxy.cs` | 浏览器 WS ↔ 图站 WS 透传（默认图站端口 7660） |
| 服务端 API | `...\Apis\RemoteDesktopApi.cs` | `WS /api/remote/capture?host={ip}&processName={name}&fps={n}`、`GET /api/remote/capture/windows/{stationIp}` |
| 前端查看器 | `...\Pages\CaptureViewer.razor`（`/remote/capture/{ip}`） | `captureInterop.js` 把 JPEG 帧逐帧刷到 `<img>`；下拉选择窗口、fps 调节 |

**特征**：已实现"**只投射单个程序**"的画面！**缺陷**：纯观看——WS 客户端消息只支持 `pause/resume/fps`，**无鼠标/键盘输入回传**，不能操作程序。

### 2.3 路径三：Apache Guacamole 集成（独立备用）

- `GuacamoleApi\Services\GuacamoleService.cs`：REST 对接外部 Apache Guacamole 服务器（连接 CRUD、嵌入 token）；
- `GuacamoleUI\Pages\GuacamoleViewer.razor`（`/guacamole/{ip}`）：iframe 嵌入。
- 未接入工具执行主链路；Guacamole 本身也只透传 VNC/RDP，**不提供单窗口能力**。

### 2.4 现状小结

| 维度 | 路径一（VNC 整桌面） | 路径二（Capture 单窗口） |
|---|---|---|
| 画面范围 | 整个桌面 | ✅ 仅目标程序窗口 |
| 交互（鼠标/键盘） | ✅ | ❌ 只读 |
| 自动衔接工具执行 | ✅ | ❌ 需手动进页面选窗口 |
| 前端依赖 | noVNC | 自研 `<img>` 帧流 |

**结论：两条路径恰好互补——"整桌面+交互"已有，"单窗口"只差交互化。目标需求 = 把路径二的画面能力 + 路径一的交互能力合并。**

---

## 3. 方案论证

### 3.1 方案 A（推荐 P1）：现有 Capture 链路升级为"准交互单窗口"

在 `WindowCaptureService` + `CaptureViewer` 基础上补齐输入回传：

1. **图站侧**：`HandleClientMessage` 增加 `mouse`（坐标/按键/滚轮）与 `key`（键码/按下抬起）消息 → P/Invoke `SetForegroundWindow` + `SendInput` 注入（或 `PostMessage` 后台注入，见 3.3）；
2. **前端侧**：`CaptureViewer.razor` 画面改为 canvas/overlay 事件层，捕获鼠标移动/点击/滚轮/键盘事件 → 经现有 WS 通道回传；
3. **坐标映射**：浏览器内坐标 → 窗口客户区坐标（× 捕获缩放比）→ 屏幕坐标；
4. **自动衔接**：`ToolExecuteService` 的 `StationExecuting` 通知增加进程名参数（或由执行入口直接 `window.open("/remote/capture/{ip}?processName={name}")`）。

- **优点**：改动集中（图站 1 服务 + 前端 1 页面 1 JS + 通知参数），零新增依赖，复用全部已有通道；
- **缺点**：JPEG 全帧重传（5~15fps），延迟/带宽/CPU 高于 VNC 增量帧；`PrintWindow` 不渲染 owned 子窗口与弹窗（见 3.3 难点一，需多窗口合成增强）；无剪贴板/文件传输。

### 3.2 方案 B（P2，体验最优）：图站实现"RFB 单窗口 VNC 服务端"

图站侧自实现一个 **RFB 3.8 协议服务端**（单窗口模式）：

- 帧源复用 `WindowCaptureService.CaptureFrame`（PrintWindow 抓取目标窗口）；
- 实现 RFB 握手/安全类型（None）/编码协商（Tight/JPEG 全帧或差量）/FramebufferUpdate；
- RFB 输入事件（PointerEvent/KeyEvent）→ `SendInput` 注入；
- 监听独立端口（如 5901），复用现有 `VncWebSocketProxy`（WS→TCP 可指端口）与前端 `VncViewer`（noVNC）——**前端几乎零改动**，交互体验与现 VNC 完全一致（键盘/剪贴板/缩放/多客户端）。

- **优点**：完整 VNC 体验；入口与现状统一；协议成熟，noVNC 客户端免开发；
- **缺点**：RFB 服务端需自行实现（握手、编码、差量帧，估算 1~2 周开发+联调）；同样受 3.3 难点一（子窗口/弹窗）限制。

### 3.3 无论选 A/B 都必须解决的技术难点

| # | 难点 | 说明 | 对策 |
|---|---|---|---|
| 1 | **子窗口/弹窗捕获** | `PrintWindow(PW_RENDERFULLCONTENT)` 只渲染窗口本体；模态对话框、右键菜单、下拉框等 **owned windows** 不出现（CAD 类程序弹窗极多） | "进程级多窗口合成"：枚举该进程全部可见顶层窗口（含 owned），合并包围盒，逐窗口 `PrintWindow` 后按相对坐标拼合成一帧；或改走 `Windows.Graphics.Capture`（WinRT 按窗口 API，但服务上下文/多窗口同样需自行处理） |
| 2 | **输入注入目标与坐标** | `SendInput` 是全局注入，需先 `SetForegroundWindow` 激活目标窗口；坐标需"捕获画面缩放比 × 窗口屏幕位置"换算；DPIAware 需声明 | 捕获会话内维护窗口句柄+屏幕矩形，前端上报窗口尺寸做缩放映射；DPI 感知进程（`SetProcessDpiAwarenessContext`） |
| 3 | **DirectX/硬件加速窗口** | `PrintWindow` 对部分硬件加速内容可能黑屏 | `PW_RENDERFULLCONTENT` 对多数 Win32/WPF 应用有效，失败降级 `BitBlt`（现有代码已有降级）；个别应用（全屏游戏/视频播放）黑屏属已知边界 |
| 4 | **进程/窗口识别** | "本次启动的程序"是谁？工具表目前无进程名字段 | 推荐：图站执行器（`ExecuteApp`/工具包装器）启动成功后**上报实际 PID** → 按 PID 定位主窗口（最可靠）；辅助：工具配置加"远程查看进程名"字段；兜底：从执行命令解析首个 exe 名 |
| 5 | **通知链路改造** | `StationExecuting(PDZId, PlugDefinitionId, StationIp, Protocol)` 无进程维度 | 增加 `processName`/`windowTitle` 参数（`StatusReporter` → `SignalRLogSink` → `MainHub` → 前端订阅方；同时 `PlugExecuteStandalone.razor` 等打开入口同步携带） |
| 6 | **性能** | JPEG 全帧 5~15fps 约占 1~3 Mbps/1080p；多客户端并发需节流 | fps 上限 30 + 会话级节流；方案 B 的差量帧可显著降低 |

### 3.4 其他外部方案（评估后不推荐）

| 方案 | 结论 | 原因 |
|---|---|---|
| RDP RemoteApp | ✗ | 仅 Windows Server 托管，图站多为 Win10/11 Pro，不可用 |
| Parsec 按窗口共享 | ✗ | 闭源商业组件，授权/内网部署/自研集成成本高 |
| 独立会话/虚拟机跑程序 | ✗ | 与现有"图站当前桌面执行工具"架构冲突，资源开销大 |
| Apache Guacamole | ✗ | 仅协议透传，无单窗口能力 |
| TightVNC/RealVNC 等 | ✗ | 均无按窗口投射能力 |

---

## 4. 推荐路线

```
P1（快速见效，约 1~2 周）  方案 A：Capture 交互化（单窗口 + 准交互）
        │  └─ 输入回传（SendInput 注入）+ 坐标映射 + 多窗口合成（弹窗）+ 通知链路带进程名
        ▼
P2（按需，约 1~2 周）      方案 B：图站 RFB 单窗口 VNC 服务端（完整交互，noVNC 复用）
        │  └─ 前端零改动，入口统一 /remote/vnc/{ip}?process=xxx
        ▼
    两条路径可共存：VNC 整桌面模式保留为"降级/诊断"选项，工具插头设置
    SupportRemoteView 之上可增加 RemoteViewMode = fullscreen | window
```

P1 与 P2 的关键代码改造点（供后续子方案设计参考）：

- 图站：`WindowCaptureService.cs`（输入回传、多窗口合成）、`RemoteDesktopApi.cs`（参数扩展）、执行器 PID 上报；
- 服务端：`CaptureWebSocketProxy.cs`（透传类型扩展）、`GuacamoleApi\Apis\RemoteDesktopApi.cs`（新增端点/参数）；
- 前端：`CaptureViewer.razor` + `captureInterop.js`（事件层与坐标映射）、`VncViewer.razor`（P2 进程参数）、`PlugExecuteStandalone.razor`（打开入口携带进程名）；
- 链路：`StatusReporter.cs` / `SignalRLogSink.cs` / `MainHub.cs`（`StationExecuting` 增加进程名参数，向后兼容默认空）。

---

## 5. 风险与边界

1. **弹窗/子窗口**是最大不确定点：多窗口合成能覆盖常规弹窗，但全屏渲染、跨进程窗口（如文件对话框由 explorer.exe 提供）不在进程内，无法合成 → 文件对话框这类场景画面会缺；边界内可接受，文档需明示。
2. **交互完整性**：方案 A 为"准交互"（依赖前台激活，用户在图站本机操作时会被抢占焦点）；方案 B 与现 VNC 体验一致。
3. **安全**：单窗口模式天然降低暴露面（不显示桌面/任务栏/其他程序），是对现状的改进；但输入注入通道需沿用现有鉴权（WS 端点当前无显式鉴权，属既有问题，不在本报告范围）。
4. **兼容**：现有整桌面 VNC 保留，改动向后兼容（新增参数默认空 = 原行为）。

---

## 6. 待拍板决策点

> 按"一次一问"原则逐个确认，全部拍板后才进入子方案设计。

### 决策记录

| # | 决策点 | 拍板结果 | 日期 |
|---|---|---|---|
| D1 | 目标形态：完整交互（方案 B RFB 单窗口 VNC）还是准交互（方案 A Capture 交互化） | **方案 B：图站自实现 RFB 单窗口 VNC 服务端，交互与现 VNC 完全一致（键盘级完整），前端复用 noVNC，零改动** | 2026-08-07 |
| D2 | 进程识别方式：执行器 PID 上报 / 工具配置进程名 / 命令解析 | **主 A（图站执行器启动工具后上报实际 PID，按 PID 定位窗口）+ 辅 B（工具配置"远程查看进程名"字段兜底）；命令解析（C）不作为识别手段** | 2026-08-07 |
| D3 | 实施分期：P1（Capture 交互化）+ P2（RFB 单窗口 VNC）分两期，还是直接只做 P2 | **直接只做 P2：图站 RFB 单窗口 VNC 服务端一步到位（含多窗口合成、PID 上报、通知链路改造），不做 P1 准交互过渡** | 2026-08-07 |
| D4 | 弹窗/子窗口合成投入程度：L1 只主窗口 / L2 进程级多窗口合成 / L3 跨进程窗口合成 | **L3：进程级多窗口合成 + 文件对话框等外部进程（explorer.exe 等）窗口纳入捕获，RFB 编码前完成合成** | 2026-08-07 |

### 待确认项（已全部拍板）

- [x] **D1**：目标形态确认 —— 选 **方案 B**（RFB 单窗口 VNC 服务端，完整交互）。方案 A（Capture 准交互）降级为备选/兜底，不再作为主路线。
- [x] **D2**：进程识别 —— **主 A（执行器 PID 上报）+ 辅 B（工具配置进程名兜底）**。
- [x] **D3**：实施分期 —— **直接只做 P2**，不做 P1 准交互过渡（Capture 交互化方案 A 降级为永久备选）。
- [x] **D4**：窗口合成档位 —— **L3 跨进程窗口合成**（含文件对话框等外部进程窗口，RFB 编码前合成）。
