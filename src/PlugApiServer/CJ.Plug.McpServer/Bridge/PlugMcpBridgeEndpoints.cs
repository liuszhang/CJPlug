using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CJ.Plug.McpServer.Services;

namespace CJ.Plug.McpServer.Bridge;

/// <summary>
/// 给 <b>CJCore 侧工具桥</b>用的轻量只读清单（P8：《（设计中）CJPlug接入CJCore实施计划-20261029》8.1）。
///
/// <para><b>为什么要新开这两个 REST 端点，而不是让桥直接走 MCP</b>：本进程对外是标准 MCP
/// (Streamable HTTP / JSON-RPC)，完整客户端侧要先 <c>initialize</c> 协商会话再 <c>tools/list</c>，
/// 还带着 SSE 会话管理 —— 为一个只读清单把整套握手移植到宿主进程，成本高且易碎。
/// 这里在<b>同一个权威源</b>（<see cref="DynamicToolRegistry"/>）上开一扇薄 REST 窗，
/// 清单与执行的语义与 MCP 完全一致（名字经同一个 <c>ToolNameSanitizer</c>，参数经同一个 <c>McpSchemaGenerator</c>）。</para>
///
/// <para><b>端口/暴露面</b>：与 MCP 端点同处<b>唯一宿主 CJPlug.Server（5066）</b> —— 旧口径写的是
/// <c>http://localhost:3001</c>（McpServer 独立进程形态），该进程已于 2026-10-30 下线（P9.1，D2=B）；
/// 现在的完整地址是 <c>http://&lt;host&gt;:5066/api/plugbridge</c>，MCP 协议面则为
/// <c>http://&lt;host&gt;:5066/mcp</c>。不新增端口、不新增对外面。</para>
/// </summary>
public static class PlugMcpBridgeContracts
{
    public const string BasePath = "/api/plugbridge";
    public const string ToolsPath = BasePath + "/tools";
    public const string CallPath = BasePath + "/call";

    public sealed record ToolSummary(
        string Name,
        string Description,
        string ParametersJson,
        /// <summary>发布侧声明的危险标记（原样透给宿主侧 <c>PlugMcpToolExecutor.IsDangerous</c>）。</summary>
        bool IsDangerous);

    public sealed record CallRequest(string Name, string? ArgumentsJson);

    public sealed record CallResponse(string? Text, string? Error);
}

/// <summary>
/// 桥接端点的映射 + JSON 选项（供 Program.cs 一行挂载）。
/// </summary>
public static class PlugMcpBridgeEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPlugMcpBridge(this IEndpointRouteBuilder app)
    {
        // 只读清单：列出当前全部动态（已发布工作流/插头）工具
        app.MapGet(PlugMcpBridgeContracts.ToolsPath, (DynamicToolRegistry registry) =>
        {
            var tools = registry.GetProtocolTools()
                .Select(t => new PlugMcpBridgeContracts.ToolSummary(
                    t.Name,
                    t.Description ?? string.Empty,
                    // InputSchema 是 JsonElement；取回原始 JSON 文本，宿主侧可直接当作 OpenAI function 参数 schema
                    t.InputSchema.ValueKind == JsonValueKind.Undefined
                        ? "{\"type\":\"object\",\"properties\":{}}"
                        : JsonSerializer.Serialize(t.InputSchema, Json),
                    registry.IsDangerous(t.Name)))
                .ToList();

            return Results.Ok(tools);
        });

        // 执行：与 CallToolHandler 同路径 —— 经 DynamicToolRegistry.FindTool → DynamicWorkflowTool.InvokeAsync
        app.MapPost(PlugMcpBridgeContracts.CallPath, async (
            PlugMcpBridgeContracts.CallRequest request,
            DynamicToolRegistry registry,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new PlugMcpBridgeContracts.CallResponse(null, "工具名为空"));

            var tool = registry.FindTool(request.Name);
            if (tool is null)
                return Results.NotFound(new PlugMcpBridgeContracts.CallResponse(null, $"未找到工具: {request.Name}"));

            JsonObject args;
            try
            {
                args = string.IsNullOrWhiteSpace(request.ArgumentsJson)
                    ? new JsonObject()
                    : JsonNode.Parse(request.ArgumentsJson)!.AsObject();
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new PlugMcpBridgeContracts.CallResponse(null, $"参数不是合法 JSON: {ex.Message}"));
            }

            var result = await tool.InvokeAsync(args, ct);

            // 统一抽取出文本内容，避免把 MCP 的 ContentBlock 结构泄漏给宿主侧
            var text = string.Join(
                Environment.NewLine,
                result.Content
                    .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                    .Select(c => c.Text));

            if (result.IsError == true)
                return Results.BadRequest(new PlugMcpBridgeContracts.CallResponse(text, string.IsNullOrWhiteSpace(text) ? "工具执行失败" : text));

            return Results.Ok(new PlugMcpBridgeContracts.CallResponse(text, null));
        });

        return app;
    }
}
