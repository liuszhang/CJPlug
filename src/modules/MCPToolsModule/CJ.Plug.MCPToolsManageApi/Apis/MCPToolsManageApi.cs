using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.Models.MCPTools;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CJ.Plug.MCPToolsManageApi.Apis
{
    public static class MCPToolsManageApi
    {
        public static IEndpointRouteBuilder MapMCPToolsManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/mcp").WithTags("MCP管理");

            api.MapGet("/getTools", async (IMCPToolsManageService service) => await service.GetAllAsync());
            api.MapGet("/getActiveTools", async (IMCPToolsManageService service) => await service.GetActiveToolsAsync());
            api.MapGet("/getPublishedWorkflows", async (IMCPToolsManageService service) => await service.GetPublishedWorkflowsAsync());
            api.MapPost("/addTool", async (IMCPToolsManageService service, [FromBody] MCPTool request) => await service.PublishToolAsync(request));
            api.MapPut("/updateTool", async (IMCPToolsManageService service, [FromBody] MCPTool request) => await service.UpdateAsync(request));
            api.MapDelete("/deleteTool/{toolId}", async (IMCPToolsManageService service, int toolId) => await service.DeleteAsync(toolId));

            // 通知 McpServer 刷新工具列表
            api.MapPost("/notifyRefresh", async (IMcpToolChangeNotifier? notifier) =>
            {
                if (notifier != null)
                    await notifier.NotifyAsync("*", "refresh");
                return Results.Ok();
            });

            // 配置 Trae MCP 配置文件
            api.MapGet("/config/trae/preview", async (IMCPToolsManageService service) =>
            {
                var (content, filePath) = await service.PreviewTraeMcpAsync();
                return Results.Ok(new { content, filePath });
            })
            .WithName("PreviewTraeMcp")
            .WithDescription("读取 Trae MCP 配置文件当前内容");

            api.MapPost("/config/trae", async (IMCPToolsManageService service, [FromBody] ConfigureTraeRequest request) =>
            {
                var message = await service.ConfigureTraeMcpAsync(request.ConfigContent);
                return Results.Ok(new { success = true, message });
            })
            .WithName("ConfigureTraeMcp")
            .WithDescription("将用户编辑的 JSON 内容覆盖写入 Trae MCP 配置文件");

            api.MapPost("/config/enable", async (IMCPToolsManageService service) =>
            {
                var message = await service.EnableTraeMcpAsync();
                return Results.Ok(new { success = true, message });
            })
            .WithName("EnableTraeMcp")
            .WithDescription("一键将固定 cj-mcpserver 配置合并写入 Trae MCP 配置文件");

            // 配置 Claude Code MCP 配置文件
            api.MapGet("/config/claude/preview", async (IMCPToolsManageService service) =>
            {
                var (content, filePath) = await service.PreviewClaudeMcpAsync();
                return Results.Ok(new { content, filePath });
            })
            .WithName("PreviewClaudeMcp")
            .WithDescription("读取 Claude Code MCP 配置文件当前内容");

            api.MapPost("/config/claude", async (IMCPToolsManageService service, [FromBody] ConfigureTraeRequest request) =>
            {
                var message = await service.ConfigureClaudeMcpAsync(request.ConfigContent);
                return Results.Ok(new { success = true, message });
            })
            .WithName("ConfigureClaudeMcp")
            .WithDescription("将用户编辑的 JSON 内容覆盖写入 Claude Code MCP 配置文件");

            // 通用自定义路径 MCP 配置
            api.MapGet("/config/custom/preview", async (IMCPToolsManageService service, string filePath) =>
            {
                var (content, fp) = await service.PreviewMcpAsync(filePath);
                return Results.Ok(new { content, filePath = fp });
            })
            .WithName("PreviewCustomMcp")
            .WithDescription("读取指定路径的 MCP 配置文件");

            api.MapPost("/config/custom", async (IMCPToolsManageService service, [FromBody] ConfigureCustomRequest request) =>
            {
                var message = await service.ConfigureMcpAsync(request.FilePath, request.ConfigContent);
                return Results.Ok(new { success = true, message });
            })
            .WithName("ConfigureCustomMcp")
            .WithDescription("将用户编辑的 JSON 覆盖写入指定路径的 MCP 配置文件");

            // 文件选择对话框
            api.MapGet("/config/pick-file", () =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $d=new-object System.Windows.Forms.OpenFileDialog; $d.Filter='JSON files (*.json)|*.json|All files (*.*)|*.*'; if($d.ShowDialog() -eq 'OK'){$d.FileName}else{''}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = System.Diagnostics.Process.Start(psi)!;
                    var output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    return Results.Ok(new { filePath = output });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { filePath = "", error = ex.Message });
                }
            })
            .WithName("PickConfigFile")
            .WithDescription("打开 Windows 文件选择对话框，返回用户选中的 JSON 文件路径");

            // MCP 配置路径持久化
            api.MapGet("/config/path/{key}", async (IMCPToolsManageService service, string key) =>
            {
                var filePath = await service.GetConfigPathAsync(key);
                return Results.Ok(new { filePath });
            })
            .WithName("GetConfigPath")
            .WithDescription("获取已持久化的 MCP 配置文件路径");

            api.MapPost("/config/path/{key}", async (IMCPToolsManageService service, string key, [FromBody] JsonElement body) =>
            {
                var filePath = body.TryGetProperty("filePath", out var f) ? f.GetString() ?? "" : "";
                await service.SaveConfigPathAsync(key, filePath);
                return Results.Ok(new { success = true });
            })
            .WithName("SaveConfigPath")
            .WithDescription("持久化 MCP 配置文件路径");

            return app;
        }
    }
}
