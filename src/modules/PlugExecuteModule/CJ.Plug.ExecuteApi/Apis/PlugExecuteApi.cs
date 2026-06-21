using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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

                // 获取当前 Blazor Server 的 baseUrl
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

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

        return app;
    }

}

