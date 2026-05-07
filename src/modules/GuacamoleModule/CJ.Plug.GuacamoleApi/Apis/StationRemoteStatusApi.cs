using CJ.Plug.Models.Station;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace CJ.Plug.GuacamoleApi.Apis
{
    /// <summary>
    /// Station 远程桌面状态查询 API
    /// 用于查询 Station 的 VNC/SSH 服务状态
    /// </summary>
    public static class StationRemoteStatusApi
    {
        public static IEndpointRouteBuilder MapStationRemoteStatusApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/station-remote").WithTags("Station远程桌面状态");

            // 查询 Station 的远程服务状态
            api.MapGet("/status/{stationIp}", async (string stationIp, IStationManageService stationService) =>
            {
                var station = await stationService.GetByStationIpAsync(stationIp);
                if (station == null)
                {
                    return Results.NotFound(new { Message = $"未找到 Station: {stationIp}" });
                }

                try
                {
                    // 调用 Station API 获取远程服务状态
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var stationUrl = $"http://{stationIp}:7660";
                    var response = await httpClient.GetAsync($"{stationUrl}/api/station/remote/status");

                    if (response.IsSuccessStatusCode)
                    {
                        var status = await response.Content.ReadFromJsonAsync<RemoteServiceStatusDto>();
                        return Results.Ok(status);
                    }

                    return Results.Ok(new RemoteServiceStatusDto
                    {
                        Message = "无法连接到 Station API"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new RemoteServiceStatusDto
                    {
                        Message = $"查询失败: {ex.Message}"
                    });
                }
            })
            .WithName("GetStationRemoteStatus")
            .WithDescription("查询 Station 的 VNC/SSH 服务状态");

            // 请求 Station 启动 VNC 服务
            api.MapPost("/vnc/start/{stationIp}", async (string stationIp, [FromQuery] int port, IStationManageService stationService) =>
            {
                var station = await stationService.GetByStationIpAsync(stationIp);
                if (station == null)
                    return Results.NotFound();

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var stationUrl = $"http://{stationIp}:7660";
                    var response = await httpClient.PostAsync($"{stationUrl}/api/station/remote/vnc/start?port={port}", null);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<object>();
                        return Results.Ok(result);
                    }

                    return Results.BadRequest(new { Message = "启动 VNC 失败" });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
            })
            .WithName("StartStationVnc")
            .WithDescription("请求 Station 启动 VNC 服务");

            // 请求 Station 启动 SSH 服务
            api.MapPost("/ssh/start/{stationIp}", async (string stationIp, [FromQuery] int port, IStationManageService stationService) =>
            {
                var station = await stationService.GetByStationIpAsync(stationIp);
                if (station == null)
                    return Results.NotFound();

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var stationUrl = $"http://{stationIp}:7660";
                    var response = await httpClient.PostAsync($"{stationUrl}/api/station/remote/ssh/start?port={port}", null);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<object>();
                        return Results.Ok(result);
                    }

                    return Results.BadRequest(new { Message = "启动 SSH 失败" });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
            })
            .WithName("StartStationSsh")
            .WithDescription("请求 Station 启动 SSH 服务");

            return app;
        }

        private class RemoteServiceStatusDto
        {
            public bool VncInstalled { get; set; }
            public bool VncRunning { get; set; }
            public int VncPort { get; set; }
            public bool SshInstalled { get; set; }
            public bool SshRunning { get; set; }
            public int SshPort { get; set; }
            public string? Message { get; set; }
        }
    }
}
