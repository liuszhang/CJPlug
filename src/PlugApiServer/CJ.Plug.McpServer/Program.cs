using System.Text.Json;
using System.Text.Json.Nodes;
using CJ.Plug.McpServer.Services;
using CJ.Plug.Models.Shared;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ★ DispatchServer (SignalR hub) 和 ApiServer (HTTP API) 分开配置
var dispatchUrl = builder.Configuration["DispatchServer:Url"] ?? GlobalData.MainDispatcherServer;
var apiUrl = builder.Configuration["ApiServer:Url"] ?? GlobalData.MainApiServer;

// DispatchServer HttpClient (用于 SignalR 连接)
builder.Services.AddHttpClient("DispatchServer", client =>
{
    client.BaseAddress = new Uri(dispatchUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ApiServer HttpClient (用于 MCP Tools API 调用)
builder.Services.AddHttpClient("ApiServer", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ★ 注册动态工具服务
builder.Services.AddSingleton(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var logger = provider.GetRequiredService<ILogger<DynamicToolRegistry>>();
    return new DynamicToolRegistry(httpClientFactory, logger, dispatchUrl, apiUrl);
});

// MCP Server 配置 —— 自定义 Handlers 实现动态工具注册
builder.Services.AddMcpServer(options =>
{
    options.Handlers.ListToolsHandler = async (request, cancellationToken) =>
    {
        var registry = request.Services!.GetRequiredService<DynamicToolRegistry>();

        // 只返回动态工具（已发布的工作流/插头）。
        // 静态工具（[McpServerTool] 标注的工具）由库的默认 handler 从 ToolCollection 返回，
        // 这里不再重复包含，避免出现一模一样的重复 tool。
        var dynamicTools = registry.GetProtocolTools();

        return new ListToolsResult { Tools = dynamicTools.ToList() };
    };

    options.Handlers.CallToolHandler = async (request, cancellationToken) =>
    {
        var registry = request.Services!.GetRequiredService<DynamicToolRegistry>();
        var toolName = request.Params!.Name;

        var dynamicTool = registry.FindTool(toolName);
        if (dynamicTool != null)
        {
            // 将 IDictionary<string, JsonElement> 转为 JsonObject
            var argsObj = new JsonObject();
            if (request.Params.Arguments != null)
            {
                foreach (var kvp in request.Params.Arguments)
                {
                    argsObj[kvp.Key] = JsonNode.Parse(kvp.Value.GetRawText());
                }
            }
            return await dynamicTool.InvokeAsync(argsObj, cancellationToken);
        }

        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Tool not found: {toolName}" }
            },
            IsError = true
        };
    };
})
.WithHttpTransport()
.WithToolsFromAssembly();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();

var app = builder.Build();

// ★ 启动时刷新动态工作流缓存，并连接 SignalR 接收实时推送
var registry = app.Services.GetRequiredService<DynamicToolRegistry>();
await registry.RefreshAsync();
await registry.StartAsync();

// ★ 配置 WorkflowTools 的 ApiServer 地址（供静态方法调用 MCP Tools API）
WorkflowTools.Configure(apiUrl);

// ★ 配置 FileTools 的 ApiServer 地址（MCP 文件上传/查询工具）
FileTools.Configure(apiUrl);

app.MapMcp();
app.Run("http://localhost:3001");
