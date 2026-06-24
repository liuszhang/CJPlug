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

        api.MapPost("/launchStandalone/{DefinitionId}", async (HttpContext context, string DefinitionId, IConfiguration configuration) =>
        {
            try
            {
                // 从当前宿主程序集位置推导 ExecuteApp 的 exe 路径
                // 项目使用 BaseOutputPath=..\..\..\02.Publish\$(MSBuildProjectName)，无 bin 层
                var hostDir = AppContext.BaseDirectory;
                var exeRelativePath = Path.Combine("..", "..", "..", "CJ.Plug.ExecuteApp");

                // 优先从配置读取路径，否则自动推导
                var exeRoot = configuration.GetValue<string>("StandaloneExecute:AppPath")
                    ?? Path.GetFullPath(Path.Combine(hostDir, exeRelativePath));

                // 尝试 Debug / Release 两种构建配置
                string? exePath = null;
                foreach (var config in new[] { "Debug", "Release" })
                {
                    var candidate = Path.Combine(exeRoot, config, "net10.0-windows", "CJ.Plug.ExecuteApp.exe");
                    if (File.Exists(candidate))
                    {
                        exePath = candidate;
                        break;
                    }
                }

                if (exePath == null)
                {
                    return Results.Problem(
                        detail: $"找不到独立执行程序。已搜索: {exeRoot}/[Debug|Release]/net10.0-windows/CJ.Plug.ExecuteApp.exe",
                        statusCode: 500);
                }

                // 获取 Web 前端 (HostWebServer) 的 baseUrl
                // /process/{id}/execute 是 Blazor 页面路由，必须在 HostWebServer 上访问
                // 不能使用 context.Request.Host（那是 ApiServer 自身端口 8687，没有 Blazor 路由）
                var baseUrl = configuration.GetValue<string>("StandaloneExecute:WebFrontendUrl")
                    ?? configuration.GetValue<string>("WebFrontend:Url")
                    ?? GlobalData.MainWebFileServerUrl;

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{baseUrl}\" \"{DefinitionId}\"",
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

