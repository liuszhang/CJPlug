using CJ.Plug.McpServer.Services;
using CJ.Plug.Models.Shared;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ★ 注册 HTTP Client 用于调用 DispatchServer
var dispatchUrl = builder.Configuration["DispatchServer:Url"] ?? GlobalData.MainApiServer;
builder.Services.AddHttpClient("DispatchServer", client =>
{
    client.BaseAddress = new Uri(dispatchUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ★ 注册动态工具服务
builder.Services.AddSingleton<DynamicToolRegistry>();

// MCP Server 配置
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();   // 自动扫描 WorkflowTools + 现有的 JobsTool/AdaptersTool

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

// ★ 启动时刷新动态工作流缓存
var registry = app.Services.GetRequiredService<DynamicToolRegistry>();
await registry.RefreshAsync();

// ★ 配置 WorkflowTools 的 DispatchServer 地址（供静态方法使用）
WorkflowTools.Configure(dispatchUrl);

app.MapMcp();
app.Run("http://localhost:3001");
