using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;

using CJ.Plug.Models.Plug;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug.StationApiService.Contracts;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace CJ.Plug_Aspire.StationApiService.StationApi
{
    public static class StationApi
    {
        public static IEndpointRouteBuilder MapConnectionApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/station");

            api.MapGet("/executeAction/{action}", ExecuteAction);
            //api.MapPost("/executeToolCommand", async (StationExecutionRequest request) =>await ExecuteToolCommand(request));
            api.MapPost("/executeToolCommand", async (IStationExecuteService service, PlugExecutionRequest request) => await service.ExecuteRequestCommand(request));
            api.MapPost("/reportResult", async (IStationExecuteService service, [FromBody] ExecuteResultData result) => await service.ReportExecuteResult(result));
            api.MapPost("/sendLog", async (IStationExecuteService service, LogModel log) => await service.SendLog(log));

            api.MapGet("/DownloadFileByFileId/{fileId}", async (IStationFileService service, string fileId, string fileName) => await service.DownloadFileByFileId(fileId,fileName));
            api.MapPost("/UploadFileToVariable", async (IStationFileService service, [FromBody] PlugVariableData request) => await service.UploadFileToVariable(request));



            // 连接状态接口 - 供 StationSettingUI 查询图站与主服务器的长连接状态
            api.MapGet("/connection-status", (StationHubService hubService) =>
            {
                return TypedResults.Ok(new
                {
                    HubConnected = hubService.IsHubConnected,
                    MainServerUrl = hubService.MainServerUrl,
                    LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            });

            // 图站任务列表
            api.MapGet("/tasks", GetTasks);

            // 图站任务详情
            api.MapGet("/tasks/{id}", GetTaskById);

            // 手动停止任务
            api.MapPost("/tasks/{id}/stop", async (IStationExecuteService service, int id) =>
            {
                await service.StopTask(id);
                return TypedResults.Ok(new { message = "已发送停止指令" });
            });

            api.MapGet("/test", () => TypedResults.Ok("pong!"));

            // 从主服务器获取所有支持的工具列表
            // 优先使用 MainApiServer，因为 API 端点在 ApiServer 上注册（非 DispatchServer）
            api.MapGet("/tools", async () =>
            {
                var mainServerUrl = !string.IsNullOrEmpty(GlobalData.MainApiServer)
                    ? GlobalData.MainApiServer
                    : StaticData.MainServerUrl;
                var baseUrl = mainServerUrl.TrimEnd('/');

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var response = await httpClient.GetAsync($"{baseUrl}/api/Tool/GetAllTools");
                    if (!response.IsSuccessStatusCode)
                        return Results.BadRequest($"获取工具列表失败，HTTP状态码: {response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    return Results.Content(content, "application/json", Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "从主服务器获取工具列表失败");
                    return Results.BadRequest($"获取工具列表失败: {ex.Message}");
                }
            });

            // 检查图站上工具文件是否存在（兼容文件、目录和工具名）
            api.MapPost("/fileExists", async (HttpContext context) =>
            {
                var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body);
                var filePath = body.GetProperty("filePath").GetString();

                // 1. 先检查原始路径（绝对路径场景）
                //    对目录额外验证包含可执行文件，避免空目录（如下载残留）产生误报
                if (File.Exists(filePath))
                    return Results.Ok(true);
                if (Directory.Exists(filePath))
                {
                    var exeExtensions = new[] { ".exe", ".bat", ".cmd" };
                    var hasExe = exeExtensions.Any(ext =>
                        Directory.GetFiles(filePath, $"*{ext}", SearchOption.AllDirectories).Length > 0);
                    if (hasExe) return Results.Ok(true);
                }

                // 2. 尝试使用图站配置的 ToolsRootPath 解析
                if (!string.IsNullOrEmpty(StaticData.ToolsRootPath) && !string.IsNullOrEmpty(filePath))
                {
                    // filePath 可能是工具名或相对路径，尝试直接拼接
                    var stationPath = Path.Combine(StaticData.ToolsRootPath, filePath);
                    if (Directory.Exists(stationPath))
                    {
                        // 目录存在，检查是否有可执行文件
                        var exeExtensions = new[] { ".exe", ".bat", ".cmd" };
                        var hasExe = exeExtensions.Any(ext =>
                            Directory.GetFiles(stationPath, $"*{ext}", SearchOption.AllDirectories).Length > 0);
                        if (hasExe) return Results.Ok(true);
                    }
                    if (File.Exists(stationPath))
                        return Results.Ok(true);
                }

                // 3. 使用默认路径（与 Step 2 一致：目录需验证包含可执行文件）
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fallbackBase = StaticData.ToolAgentServer ?? GlobalData.StationFileRootPath;
                    var fallbackPath = Path.Combine(fallbackBase, "Tools", filePath);
                    if (File.Exists(fallbackPath))
                        return Results.Ok(true);
                    if (Directory.Exists(fallbackPath))
                    {
                        var exeExtensions = new[] { ".exe", ".bat", ".cmd" };
                        var hasExe = exeExtensions.Any(ext =>
                            Directory.GetFiles(fallbackPath, $"*{ext}", SearchOption.AllDirectories).Length > 0);
                        if (hasExe) return Results.Ok(true);
                    }
                }

                return Results.Ok(false);
            });

            // 从主服务器下载工具文件到图站
            // 使用 staging 目录 + 原子 rename，确保工具安装完整性
            api.MapPost("/downloadTool", async (HttpContext context) =>
            {
                var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body);
                var targetPath = body.GetProperty("targetPath").GetString();
                var toolName = body.TryGetProperty("toolName", out var nameProp) ? nameProp.GetString() : "unknown";
                var toolVersion = body.TryGetProperty("toolVersion", out var verProp) ? verProp.GetString() : "latest";
                var toolFilePath = body.TryGetProperty("toolFilePath", out var fpProp) ? fpProp.GetString() : null;

                if (string.IsNullOrEmpty(targetPath))
                    return Results.BadRequest("targetPath 不能为空");

                // 优先使用图站配置的工具安装路径（StationSettingUI 中设置的 ToolsRootPath）
                // —— targetPath 可能已包含用户层级（如 "liusz/MyTool"），直接使用而非仅依赖 toolName
                var relativeTarget = targetPath ?? toolName ?? "unknown";
                relativeTarget = relativeTarget.Replace('/', Path.DirectorySeparatorChar);
                if (!string.IsNullOrEmpty(StaticData.ToolsRootPath))
                {
                    var stationTargetPath = Path.Combine(StaticData.ToolsRootPath, relativeTarget);
                    Log.Information("使用图站配置的工具安装路径: {StationPath}（原始请求: {OriginalPath}）",
                        stationTargetPath, targetPath);
                    targetPath = stationTargetPath;
                }
                else
                {
                    // ToolsRootPath 未配置时，使用 ToolAgentServer 默认路径
                    var fallbackBase = StaticData.ToolAgentServer ?? GlobalData.StationFileRootPath;
                    targetPath = Path.Combine(fallbackBase, "Tools", relativeTarget);
                    Log.Warning("图站未配置 ToolsRootPath，使用默认路径: {FallbackPath}", targetPath);
                }

                // 优先使用 MainApiServer，因为 API 端点在 ApiServer 上注册（非 DispatchServer）
                var mainServerUrl = !string.IsNullOrEmpty(GlobalData.MainApiServer)
                    ? GlobalData.MainApiServer
                    : StaticData.MainServerUrl;
                var baseUrl = mainServerUrl.TrimEnd('/');
                var downloadUrl = $"{baseUrl}/api/file/downloadTool?name={Uri.EscapeDataString(toolName!)}&version={Uri.EscapeDataString(toolVersion!)}";
                if (!string.IsNullOrEmpty(toolFilePath))
                    downloadUrl += $"&path={Uri.EscapeDataString(toolFilePath)}";

                // staging 目录：解压到临时目录，成功后原子 rename 到正式目标路径
                var stagingDir = targetPath + "_staging_" + Guid.NewGuid().ToString("N")[..8];
                var tempZipPath = Path.Combine(Path.GetTempPath(), $"tool_{Guid.NewGuid()}.zip");

                // 重试逻辑：带超时与指数退避（后续可升级为 IHttpClientFactory）
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        // 清理上次失败的残留
                        SafeDeleteDirectory(stagingDir);
                        SafeDeleteFile(tempZipPath);

                        // 使用带超时的 HttpClient
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
                        var response = await httpClient.GetAsync(downloadUrl);
                        if (!response.IsSuccessStatusCode)
                            return Results.BadRequest($"下载工具失败，HTTP状态码: {response.StatusCode}");

                        // 下载 zip 到临时文件
                        await using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        // 解压到 staging 目录
                        Directory.CreateDirectory(stagingDir);
                        ZipFile.ExtractToDirectory(tempZipPath, stagingDir, overwriteFiles: true);

                        // 原子替换：删除旧目录，将 staging 重命名为正式路径
                        if (Directory.Exists(targetPath))
                        {
                            SafeDeleteDirectory(targetPath);
                        }
                        Directory.Move(stagingDir, targetPath);

                        Log.Information("工具 {ToolName}({ToolVersion}) 下载安装成功 -> {TargetPath}",
                            toolName, toolVersion, targetPath);
                        return Results.Ok(true);
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        var delaySeconds = (int)Math.Pow(2, attempt);
                        Log.Warning(ex, "下载工具 {ToolName}({ToolVersion}) 第{Attempt}次失败，{Delay}秒后重试",
                            toolName, toolVersion, attempt, delaySeconds);

                        // 清理本次失败的残留
                        SafeDeleteDirectory(stagingDir);
                        SafeDeleteFile(tempZipPath);

                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "下载工具 {ToolName}({ToolVersion}) 失败（已重试{MaxRetries}次）",
                            toolName, toolVersion, maxRetries);

                        // 失败清理：确保不残留损坏文件
                        SafeDeleteDirectory(stagingDir);
                        SafeDeleteFile(tempZipPath);

                        return Results.BadRequest($"下载工具失败: {ex.Message}");
                    }
                    finally
                    {
                        // 保底清理临时 zip（不管成功失败）
                        SafeDeleteFile(tempZipPath);
                    }
                }

                return Results.BadRequest("下载工具失败，已达最大重试次数");
            });


            return app;
        }


        public static Task<string> ExecuteAction(string action)
        {
            Log.Information("Station执行消息-" + action);
            string tmp = "";
            // 创建 ProcessStartInfo 对象，设置 FileName 为 notepad.exe
            var startInfo = new ProcessStartInfo("notepad.exe")
            {
                UseShellExecute = true, // 允许用户交互
                CreateNoWindow = false // 创建窗口，以便用户可以看到并操作应用程序
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;

                // 添加 Exited 事件处理器来处理进程退出时的操作
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    Log.Information("Notepad has been closed.");
                    // 在这里可以添加额外的逻辑，例如通知用户或进行其他清理工作
                };

                // 启动进程
                process.Start();

                // 输出进程 ID
                Log.Information($"Notepad started with process id: {process.Id}");
                tmp += $"Notepad started with process id: {process.Id}\n";

                // 这里可以做其他事情，或者等待用户指令

                // 等待一段时间后检查进程是否已经退出
                for (int i = 0; i < 50; i++)
                {
                    if (process.HasExited)
                    {
                        Log.Information("Process has already exited.");
                        break;
                    }
                    Log.Information("Checking if Notepad is still running...");
                    System.Threading.Thread.Sleep(3000); // 每隔3秒检查一次
                }

                // 如果程序没有退出，我们可以选择等待它退出
                if (!process.HasExited)
                {
                    Log.Information("Waiting for Notepad to exit...");
                    process.WaitForExit(); // 阻塞直到进程退出
                }

                Log.Information("Notepad has finished execution.");
                tmp += "Notepad has finished execution.\n";
            }

            return Task.FromResult("Station返回消息：\n" + tmp);
        }

        private static IResult GetTasks(StationTaskStore taskStore)
        {
            taskStore.FixStaleRunningTasks();
            var tasks = taskStore.GetAll(200);
            return TypedResults.Ok(tasks);
        }

        private static IResult GetTaskById(StationTaskStore taskStore, int id)
        {
            var task = taskStore.GetById(id);
            return task is not null ? TypedResults.Ok(task) : TypedResults.NotFound();
        }

        /// <summary>
        /// 安全删除目录：忽略文件锁、权限不足等异常，仅记录日志。
        /// </summary>
        private static void SafeDeleteDirectory(string? path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;
            try
            {
                Directory.Delete(path, recursive: true);
                Log.Information("已清理残留目录: {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "清理残留目录失败（已忽略）: {Path}", path);
            }
        }

        /// <summary>
        /// 安全删除文件：忽略文件锁、权限不足等异常，仅记录日志。
        /// </summary>
        private static void SafeDeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;
            try
            {
                File.Delete(path);
                Log.Information("已清理残留文件: {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "清理残留文件失败（已忽略）: {Path}", path);
            }
        }
    }
}
