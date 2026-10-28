# （待评审）子方案1-单窗口RFB VNC投射-设计文档

> 项目：CJPlug（寸金插座平台）
> 日期：2026-08-07
> 状态：待评审
> 前置文档：（已评审）VNC远程界面只投射启动程序-可行性分析报告.md（D1~D4 已拍板）
> 范围：图站 `StationApiServer` / `StationAgent`、服务端 `GuacamoleApi` 模块、模型 `Tool/Station`、执行链路 `ToolExecuteService`、前端 `GuacamoleUI`

---

## 1. 目标

图站执行工具时，浏览器远程界面**只投射该工具程序的窗口**（含弹窗/子窗口/文件对话框），交互与现有 VNC 完全一致（noVNC 客户端复用，零前端协议改动）。

## 2. 已拍板决策（前置文档）

| # | 决策 | 内容 |
|---|---|---|
| D1 | 技术路线 | 图站自实现 **RFB 3.8 单窗口 VNC 服务端**（独立端口），前端复用 noVNC |
| D2 | 进程识别 | **主 A**：执行器（StationAgent）启动工具后上报实际 PID；**辅 B**：工具配置进程名兜底 |
| D3 | 实施范围 | 直接只做 P2，不做 Capture 准交互过渡 |
| D4 | 窗口合成 | **L3 跨进程窗口合成**（目标进程全部窗口 + 文件对话框等外部进程窗口），RFB 编码前合成 |

## 3. 现状关键事实（代码级）

工具进程实际启动链路（图站侧）：

```
主服务 ToolExecuteService ──► StationApiServer
    DefaultStationExecuteService.ExecuteRequestCommand
      └─► ExecuteActions.InvokeStationAgentAsync   // Process.Start(CJ.Plug.StationAgent.exe)，返回 StationAgent PID
            └─► StationAgent DefaultCmdExecute.ExecuteCMD
                  └─► Process.Start("cmd.exe", $"/c {command}")   // CreateNoWindow=true，GUI 子进程自行出窗
```

- 现有图站服务注册点：`StationApiServer\Program.cs`（`AddSingleton<UltraVncService/RemoteDesktopService/WindowCaptureService>`、`AddHostedService<VncAutoStartService>`、`app.MapRemoteDesktopApi()`）。
- 现有 WS→TCP 代理 `VncWebSocketProxy.HandleAsync(ws, host, port)` **支持任意端口** —— RFB 单窗口服务监听新端口后，服务端代理与前端 noVNC **零改动复用**。
- 现有 `WindowCaptureService` 的 Win32 P/Invoke（PrintWindow / EnumWindows / GetWindowRect / GetWindowThreadProcessId）与 JPEG 编码可直接提取复用。

## 4. 总体设计

```
浏览器 noVNC (VncViewer.razor 复用)
   │  ws://…/api/remote/vnc?host={图站IP}&port=5901      ← VncWebSocketProxy 复用（任意端口）
   ▼
服务端 GuacamoleApi（零改动）
   │  WS→TCP 5901 透传
   ▼
图站 StationApiServer
   ├─ RfbWindowVncServer（新，TCP 5901 监听，RFB 3.8 服务端）
   │    ├─ 帧源：WindowCompositor（新，L3 多窗口合成 → 位图 → JPEG/Tight 编码）
   │    └─ 输入：RFB PointerEvent/KeyEvent → InputInjector（新，SetForegroundWindow+SendInput）
   ├─ WindowBindRegistry（新：PID/进程名 → 目标会话绑定，支持热切换）
   │    ▲ 绑定接口 POST /api/station/remote/vnc-window/bind
   │    │   （由 StationAgent 启动工具后上报 PID；或主服务按辅B配置进程名调用）
   └─ StationTaskStore（现有：已有 ProcessId 字段，StationAgent PID 已落库）
```

**关键时序**（解决"PID 在工具启动后才可知"）：

```
1. 主服务 ToolExecuteService：SupportRemoteView && RemoteViewMode=window
   → StationExecuting(…, protocol="window", processName=辅B配置或空)   // 先行通知
2. 前端收到 → 打开 /remote/vnc/{ip}?port=5901（显示"等待程序窗口…"）
3. 图站执行：StationAgent 启动工具后上报真实 PID（或进程名）→ POST …/vnc-window/bind
4. RfbWindowVncServer 绑定目标 → 前端连接后立即出画面；未绑定前返回"等待"帧
5. 工具退出 → 绑定失效 → 前端提示"程序已退出"
```

## 5. 模块设计

### 5.1 图站：`RfbWindowVncServer`（新文件 `StationApiServer\Services\Rfb\RfbWindowVncServer.cs`）

- 职责：TCP 监听独立端口（配置 `RemoteDesktop:RfbWindowPort`，默认 **5901**），实现 RFB 3.8 服务端会话。
- 协议实现（最小可用集合）：
  - 握手：`RFB 003.008`、安全类型 `None`（鉴权走平台 WS 层/网络层，见 D6）；
  - `ClientInit`/`ServerInit`：帧缓冲宽高 = 目标窗口合成包围盒尺寸（跟随窗口变化，通过 `FramebufferUpdate` 前先发 `Resize` 或简化：固定首帧尺寸+周期探测）；
  - `SetEncodings`：支持 `Raw` + `Tight`（JPEG 子编码）——首版建议全帧 JPEG（见 D5）；
  - `FramebufferUpdate`：合成帧全量推送（首版）/ 差量矩形（增强项）；
  - 输入：`PointerEvent`（x,y,buttonMask）、`KeyEvent`（keysym,down）→ `InputInjector`。
- 生命周期：`BackgroundService`（同 `VncAutoStartService` 模式），`AddHostedService` 注册。
- 目标绑定：`WindowBindRegistry.SetTarget(binding)`；无目标时给客户端发"等待绑定"占位帧（黑底+提示文本可后置）。

### 5.2 图站：`WindowCompositor`（新 `Services\Rfb\WindowCompositor.cs`）

L3 合成管线（复用/抽取 `WindowCaptureService` 的 P/Invoke）：

```
BindTarget(PID | 进程名)
  → EnumerateTargetWindows()
       1) 进程树：目标 PID 及其子孙进程（launcher 场景）
       2) 全部可见顶层窗口（EnumWindows + IsWindowVisible + 有标题/可见尺寸）
       3) owned windows（GetWindow(GW_OWNER) 属于目标集合 → 一并纳入）
       4) 跨进程：前台模态链（前台窗口的 owner 属于目标集合 → 该窗口纳入，
          覆盖文件对话框 explorer.exe 场景）
  → MergeBounds()：所有窗口包围盒合并为合成画布（多窗口并排/层叠取外接矩形）
  → RenderFrame()：逐窗口 PrintWindow(PW_RENDERFULLCONTENT) → 按相对坐标 BitBlt 到合成位图
  → 输出：位图 → JPEG（复用现有 ImageFormat.Jpeg 逻辑）
```

- 增量优化（增强项）：窗口矩形变化检测，仅重绘变化窗口；全合成帧缓存 + 脏矩形。
- 窗口跟踪：会话内维护 `List<WindowEntry>{Hwnd, Title, ProcessId, Rect, Visible}`，每帧刷新比对。

### 5.3 图站：`InputInjector`（新 `Services\Rfb\InputInjector.cs`）

- 坐标映射：合成画布坐标 → 命中窗口 → 屏幕坐标（窗口 Rect 偏移 + 画布偏移）→ `SendInput`（`SetForegroundWindow` 先激活）。
- 键盘：`keysym → VK`（X11 keysym 到 Win32 虚拟键映射表，常用键 + 功能键），`SendInput` KEYEVENTF_KEYUP/KEYDOWN。
- 鼠标：按钮按下/抬起/滚轮 → `SendInput`（MOUSEEVENTF_*）；指针移动直接 `SendInput` 绝对坐标（或 `SetCursorPos`）。
- DPI：进程设置 `SetProcessDpiAwarenessContext`，坐标按真实像素换算。

### 5.4 图站：`WindowBindRegistry` + 绑定 API（新 `Services\Rfb\WindowBindRegistry.cs`、`Apis\RemoteDesktopApi.cs` 扩展）

```csharp
// 绑定请求（POST /api/station/remote/vnc-window/bind）
record WindowBindRequest {
    int?  ProcessId;        // 主A：StationAgent 上报的实际工具进程 PID
    string? ProcessName;    // 辅B：配置的进程名（PID 为空时按名定位）
    string? SessionKey;     // 可选：PDZ/任务关联键，便于多任务区分与解绑
}
```

- 幂等：重复绑定覆盖；工具退出（目标窗口全消失）→ 自动解绑并通知客户端。
- `Program.cs`：`AddSingleton<RfbWindowVncServer / WindowCompositor / InputInjector / WindowBindRegistry>()` + `AddHostedService<RfbWindowVncServer>()`；配置节 `RemoteDesktop:RfbWindowPort=5901`。

### 5.5 图站：`StationAgent` PID 上报（改 `CJ.Plug.StationAgent\ToolAgents\DefaultCmdExecute.cs`）

- `ExecuteCMD` 中 `Process.Start(cmd.exe …)` 后：
  - 启动时记录 cmd PID（现有逻辑已拿到）；
  - 启动后短轮询（如 0.5s×10）目标窗口出现：`EnumWindows` 匹配"cmd 进程树内首次出现的可见顶层窗口所属进程" → 取该进程 PID 作为**工具进程 PID**；
  - 经 `StationApiClient`（或直接 HTTP）调用 `POST {StationApi}/api/station/remote/vnc-window/bind` 上报 `{ProcessId, ProcessName=配置或解析}`；
  - cmd 退出但工具子进程存活（GUI 常见）→ 以已上报 PID 为准，不因 cmd 退出而解绑。
- 兜底：PID 上报失败/超时 → 主服务按辅 B（`Tool.RemoteViewProcessName`）调用绑定接口。

### 5.6 模型层

- `CJ.Plug.Models\Station\Tool.cs`：新增 `string? RemoteViewProcessName`（辅 B 配置，工具表字段，UI 工具配置页可选填）。
- `CJ.Plug.Models\Station\Station.cs`：新增 `string? RemoteViewMode = "fullscreen"`（`fullscreen`=现整桌面 VNC；`window`=单窗口投射），默认 fullscreen 保证向后兼容。
- `PlugExecutionRequest` 传递链：`ToolExecuteService → StationAgent` 需携带 `RemoteViewMode` 与 `RemoteViewProcessName`（检查现有 DTO 是否已有透传字段，无则补）。

### 5.7 执行链路与通知（主服务侧）

- `ToolExecuteService.cs`（L322-342 区域）：`SupportRemoteView=true` 时按 `RemoteViewMode` 分支：
  - `window` → `StatusReporter.ReportStationExecuting(…, protocol: "window", processName: Tool.RemoteViewProcessName)`；
  - `fullscreen` → 维持现行为（protocol="vnc"）。
- `StatusReporter.ReportStationExecuting` / `SignalRLogSink.SendStationExecuting` / `DispatchServer\Hub\MainHub.StationExecuting`：新增可选参数 `ProcessName`（默认空，向后兼容）；`StationExecutingData` 增加 `ProcessName`。
- `MainApiClient`（服务端→图站）：新增 `BindWindowAsync(stationIp, request)` 调用图站绑定接口（辅 B 路径用）。

### 5.8 前端（GuacamoleUI）

- `RemoteViewer.razor`：`protocol=="window"` → 重定向 `/remote/vnc/{StationIp}?port=5901&window=1`（沿用 VncViewer）。
- `VncViewer.razor`：读取 `window`/`port` 参数；`window=1` 时展示"等待程序窗口…"状态（连接建立后由 RFB 服务端首帧驱动消除，改动最小：仅加载文案 + 默认端口按参数）。
- `PlugExecuteStandalone.razor`（L366 附近）：`resolvedProtocol` 按图站 `RemoteViewMode` 计算（window → `vnc` + `?port=5901&window=1`）；`ProcessEditor.razor`/`ToolActionSettingPage.razor` 的远程测试入口不变（默认 fullscreen）。
- 订阅 `StationExecuting` 的现有逻辑（若已接）补 `ProcessName` 透传。

## 6. 配置项

| 配置 | 位置 | 默认 | 说明 |
|---|---|---|---|
| `RemoteDesktop:RfbWindowPort` | 图站 appsettings | 5901 | RFB 单窗口服务监听端口 |
| `RemoteDesktop:AutoStartRfbWindow` | 图站 appsettings | true | 随 StationApiServer 启动 |
| `Station:RemoteViewMode` | 图站/工具表 | fullscreen | window=单窗口投射 |
| `Tool:RemoteViewProcessName` | 工具表 | 空 | 辅 B 兜底进程名 |

## 7. 实施步骤（编码阶段划分）

> 每阶段结束统一构建（`dotnet build CJPlug/CJ.Plug-Aspire.sln` 相关子项目），阶段内不反复构建。

| 阶段 | 内容 | 涉及文件 |
|---|---|---|
| S1 | RFB 服务端骨架：TCP 5901 监听 + RFB 3.8 握手 + 全帧 JPEG 推送（先绑"任意单窗口"测试）+ 端口配置/自启动注册 | `RfbWindowVncServer.cs`、`Program.cs`、appsettings |
| S2 | 多窗口合成器 L3：窗口枚举（进程树+owned+跨进程模态链）+ 包围盒合成 + 逐窗 PrintWindow | `WindowCompositor.cs`（抽取复用 `WindowCaptureService` P/Invoke） |
| S3 | 输入注入：RFB Pointer/Key 事件 → SendInput + keysym 映射 + 坐标换算 + DPI | `InputInjector.cs` |
| S4 | PID 上报绑定：`WindowBindRegistry` + 绑定 API + `DefaultCmdExecute` 上报改造 + StationAgent 侧轮询 | `WindowBindRegistry.cs`、`RemoteDesktopApi.cs`、`DefaultCmdExecute.cs` |
| S5 | 链路与前端：模型字段（Tool/Station）、`ToolExecuteService` 分支、`StatusReporter/SignalRLogSink/MainHub` 参数、`RemoteViewer`/`VncViewer`/`PlugExecuteStandalone` | 见 5.6~5.8 |
| S6 | 联调验证 + 整桌面 VNC 回归 | — |

## 8. 验证计划

1. **RFB 协议**：浏览器 noVNC 连接 5901 成功显示合成画面（FramebufferUpdate 正常）；缩放/全屏（noVNC 内置）可用。
2. **多窗口合成（L3）**：对带模态框/右键菜单的程序（如记事本另存为、CAD 类）验证弹窗与文件对话框均出现在画面内；窗口移动/缩放后画面跟随。
3. **输入注入**：鼠标点击/拖拽/滚轮、键盘输入（含中文输入法状态、功能键）回传正确；坐标在"浏览器缩放画面"下命中正确位置。
4. **PID 上报时序**：工具经 StationAgent 启动 → 绑定成功 → 前端出画面；工具退出 → 前端提示退出；launcher（cmd 包装 exe 再拉子进程）场景窗口定位正确。
5. **辅 B 兜底**：配置进程名后，无 PID 上报也能绑定。
6. **回归**：`RemoteViewMode=fullscreen` 时整桌面 VNC（5900/UltraVNC）原流程不受影响；工具执行、结果上报链路无回归。
7. 多实例并发：两台浏览器同时看同一目标（RFB 多客户端）不崩溃。

### 8.1 联调实测记录（2026-08-08，自动测试）

**链路已 100% 打通**（C# 测试程序模拟 noVNC 0.6 完整交互，直连 5901 + WS 代理 5066 双路验证）：

| 验证项 | 结果 |
|---|---|
| RFB 握手（版本/安全类型/ClientInit/ServerInit） | ✅ ServerInit 1246x789（绑定记事本真实窗口，最小化窗口已过滤） |
| XResize 伪编码 | ✅ 0xFFFFFF21，新尺寸 1246x789 正确送达 |
| Tight JPEG 子编码 | ✅ 控制字节 0x90 + 变长长度 + JPEG（FFD8 头，28089 字节） |
| 帧数据内容 | ✅ 保存 JPEG 尺寸 1246x789，为记事本窗口真实画面 |
| WS 代理端到端 | ✅ `ws://127.0.0.1:5066/api/remote/vnc?host=127.0.0.1&port=5901` 全链路 JPEG 到达 |
| 差量编码 | ✅ 画面静止后无新帧（无脏矩形不推帧，符合预期） |

**联调中修复的根因**（均为协议级 bug）：
1. ServerInit 前误调 CaptureFrame 消费首帧 → 新增 RefreshSize 只算尺寸不渲染；
2. 最小化窗口 GetWindowRect 返回 -21333 伪坐标 → 画布 22959x22502（2GB 帧缓冲）→ 枚举过滤 IsIconic；
3. RFB 消息 body 长度错位：FBURequest 9 字节（非 10）、SetEncodings count 为 U16、ClientCutText 7 字节（非 4）；
4. XResize 伪编码常量 -309 应为 -223（0xFFFFFF21）；
5. Tight JPEG 格式：控制字节应为 0x90（子编码 9）+ 变长长度（7bit/字节+续位），原 0x30+3 字节大端+16 对齐与 noVNC 0.6 解码不符；
6. 帧循环在客户端断开后不退出（主机令牌）→ 补 IOException/ObjectDisposedException 分支；
7. 诊断日志刷屏 → 限频 5 秒 + 窗口列表只打一次。

**待浏览器端人工验收**：输入注入（点击/键盘）、多窗口合成视觉确认、PID 上报时序、fullscreen 回归（验证计划 1~7 中浏览器相关项）。

## 9. 风险与边界

- PrintWindow 对 DirectX/硬件加速窗口可能黑屏（沿用现有降级 BitBlt；全屏游戏类属已知边界）。
- 文件对话框识别依赖"前台模态链"启发式，个别程序弹窗归属可能误判（S2 验收重点）。
- `cmd /c` launcher 场景下进程树定位存在延迟（S4 轮询窗口出现），首帧出画可能慢 1~3 秒，可接受。
- 5901 鉴权：RFB 安全类型 None，依赖网络层/WS 层防护（见 D6）。

## 10. 待拍板决策点

> 一次一问，全部拍板后文档定稿（状态 → 已评审），再进入编码。

- [x] **D5**：RFB 编码策略 —— **首版直接做差量矩形（Tight 增量编码，脏矩形 + JPEG 子编码）**，不做全帧 JPEG 过渡。
- [x] **D6**：5901 端口访问控制 —— **给服务端 WS 代理端点（`/api/remote/vnc` 等）加平台 Token 校验**，覆盖现有整桌面 VNC 与新增单窗口 VNC 通道（修复现状裸奔问题）。
- [x] **D7**：StationAgent PID 上报 —— **A：启动后短轮询（0.5s×10）等待目标窗口出现，取窗口所属进程 PID 上报**；合成器进程树枚举为主，启动瞬间 cmd 窗口按无标题/黑窗过滤。

## 11. 决策记录

| # | 决策点 | 拍板结果 | 日期 |
|---|---|---|---|
| D5 | RFB 编码策略：全帧 JPEG 还是差量矩形（Tight 增量） | **首版直接做差量矩形（Tight 增量编码：脏矩形检测 + JPEG 子编码）** | 2026-08-07 |
| D6 | 5901 端口访问控制：Loopback 沿用现状 还是 WS 代理加 Token 校验 | **B：服务端 WS 代理端点加平台 Token 校验（`/api/remote/vnc`、`/api/remote/ssh`、`/api/remote/capture` 统一覆盖），修复现状代理裸奔问题** | 2026-08-07 |
| D7 | StationAgent PID 上报方式：轮询窗口出现 还是 启动即报 cmd PID | **A：启动后短轮询（0.5s×10）等目标窗口出现取真实 PID 上报；合成器进程树枚举为主 + 无标题/黑窗过滤（cmd 黑窗不抓帧）** | 2026-08-07 |

## 12. 定稿说明

- 本子方案决策点（D5/D6/D7）已全部拍板；实施步骤（§7 S1~S6）与验证计划（§8）齐全，文档定稿，状态 → 已评审。
- 编码开始前需确认：图站 `StationAgent`/`StationApiServer` 侧改动与主服务侧改动均为独立项目，可并行；构建验证入口见 §7。
