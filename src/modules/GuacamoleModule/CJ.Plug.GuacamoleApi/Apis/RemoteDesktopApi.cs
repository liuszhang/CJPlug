using CJ.Plug.GuacamoleApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CJ.Plug.GuacamoleApi.Apis
{
    /// <summary>
    /// 远程桌面 WebSocket 代理 API
    /// 提供 VNC 和 SSH 的 WebSocket 代理端点
    /// </summary>
    public static class RemoteDesktopApi
    {
        public static IEndpointRouteBuilder MapRemoteDesktopApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/remote").WithTags("远程桌面代理");

            // VNC WebSocket 代理端点
            // ws://host/api/remote/vnc?host=192.168.1.100&port=5900
            api.Map("/vnc", async (HttpContext context, VncWebSocketProxy proxy) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("需要 WebSocket 请求");
                    return;
                }

                var host = context.Request.Query["host"].FirstOrDefault() ?? throw new ArgumentException("缺少 host 参数");
                var portStr = context.Request.Query["port"].FirstOrDefault();
                var port = string.IsNullOrEmpty(portStr) ? 5900 : int.Parse(portStr);

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await proxy.HandleAsync(webSocket, host, port);
            })
            .WithName("VncWebSocketProxy")
            .WithDescription("VNC WebSocket 代理");

            // SSH WebSocket 代理端点
            // ws://host/api/remote/ssh?host=192.168.1.100&port=22&username=root&password=xxx
            api.Map("/ssh", async (HttpContext context, SshWebSocketProxy proxy) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("需要 WebSocket 请求");
                    return;
                }

                var host = context.Request.Query["host"].FirstOrDefault() ?? throw new ArgumentException("缺少 host 参数");
                var portStr = context.Request.Query["port"].FirstOrDefault();
                var port = string.IsNullOrEmpty(portStr) ? 22 : int.Parse(portStr);
                var username = context.Request.Query["username"].FirstOrDefault() ?? "root";
                var password = context.Request.Query["password"].FirstOrDefault() ?? "";

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await proxy.HandleAsync(webSocket, host, port, username, password);
            })
            .WithName("SshWebSocketProxy")
            .WithDescription("SSH WebSocket 代理");

            // Capture WebSocket 代理端点 (WS-to-WS)
            // ws://host/api/remote/capture?host=192.168.1.100&processName=MyApp&fps=5
            api.Map("/capture", async (HttpContext context, CaptureWebSocketProxy proxy) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("需要 WebSocket 请求");
                    return;
                }

                var host = context.Request.Query["host"].FirstOrDefault() ?? throw new ArgumentException("缺少 host 参数");
                var processName = context.Request.Query["processName"].FirstOrDefault() ?? throw new ArgumentException("缺少 processName 参数");
                var fpsStr = context.Request.Query["fps"].FirstOrDefault();
                var fps = string.IsNullOrEmpty(fpsStr) ? 5 : int.Parse(fpsStr);
                var portStr = context.Request.Query["port"].FirstOrDefault();
                var stationPort = string.IsNullOrEmpty(portStr) ? 7660 : int.Parse(portStr);

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await proxy.HandleAsync(webSocket, host, processName, fps, stationPort);
            })
            .WithName("CaptureWebSocketProxy")
            .WithDescription("屏幕捕获 WebSocket 代理");

            // 窗口列表代理 — 转发到 StationApiServer 的 /api/station/remote/capture/windows
            api.MapGet("/capture/windows/{stationIp}", async (string stationIp) =>
            {
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var stationUrl = $"http://{stationIp}:7660";
                    var response = await httpClient.GetAsync($"{stationUrl}/api/station/remote/capture/windows");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return Results.Content(json, "application/json");
                    }
                    return Results.Ok(new { windows = Array.Empty<object>(), message = "Station 无响应" });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { windows = Array.Empty<object>(), message = $"连接失败: {ex.Message}" });
                }
            })
            .WithName("GetCaptureWindows")
            .WithDescription("获取 Station 可捕获的窗口列表");

            return app;
        }
    }
}
