using CJ.Plug.ApiServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.ApiServer.Apis;

public static class PackageApi
{
    public static IEndpointRouteBuilder MapPackageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/package").WithTags("本地部署包");

        // ⚠ 已退役（《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》枝 18/19，2026-10-07）
        api.MapPost("/download", ([FromQuery] string platform, [FromQuery] bool includeDocker) =>
                RetiredPackageFeature())
        .WithName("StartDownloadPackage")
        .WithDescription("[已退役] 启动下载任务");

        // ⚠ 已退役（同上）
        api.MapPost("/download-station", ([FromQuery] string platform) =>
                RetiredPackageFeature())
        .WithName("StartDownloadStationPackage")
        .WithDescription("[已退役] 启动图站部署包下载");

        // ⚠ 已退役（同上）
        api.MapPost("/download-station-direct", ([FromQuery] string platform) =>
                RetiredPackageFeature())
        .WithName("DownloadStationPackageDirect")
        .WithDescription("[已退役] 同步下载图站部署包");

        // ⚠ 已退役（同上）
        api.MapGet("/download-station-direct", ([FromQuery] string platform) =>
                RetiredPackageFeature())
        .WithName("DownloadStationPackageGet")
        .WithDescription("[已退役] 浏览器原生下载图站部署包");

        // 获取任务进度
        api.MapGet("/progress/{taskId}", (
            string taskId,
            PackageProgressTracker progressTracker) =>
        {
            var progress = progressTracker.GetProgress(taskId);
            if (progress == null)
            {
                return Results.NotFound("任务不存在");
            }

            return Results.Ok(new
            {
                progress.TaskId,
                Status = progress.Status.ToString(),
                progress.Progress,
                progress.Message,
                progress.StartTime,
                progress.EndTime,
                Logs = progress.Logs.Select(l => new
                {
                    l.Timestamp,
                    l.Message,
                    l.Level
                }).ToList()
            });
        })
        .WithName("GetPackageProgress")
        .WithDescription("获取打包任务进度");

        // 下载已完成的包
        api.MapGet("/download/{taskId}", (
            string taskId,
            PackageProgressTracker progressTracker,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PackageApi");
            var progress = progressTracker.GetProgress(taskId);
            
            if (progress == null)
            {
                return Results.NotFound("任务不存在");
            }

            if (progress.Status != PackageStatus.Completed)
            {
                return Results.BadRequest("任务尚未完成");
            }

            if (progress.ZipBytes == null)
            {
                return Results.NotFound("打包文件不存在");
            }

            logger.LogInformation("下载打包文件，任务ID: {TaskId}", taskId);
            return Results.File(progress.ZipBytes, "application/zip", $"CJPlug-Local-{taskId[..8]}.zip");
        })
        .WithName("DownloadPackageFile")
        .WithDescription("下载打包文件");

        // 获取支持的平台列表
        api.MapGet("/platforms", () =>
        {
            var platforms = new[]
            {
                new { Id = "win-x64", Name = "Windows x64", Description = "Windows 64位系统" },
                new { Id = "win-arm64", Name = "Windows ARM64", Description = "Windows ARM64系统" },
                new { Id = "linux-x64", Name = "Linux x64", Description = "Linux 64位系统" },
                new { Id = "linux-arm64", Name = "Linux ARM64", Description = "Linux ARM64系统" },
                new { Id = "osx-x64", Name = "macOS x64", Description = "macOS Intel处理器" },
                new { Id = "osx-arm64", Name = "macOS ARM64", Description = "macOS Apple Silicon处理器" }
            };

            return Results.Ok(platforms);
        })
        .WithName("GetSupportedPlatforms")
        .WithDescription("获取支持的平台列表");

        // 检查打包服务状态
        api.MapGet("/status", () =>
        {
            return Results.Ok(new
            {
                Status = "Available",
                SupportedPlatforms = new[] { "win-x64", "linux-x64", "osx-x64", "win-arm64", "linux-arm64", "osx-arm64" },
                Features = new[] { "Docker支持", "跨平台启动脚本", "自动配置生成", "实时进度监控" },
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetPackageStatus")
        .WithDescription("检查打包服务状态");

        return app;
    }

    /// <summary>
    /// 「本地启动包 / 图站部署包」已退役（《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》枝 18/19，2026-10-07）。
    /// <para>
    /// 为什么退役：这两个功能由主服务器**现场从仓库 <c>02.Publish</c> 取件**生成
    /// （<c>PackageService.GetRepositoryRoot</c> 靠向上找 <c>CJ.Plug-Aspire.sln</c>），
    /// 一旦装到 <c>%ProgramFiles%</c> 就必然找不到仓库 ⇒ 功能必坏（实测，见方案 §4.7 依据）。
    /// </para>
    /// <para>
    /// 打包分发已统一收敛到 CJSuite：<c>cjplug</c>（服务器形态）/ <c>cjplug-desktop</c>（个人形态）/
    /// <c>station-settingui</c>（图站形态）三个组件。端点保留路径但返回 410，便于旧客户端区分"退役"与"故障"。
    /// </para>
    /// </summary>
    private static IResult RetiredPackageFeature() =>
        Results.Problem(
            "该功能已退役：CJPlug 打包分发已统一收敛到 CJSuite 安装包（cjplug / cjplug-desktop / station-settingui 组件）。请改用 CJSuite 出包。",
            statusCode: StatusCodes.Status410Gone);
}