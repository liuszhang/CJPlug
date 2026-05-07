using System.Net.WebSockets;
using CJ.Plug.StationApiServer.Services;

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

            return app;
        }

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
