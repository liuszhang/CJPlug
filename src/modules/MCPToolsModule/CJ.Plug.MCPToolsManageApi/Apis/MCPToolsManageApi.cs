using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.Models.MCPTools;
using Microsoft.AspNetCore.Mvc;

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

            return app;
        }
    }
}
