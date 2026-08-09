using System.Net.WebSockets;
using CJ.Plug.Models.LogModels;
using CJ.Plug.StationApiServer.Services;
using CJ.Plug.StationApiServer.Services.Rfb;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.StationApiServer.Apis
{
    /// <summary>
    /// 远程桌面 API
    /// 提供 VNC/SSH 服务的检测和管理功能，以及窗口捕获 WebSocket 流
    /// </summary>
    public static class RemoteDesktopApi
    {
        public static IEndpointRouteBuilder MapRemoteDesktopApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/station/remote").WithTags("远程桌面服务");

            // 获取远程服务状态
            api.MapGet("/status", GetRemoteServiceStatus);

            // 启动 VNC 服务
            api.MapPost("/vnc/start", StartVncService);

            // 停止 VNC 服务
            api.MapPost("/vnc/stop", StopVncService);

            // 启动 SSH 服务
            api.MapPost("/ssh/start", StartSshService);

            // UltraVNC 端点组
            var uvncApi = api.MapGroup("/uvnc").WithTags("UltraVNC 管理");

            // UltraVNC 部署状态
            uvncApi.MapGet("/status", GetUvncStatus);

            // 部署 UltraVNC portable
            uvncApi.MapPost("/deploy", DeployUvnc);

            // 启动 UltraVNC
            uvncApi.MapPost("/start", StartUvnc);

            // 停止 UltraVNC
            uvncApi.MapPost("/stop", StopUvnc);

            // 更新 UltraVNC 配置
            uvncApi.MapPut("/config", UpdateUvncConfig);

            // 窗口捕获：列出所有可捕获的窗口
            api.MapGet("/capture/windows", GetCapturableWindows);

            // 窗口捕获：WebSocket 端点
            api.Map("/capture", HandleCaptureWebSocket);

            // === RFB 单窗口 VNC 绑定（子方案1）===
            // 绑定：StationAgent 上报工具 PID / 主服务按进程名绑定
            api.MapPost("/vnc-window/bind", BindWindowTarget);

            // 解绑
            api.MapDelete("/vnc-window/bind", UnbindWindowTarget);

            // 绑定状态
            api.MapGet("/vnc-window/status", GetWindowBindStatus);

            return app;
        }

        #region RFB 单窗口 VNC 绑定端点

        private static IResult BindWindowTarget(WindowBindRegistry registry, [FromBody] WindowBindRequest? request)
        {
            if (request == null || (request.ProcessId is null or <= 0) && string.IsNullOrEmpty(request.ProcessName))
                return TypedResults.BadRequest(new { Message = "缺少 ProcessId 或 ProcessName" });

            // 幂等：相同目标重复绑定不重复发 VncPidReady（否则 VncViewer 打开→绑定→通知→再打开 无限循环）。
            // 关键：判定"冗余绑定"必须带上 sessionKey（执行关联 ID = ToolJobCorrelationId）。
            //   - VncViewer 页面加载时用自己的 bind 回绑（sessionKey 为 null）→ 视为冗余，跳过；
            //   - 新的执行（sessionKey 与上一次不同）→ 视为新目标，必须重新通知。
            // 否则第二次执行若复用了同一 PID（Windows 回收 PID / 图站工具为常驻进程），会被误判为"相同目标"
            // 而漏发 VncPidReady，导致可视化弹窗第二次打不开（缺陷）。
            var current = registry.Current;
            var currentSession = registry.SessionKey;
            bool isRedundantBind = current != null
                && current.ProcessId == request.ProcessId
                && string.Equals(current.ProcessName, request.ProcessName, StringComparison.OrdinalIgnoreCase)
                && (request.SessionKey == null
                    || string.Equals(request.SessionKey, currentSession, StringComparison.OrdinalIgnoreCase));

            registry.SetTarget(new WindowTarget(request.ProcessId, request.ProcessName), request.SessionKey);

            if (isRedundantBind)
            {
                Console.WriteLine($"[VncPidReady] 目标未变化(PID={request.ProcessId})，跳过重复通知");
                return TypedResults.Ok(new { Message = "VNC 窗口目标已绑定（重复绑定，未变化）" });
            }

            // 单窗口 VNC 目标 PID 已就绪：通知前端此时再打开可视化页面（URL 带 pid）
            // 前端收到 VncPidReady 后才 window.open(...&pid=XXX)，彻底消除"窗口未出现"竞态
            try
            {
                var stationIp = ResolveLocalIp();
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    StationIp = stationIp,
                    Pid = request.ProcessId,
                    ProcessName = request.ProcessName,
                    SessionKey = request.SessionKey
                });
                CLog.Information(payload, null, null, null, null, LogTypeEnum.VncPidReady);
                Console.WriteLine($"[VncPidReady] 已通知前端打开可视化页面: pid={request.ProcessId} station={stationIp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VncPidReady] 通知失败: {ex.Message}");
            }

            return TypedResults.Ok(new { Message = "VNC 窗口目标已绑定" });
        }

        /// <summary>解析本机 IP（VncPidReady 通知用，前端按 IP 匹配图站）。</summary>
        private static string ResolveLocalIp()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ip?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private static IResult UnbindWindowTarget(WindowBindRegistry registry, string? sessionKey = null)
        {
            registry.Clear(sessionKey);
            return TypedResults.Ok(new { Message = "VNC 窗口目标已解绑" });
        }

        private static IResult GetWindowBindStatus(WindowBindRegistry registry)
        {
            var target = registry.Current;
            return TypedResults.Ok(new
            {
                bound = target != null,
                processId = target?.ProcessId,
                processName = target?.ProcessName,
                sessionKey = registry.SessionKey
            });
        }

        public class WindowBindRequest
        {
            public int? ProcessId { get; set; }
            public string? ProcessName { get; set; }
            public string? SessionKey { get; set; }
        }

        #endregion

        #region 窗口捕获端点

        private static IResult GetCapturableWindows(WindowCaptureService captureService)
        {
            var windows = captureService.GetCapturableWindows();
            return TypedResults.Ok(windows.Select(w => new
            {
                handle = w.Handle.ToString(),
                title = w.Title,
                processName = w.ProcessName,
                processId = w.ProcessId
            }));
        }

        private static async Task HandleCaptureWebSocket(HttpContext context, WindowCaptureService captureService)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("需要 WebSocket 连接");
                return;
            }

            var processName = context.Request.Query["processName"].ToString();
            if (string.IsNullOrEmpty(processName))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("缺少 processName 参数");
                return;
            }

            var fpsStr = context.Request.Query["fps"].ToString();
            int fps = 15;
            if (!string.IsNullOrEmpty(fpsStr) && int.TryParse(fpsStr, out var parsedFps))
                fps = parsedFps;

            var ws = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid().ToString();

            try
            {
                await captureService.HandleWebSocketAsync(ws, processName, fps, connectionId);
            }
            finally
            {
                captureService.RemoveSession(connectionId);
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "完成", CancellationToken.None);
                    }
                    catch { }
                }
                ws.Dispose();
            }
        }

        #endregion

        #region 原有端点

        private static IResult GetRemoteServiceStatus(RemoteDesktopService service)
        {
            var status = service.GetStatus();
            return TypedResults.Ok(status);
        }

        private static async Task<IResult> StartVncService(RemoteDesktopService service)
        {
            var success = await service.StartVncAsync(5900);
            return success
                ? TypedResults.Ok(new { Message = "VNC 服务已启动", Port = 5900 })
                : TypedResults.BadRequest(new { Message = "启动 VNC 服务失败，请检查是否已安装 VNC 或已部署 UltraVNC" });
        }

        private static IResult StopVncService(RemoteDesktopService service)
        {
            var (success, message) = service.StopVnc();
            return success
                ? TypedResults.Ok(new { Message = message })
                : TypedResults.BadRequest(new { Message = message });
        }

        private static async Task<IResult> StartSshService(RemoteDesktopService service)
        {
            var success = await service.StartSshAsync(22);
            return success
                ? TypedResults.Ok(new { Message = "SSH 服务已启动", Port = 22 })
                : TypedResults.BadRequest(new { Message = "启动 SSH 服务失败，请检查是否已安装 OpenSSH" });
        }

        #endregion

        #region UltraVNC 端点

        private static IResult GetUvncStatus(UltraVncService service)
        {
            var status = service.GetStatus();
            return TypedResults.Ok(status);
        }

        private static async Task<IResult> DeployUvnc(UltraVncService service)
        {
            var (success, message) = await service.DeployAsync();
            return success
                ? TypedResults.Ok(new { Message = message })
                : TypedResults.BadRequest(new { Message = message });
        }

        private static async Task<IResult> StartUvnc(UltraVncService service)
        {
            var (success, message) = await service.StartAsync();
            return success
                ? TypedResults.Ok(new { Message = message })
                : TypedResults.BadRequest(new { Message = message });
        }

        private static IResult StopUvnc(UltraVncService service)
        {
            var (success, message) = service.Stop();
            return success
                ? TypedResults.Ok(new { Message = message })
                : TypedResults.BadRequest(new { Message = message });
        }

        private static IResult UpdateUvncConfig(UltraVncService service, UvncConfigRequest? request)
        {
            if (request == null)
                return TypedResults.BadRequest(new { Message = "缺少配置参数" });

            var (success, message) = service.UpdateConfig(
                port: request.Port ?? 5900,
                loopback: request.Loopback ?? true);

            return success
                ? TypedResults.Ok(new { Message = message })
                : TypedResults.BadRequest(new { Message = message });
        }

        #endregion

        private class UvncConfigRequest
        {
            public int? Port { get; set; }
            public bool? Loopback { get; set; }
        }
    }
}
