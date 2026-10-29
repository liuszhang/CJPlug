using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class PlugExecuteApi
{
    public static IEndpointRouteBuilder MapPlugExecuteApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/plug").WithTags("插头执行");

        api.MapGet("/executePlugByDefinitionId/{DefinitionId}", async (IPlugExecuteService service, string DefinitionId) => await service.ExecutePlug(DefinitionId));

        api.MapPost("/executePlug/{DefinitionId}", async (IPlugExecuteService service, string DefinitionId, PlugExecutionRequest? request) => await service.ExecutePlug(DefinitionId,request));

        //统一使用PlugExecutionRequest进行执行
        api.MapPost("/executePlug", async (IPlugExecuteService service, PlugExecutionRequest? request) => await service.StartExecutePlug(request));

        api.MapPost("/executeMcpTool", async (IPlugExecuteService service, [FromBody] CJ.Plug.Models.MCPTools.McpToolExecutionRequest request) => await service.ExecuteMcpTool(request));

        api.MapGet("/executionStatus/{workflowInstanceId}", async (IPlugExecuteService service, string workflowInstanceId) =>
        {
            var result = await service.GetExecutionStatus(workflowInstanceId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        });

        api.MapPost("/ReportExecuteResult", async (IPlugExecuteService service, [FromBody] ExecuteResultData executeReport) => await service.ReportExecuteResult(executeReport));

        api.MapPost("/launchStandalone/{DefinitionId}", async (HttpContext context, string DefinitionId, IConfiguration configuration, LaunchStandaloneRequest? request) =>
        {
            try
            {
                // ── P5.9 / D-P5-7：身份传递 ──
                // 本端点由 Blazor 电路经**裸 HttpClient**（BaseApiClient.httpClient）调用 ⇒ 端点自身拿不到
                // Cookie/Bearer，调用者身份只能由前端读 .ThreeRole.Token（非 HttpOnly）后放进 body 带过来。
                //
                // ⚠ 无 AT 一律 401：本端点的副作用是 Process.Start，匿名可达等于给本机开一个「无限弹窗」入口。
                //    手工要从命令行启动 ExecuteApp 的用户仍可直接跑 exe（无 --ticket 时降级到登录页），不受影响。
                var accessToken = request?.AccessToken;
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return Results.Unauthorized();
                }

                // 定位 ExecuteApp.exe。
                // ⚠ P5.9 修正存量 bug：原代码用 Path.Combine("..","..","..","CJ.Plug.ExecuteApp") +
                //   "[Debug|Release]/net10.0-windows/" 去拼，但本仓 BaseOutputPath 是 `..\..\..\..\02.Publish\Services`
                //   （**不带项目名**），所以 ExecuteApp 实际与 ApiServer 是**同层兄弟**：
                //     02.Publish\Services\Debug\net10.0\        ← ApiServer（ApiServer 的 TFM 是 net10.0）
                //     02.Publish\Services\Debug\net10.0-windows\ ← ExecuteApp（WinExe，TFM 带 -windows）
                //   旧写法落到 02.Publish\CJ.Plug.ExecuteApp，永远找不到 —— 此前该端点从未被真实调用过故未暴露。
                //   这里改成**候选探测**而非再次硬推目录层级：目录布局随打包方式（Debug/Release/05.Release）变化，
                //   逐个试比赌一个相对路径可靠。
                var hostDir = AppContext.BaseDirectory;
                var configuredRoot = configuration.GetValue<string>("StandaloneExecute:AppPath");

                var candidates = new List<string>();
                foreach (var config in new[] { "Debug", "Release" })
                {
                    // ① 同层兄弟（当前 BaseOutputPath 布局）
                    candidates.Add(Path.Combine(hostDir, "..", "net10.0-windows", "CJ.Plug.ExecuteApp.exe"));
                    // ② 配置的 AppPath 根目录
                    if (!string.IsNullOrWhiteSpace(configuredRoot))
                        candidates.Add(Path.Combine(configuredRoot, config, "net10.0-windows", "CJ.Plug.ExecuteApp.exe"));
                    // ③ Services 根 + 配置 + TFM（历史布局 / 05.Release 打包布局）
                    candidates.Add(Path.GetFullPath(Path.Combine(hostDir, "..", "..", config, "net10.0-windows", "CJ.Plug.ExecuteApp.exe")));
                    // ④ 按项目名分目录的布局（BaseOutputPath 若改成带 $(MSBuildProjectName) 也兼容）
                    candidates.Add(Path.GetFullPath(Path.Combine(hostDir, "..", "..", "CJ.Plug.ExecuteApp", config, "net10.0-windows", "CJ.Plug.ExecuteApp.exe")));
                }

                var exePath = candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);

                if (exePath == null)
                {
                    return Results.Problem(
                        detail: "找不到独立执行程序 CJ.Plug.ExecuteApp.exe。已搜索:\n" + string.Join("\n", candidates.Distinct()),
                        statusCode: 500);
                }

                // 获取 Web 前端 (HostWebServer) 的 baseUrl
                // /process/{id}/execute 是 Blazor 页面路由，必须在 HostWebServer 上访问
                // 不能使用 context.Request.Host（那是 ApiServer 自身端口 8687，没有 Blazor 路由）
                var baseUrl = configuration.GetValue<string>("StandaloneExecute:WebFrontendUrl")
                    ?? configuration.GetValue<string>("WebFrontend:Url")
                    ?? GlobalData.MainWebFileServerUrl;

                // 一次性换票码（60s / 单次 / 192 位随机）：
                // AT 有效期 15 分钟，直接塞进 Process.Start 命令行等于把它广播给本机所有进程
                // （Win32_Process.CommandLine 全局可查）。换票码把「可被记录的字符串」压成
                // 「换完即焚的随机值」，AT 全程只在服务端内存流转。
                var ticket = CJCore.Framework.Api.Auth.CJCoreEmbedTicketStore.Issue(accessToken);

                // ⚠ 参数顺序与 MainWindow.ParseCommandLineArgs 对齐：
                //    第 1 个 = baseUrl（可省略），第 2 个 = PlugDefinitionId，具名参数 --ticket= 由前端自行摘走。
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{baseUrl}\" \"{DefinitionId}\" --ticket={ticket}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(psi);

                return Results.Ok(new { success = true, message = $"已启动独立执行程序，流程: {DefinitionId}" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"启动独立执行程序失败: {ex.Message}",
                    statusCode: 500);
            }
        });

        // 系统配置 API
        var sysConfigApi = app.MapGroup("api/systemConfig").WithTags("系统配置");

        sysConfigApi.MapGet("", (IConfiguration configuration) =>
        {
            var appPath = configuration.GetValue<string>("StandaloneExecute:AppPath") ?? "";
            return Results.Ok(new { appPath });
        });

        sysConfigApi.MapPost("", (HttpContext context, IConfiguration configuration) =>
        {
            try
            {
                var body = context.Request.ReadFromJsonAsync<SystemConfigRequest>().Result;
                var newPath = body?.AppPath ?? "";

                var hostDir = AppContext.BaseDirectory;
                var publishSettings = Path.GetFullPath(Path.Combine(hostDir, "appsettings.json"));
                var srcRoot = Path.GetFullPath(Path.Combine(hostDir, "..", "..", "..", "..", "..", "src"));

                WriteAppSetting(publishSettings, "StandaloneExecute:AppPath", newPath);

                var hostName = new DirectoryInfo(Path.GetFullPath(Path.Combine(hostDir, "..", ".."))).Name;
                var srcSettings = Path.Combine(srcRoot, "PlugWebHost", hostName, "appsettings.json");
                if (File.Exists(srcSettings))
                {
                    WriteAppSetting(srcSettings, "StandaloneExecute:AppPath", newPath);
                }

                return Results.Ok(new { success = true, path = newPath });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"保存配置失败: {ex.Message}", statusCode: 500);
            }
        });

        return app;
    }

    private static void WriteAppSetting(string filePath, string keyPath, string value)
    {
        if (!File.Exists(filePath)) return;

        var json = File.ReadAllText(filePath);
        var node = JsonNode.Parse(json);
        if (node == null) return;

        var keys = keyPath.Split(':');
        var current = node;
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (current[keys[i]] == null)
                current[keys[i]] = new JsonObject();
            current = current[keys[i]]!;
        }
        current[keys[^1]] = string.IsNullOrEmpty(value) ? null : value;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, node.ToJsonString(options));
    }

}

internal record SystemConfigRequest(string? AppPath);

/// <summary>
/// 启动独立执行程序的请求体（P5.9 / D-P5-7）。
/// <para><c>AccessToken</c> = 调用方浏览器里的 <c>.ThreeRole.Token</c>（非 HttpOnly，电路 JS 可读）。
/// 端点把它换成 60 秒一次性换票码再传给子进程，AT 本身不出服务端内存。</para>
/// </summary>
public record LaunchStandaloneRequest(string? AccessToken);


