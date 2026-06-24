using CJ.Plug.ApiServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.ApiServer.Apis;

public static class PackageApi
{
    public static IEndpointRouteBuilder MapPackageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/package").WithTags("本地部署包");

        // 异步下载 - 返回任务ID
        api.MapPost("/download", async (
            [FromQuery] string platform,
            [FromQuery] bool includeDocker,
            PackageService packageService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PackageApi");
            try
            {
                logger.LogInformation("收到下载请求，平台: {Platform}, 包含Docker: {IncludeDocker}", platform, includeDocker);

                // 验证平台参数
                var supportedPlatforms = new[] { "win-x64", "linux-x64", "osx-x64", "win-arm64", "linux-arm64", "osx-arm64" };
                if (!supportedPlatforms.Contains(platform))
                {
                    return Results.BadRequest($"不支持的平台: {platform}。支持的平台: {string.Join(", ", supportedPlatforms)}");
                }

                // 启动异步打包任务
                var taskId = await packageService.GenerateLocalPackageAsync(platform, includeDocker);

                return Results.Ok(new { TaskId = taskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "启动打包任务失败");
                return Results.Problem($"启动打包任务失败: {ex.Message}");
            }
        })
        .WithName("StartDownloadPackage")
        .WithDescription("启动下载任务");

        // 图站部署包下载 - 返回任务ID
        api.MapPost("/download-station", async (
            [FromQuery] string platform,
            PackageService packageService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PackageApi");
            try
            {
                logger.LogInformation("收到图站部署包下载请求，平台: {Platform}", platform);

                var supportedPlatforms = new[] { "win-x64", "linux-x64", "osx-x64", "win-arm64", "linux-arm64", "osx-arm64" };
                if (!supportedPlatforms.Contains(platform))
                {
                    return Results.BadRequest($"不支持的平台: {platform}。支持的平台: {string.Join(", ", supportedPlatforms)}");
                }

                var taskId = await packageService.GenerateStationPackageAsync(platform);

                return Results.Ok(new { TaskId = taskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "启动图站部署包打包失败");
                return Results.Problem($"启动图站部署包打包失败: {ex.Message}");
            }
        })
        .WithName("StartDownloadStationPackage")
        .WithDescription("启动图站部署包下载");

        // 图站部署包下载 - 同步一步完成，直接返回文件流
        api.MapPost("/download-station-direct", async (
            [FromQuery] string platform,
            PackageService packageService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PackageApi");
            try
            {
                logger.LogInformation("收到图站部署包同步下载请求，平台: {Platform}", platform);

                var supportedPlatforms = new[] { "win-x64", "linux-x64", "osx-x64", "win-arm64", "linux-arm64", "osx-arm64" };
                if (!supportedPlatforms.Contains(platform))
                {
                    return Results.BadRequest($"不支持的平台: {platform}。支持的平台: {string.Join(", ", supportedPlatforms)}");
                }

                // 同步：打包完成后直接返回文件流，不经过进度轮询
                var zipBytes = await packageService.GenerateStationPackageDirectAsync(platform, ct);
                return Results.File(zipBytes, "application/zip", $"CJPlug-Station-{platform}.zip");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("图站部署包同步打包被取消");
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "图站部署包同步打包失败");
                return Results.Problem($"打包失败: {ex.Message}");
            }
        })
        .WithName("DownloadStationPackageDirect")
        .WithDescription("同步下载图站部署包（一步完成，直接返回文件流）");

        // 图站部署包下载 - GET 端点，浏览器原生下载
        api.MapGet("/download-station-direct", async (
            [FromQuery] string platform,
            PackageService packageService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PackageApi");
            try
            {
                logger.LogInformation("收到图站部署包 GET 下载请求，平台: {Platform}", platform);

                var supportedPlatforms = new[] { "win-x64", "linux-x64", "osx-x64", "win-arm64", "linux-arm64", "osx-arm64" };
                if (!supportedPlatforms.Contains(platform))
                {
                    return Results.BadRequest($"不支持的平台: {platform}。支持的平台: {string.Join(", ", supportedPlatforms)}");
                }

                var zipBytes = await packageService.GenerateStationPackageDirectAsync(platform, ct);
                return Results.File(zipBytes, "application/zip", $"CJPlug-Station-{platform}.zip");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("图站部署包 GET 打包被取消");
                return Results.StatusCode(499);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "图站部署包 GET 打包失败");
                return Results.Problem($"打包失败: {ex.Message}");
            }
        })
        .WithName("DownloadStationPackageGet")
        .WithDescription("浏览器原生下载图站部署包（GET，直接导航即可下载）");

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
}