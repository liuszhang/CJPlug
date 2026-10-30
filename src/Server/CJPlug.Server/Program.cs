using CJ.Plug.ApiServer;
// ═══════════════════════════════════════════════════════════════════════════════════
//  CJPlug.Server —— CJPlug 的唯一宿主（P9 / 2026-10-30，D1 = A）
//
//  本文件于 2026-10-30 从 CJ.Plug.ApiServer/Program.cs **原样搬迁**（逐字搬迁，未重写逻辑），
//  搬迁的唯一目的是让"谁负责装配"只有一个答案：
//    · 改之前：exe 叫 ApiServer 却承载全站，装配入口分裂在 ApiServer/Program.cs 与
//              HostWebStartup.cs 两处，边界只写在注释里 ⇒ 部署链按旧形态维护 ⇒ 双宿主事故。
//    · 改之后：本文件 = 装配编排表（按顺序调各模块包的扩展方法 + 固定管道顺序 + 端口契约）；
//              CJ.Plug.ApiServer = 纯类库（能力模块的服务/API 注册）。
//
//  ⚠ 新增业务注册不要去改这个文件：按归属加到对应模块包的扩展方法里
//    （Web 侧 → HostWebStartup；业务模块 → 各模块 ServiceCollectionExtensions）。
//  ⚠ 下游模块包不得反向引用本项目（本项目是叶子节点）。
// ═══════════════════════════════════════════════════════════════════════════════════

using CJ.Plug.ApiServer.Infrastructure;
using CJ.Plug.DispatchServer;
using CJ.Plug.HostWebServer;
using CJ.Plug.HostWebServer.Shell;
using CJ.Plug.McpServer.Bridge;
using CJ.Plug.McpServer.Services;
using CJ.Plug.ElsaIntegration.ApiClient;
using CJ.Plug.ElsaIntegration.Contracts;
using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using Elsa.Extensions;
using Elsa.Studio.Agents.UI.Pages;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlugsBundle;
using Serilog;
using System.Text;
using System.Text.Json.Nodes;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.EnableMultiplexing", false);

// 设置 .NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 环回地址绕过系统代理（2026-10-29，文档 K.7）：本机配了 HTTP_PROXY 时，服务连自己的
// http://localhost:5066 也会被送进代理并拿到 502。幂等，重复调用无害。
LoopbackProxyBypass.Apply();

// 清除 VS 调试器注入的缺失程序集，避免 Hosting startup assembly exception
var hostingAssemblies = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");
if (!string.IsNullOrEmpty(hostingAssemblies))
{
    var cleaned = string.Join(";", hostingAssemblies
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Where(a => !a.Equals("Microsoft.WebTools.ApiEndpointDiscovery", StringComparison.OrdinalIgnoreCase)));
    Environment.SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES",
        string.IsNullOrEmpty(cleaned) ? null : cleaned);
}

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// 服务专属配置（ASPNETCORE_CONFIG_PATH）由 AddServiceDefaults() 统一加载，
// 见 CJCore/src/Framework/CJCore.AspireHost.ServiceDefaults/Extensions.cs 的 LoadServiceScopedConfig。
// 2026-10-07 删除：原先这里把配置键 env 覆盖到 ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT。
//   它发生在 CreateBuilder 之后（host 环境已定型）⇒ 对 host 环境无效，却让"配置里写着 Development"
//   看着能控制环境，并会污染子进程环境。环境名一律交给标准来源（launchSettings / 环境变量；缺省 Production）。
//   背景与修复见《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》§9.7。
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production（默认）"}");

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// ===== 端口 = 架构契约（不再写进 appsettings）=====
// 2026-10-29 Phase 5：合并后本进程同时服务主 API / 调度 Hub / Elsa 引擎 / MCP 面。
//   ⚠ 为什么从配置里删掉 Kestrel 段：所有服务共享同一个输出目录，目录里的 appsettings.json 属于
//     「最后一个构建或打包的服务」，而 JSON 配置是**按键深度合并**的 —— 别的服务加载它就会顺带继承
//     这些端点（实测：Web 宿主因此也去抢主端口并启动失败）。端口改为代码里的默认值 + 环境变量覆盖。
//
// 2026-10-29 第二轮（C 档 + 单端口）：**整个主服务端只监听 5066 一个端口**。
//   全部功能按路径区分，路径归属见下表（无重叠）：
//     /                 Blazor 页面（含 /_blazor circuit、/_framework/*）
//     /elsa/api/*       Elsa 引擎
//     /mcp              MCP 协议面（原挂在根路径，见下方 MapMcp 处的说明）
//     /mainHub         调度 SignalR（原 8686）
//     /api/dispatch/*  调度 REST（原 8686）
//     /api/* /swagger  27 模块业务 API（原 8687）
//     /webFiles/* /plugconfig/*  静态文件
//   ⚠ 端口级门禁 PlugPortGate 随之停用：只有一个端口时它没有区分能力。
//   ⚠ 回退到多端口形态：改回 "http://*:8687;http://*:8686;http://*:5001;http://*:3001"... 并恢复 UsePlugPortGate()。
const string ContractUrls = "http://*:5066";
var overriddenUrls = builder.Configuration["urls"];
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(overriddenUrls) ? ContractUrls : overriddenUrls);


// 2026-10-29《CJPlug 服务架构简化》Phase 2：调度 Hub 改由本进程承载，
//   DispatchServer 不再作为独立进程启动（项目保留，回退时 AppHost 加回一行即可）。
builder.Services.AddSignalR();

//builder.Services.AddSingleton<ILogEventSink>(provider => new SignalRLogSink(provider.GetRequiredService<IHubContext<MainHub>>()));
Log.Logger = new LoggerConfiguration()
    //.WriteTo.File("ApiLogs/log.txt",
    //    rollingInterval: RollingInterval.Day,
    //    rollOnFileSizeLimit: true)
    //.WriteTo.Sink(new InMemorySink())
    //.WriteTo.Sink(new SignalRLogSink(builder.Services.BuildServiceProvider().GetRequiredService<IHubContext<MainHub>>()))
    .WriteTo.Sink(new SignalRLogSink("API"))
    //.WriteTo.Sink(builder.Services.BuildServiceProvider().GetService<ILogEventSink>())
    
    .CreateLogger();


// Add services to the container.
builder.Services.AddProblemDetails();

// 全局 JSON 序列化配置：忽略循环引用
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

//builder.AddNpgsqlDataSource("postgresdb");
//builder.AddNpgsqlDataSource("NpgsqlConnectionString");

// Phase 2/3：保留实例引用 —— Elsa 的两个回调要注册在**这个**实例上。
//   原 ElsaApiServer 用 builder.Services.BuildServiceProvider() 拿服务，那会新建一个容器、
//   再构造出第二个连接管理器（第二个 SignalR 连接），既浪费又会让回调挂在与 DI 不同的实例上。
var dispatchHubConnection = new HubConnectionManagerService();
builder.Services.AddSingleton<HubConnectionManagerService>(dispatchHubConnection);

// Phase 2：把调度面（/mainHub + /api/dispatch/*）挂进本进程。
//   AddDispatchApiService 内部是幂等的：AddSignalR、HubConnectionManagerService、IStationService 均已注册时不会重复登记。
builder.Services.AddDispatchApiService();

// ===== Phase 4：MCP 协议面并入本进程 =====
// 原 CJ.Plug.McpServer 独立进程（3001）的注册整体搬到这里。
// ⚠ 契约更正（2026-10-30，P9.1）：**3001 已经不存在**。原注释写"3001 仍由本进程监听、
//   对外契约不变"，与代码矛盾 —— UseUrls 只有 5066（见本文第 ② 处的端口说明），
//   AppHost 清单也只有 5066 一条。真实的对外契约是 **http://<host>:5066/mcp**
//   （路径由旧独立形态的根路径 "/" 改为 "/mcp"，见下方 app.MapMcp("/mcp")）。
//   6001/3001 一类旧端口表述集中在 PlugPortGate.cs / PlugMcpBridgeEndpoints.cs /
//   PlugMcpToolExecutor.cs，本轮只改注释不改行为。
// ⚠ 未搬运 McpServer 的 AddOpenTelemetry().UseOtlpExporter()：本进程已由 AddServiceDefaults() 统一接管 OTel。
var mcpDispatchUrl = builder.Configuration["DispatchServer:Url"] ?? GlobalData.MainDispatcherServer;
var mcpApiUrl = builder.Configuration["ApiServer:Url"] ?? GlobalData.MainApiServer;

builder.Services.AddHttpClient("DispatchServer", client =>
{
    client.BaseAddress = new Uri(mcpDispatchUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("ApiServer", client =>
{
    client.BaseAddress = new Uri(mcpApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var logger = provider.GetRequiredService<ILogger<DynamicToolRegistry>>();
    return new DynamicToolRegistry(httpClientFactory, logger, mcpDispatchUrl, mcpApiUrl);
});

// 启动后延迟刷新 + 60s 周期兜底（本进程同时是 MCP 服务方与自己的 HTTP 客户端，必须等 Kestrel 起来）。
builder.Services.AddHostedService<PlugMcpToolRefreshService>();

builder.Services.AddMcpServer(options =>
{
    options.Handlers.ListToolsHandler = (request, cancellationToken) =>
    {
        var registry = request.Services!.GetRequiredService<DynamicToolRegistry>();

        // 只返回动态工具（已发布的工作流/插头）；静态 [McpServerTool] 由库默认 handler 从 ToolCollection 返回。
        var dynamicTools = registry.GetProtocolTools();
        return ValueTask.FromResult(new ListToolsResult { Tools = dynamicTools.ToList() });
    };

    options.Handlers.CallToolHandler = async (request, cancellationToken) =>
    {
        var registry = request.Services!.GetRequiredService<DynamicToolRegistry>();
        var toolName = request.Params!.Name;
        var dynamicTool = registry.FindTool(toolName);
        if (dynamicTool != null)
        {
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
// ⚠ 必须显式指定程序集：无参重载扫描的是**调用方**（本 API 程序集），会漏掉 McpServer 里的 [McpServerTool]。
.WithToolsFromAssembly(typeof(DynamicToolRegistry).Assembly);

// ========== 数据访问层 ==========
// ⚠ P5.7（2026-10-29）：原先这里注册 AddDbContextPool<MainDbContext>（IdentityDbContext，
//   承载 User/UserRole/UserGroup/AspNet* 共 7 张身份表 + IModuleDbConfig 动态拼装）。
//   旧身份体系（Login/UserManage/Auth 四模块）已物理删除，业务表改由 CJCore Data 引擎
//   （DataDbContext + 快照记账）负责 ⇒ MainDbContext 连同 User/UserRole/UserGroup/
//   UserGroupMember 实体、Migrations/MainDb 迁移快照一并移除。
//   数据库文件里的 Users/AspNet* 等旧表**不做自动清理**（只停止映射），需人工评估后再删，
//   避免误删现场数据。
//   认证与用户/角色/部门改由 CJCore.Modules.ThreeRoleAdmin（TRA_* 表）接管。

//自定义插头的执行注册
builder.Services.AddPlugsExecutebundle();
builder.Services.AddXmlConfiguredExecuteServices();

builder.Services.AddAntiforgery();

builder.Services.AddEndpointsApiExplorer();
// 添加 NSwag 服务
builder.Services.AddOpenApiDocument(configure =>
{
    configure.Title = "Main API";
});

// 添加CORS服务
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加各模块服务
builder.Services.ConfigModuleApiServices();
// 独立认证服务由 CJCore 三员模块提供（P5.6：旧 AddLoginModuleApiServices 已随 LoginModule 删除）

builder.Services.AddSingleton<MainApiClient>();
//builder.Services.AddHttpClient<MainApiClient>(client =>
//{
//    client.BaseAddress = new(GlobalData.MainDispatcherServer);
//    client.Timeout = TimeSpan.FromSeconds(60);
//});
builder.Services.AddHttpClient<ElsaApiClient>(client =>
{
    client.BaseAddress = new(GlobalData.MainDispatcherServer);
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Phase 2：MCP Tool 变更通知改为**进程内一跳**。
//   模块默认注册 HttpMcpToolChangeNotifier（POST /api/dispatch/notifyMcpToolUpdated → DispatchServer → SignalR 广播），
//   合并后 Hub 就在本进程，直接广播即可；后注册者胜出，故此处覆盖。
//   ⚠ 同时修掉一个既有缺陷：HttpMcpToolChangeNotifier 依赖命名 HttpClient「DispatchServer」，
//     而 ApiServer 从未注册过该命名客户端 ⇒ CreateClient 拿到无 BaseAddress 的默认实例，
//     相对 URI 请求必然抛异常并被 catch 吞掉 ⇒ 发布工具后 McpServer 一直收不到刷新通知。
builder.Services.AddSingleton<CJ.Plug.Models.MCPTools.IMcpToolChangeNotifier, CJ.Plug.ApiServer.Hubs.SignalRMcpToolChangeNotifier>();

// 添加认证服务
// 2026-10-29 第二轮：这里原本是裸 AddAuthentication()（无 scheme）。合并 Blazor Web 宿主后，
//   WebHostStartup.AddPlugWebHost() 会调用 AddAuthentication(PlugTempAuthHandler.Scheme).AddScheme<...>()，
//   那才是本进程唯一的认证方案来源（CJCore 管理端点 / Elsa 端点都依赖它）。
//   两处都调会让「无 scheme 的核心服务注册」跑在「带 scheme 的注册」之前 —— 虽然结果相同，
//   但阅读时容易误以为默认方案来自这里。故只保留 Web 扩展里那一处。
//builder.Services.AddAuthentication();

// 2026-10-29《CJPlug 服务架构简化》Phase 3：Elsa 引擎并入本进程。
//   原为 ConfigElsaServicesWithOutDB()（只注册活动/表达式，无 WorkflowManagement/Runtime 的 EF store），
//   引擎由独立的 CJ.Plug.ElsaApiServer 进程（5001）承载。
//   合并后改用带库版本（main-elsa.db），本进程同时是"编辑面"与"执行面"。
//   ⚠ 回退：换回 ConfigElsaServicesWithOutDB() + AppHost 起回 elsaapiserver 资源 + 删下面的回调与 UseElsaEndpoints()。
builder.AddElsaServicesForApi();

// ===== 2026-10-29《CJPlug 服务架构简化》第二轮（C 档）：Blazor Web 宿主并入本进程 =====
// 原 CJ.Plug.HostWebServer 是独立进程（5066）。现在它的全部 DI 注册搬到这里，
// 请求管道由下方 MapPlugWebHost() / MapPlugWebRazorComponents() 接入同一张路由表。
//
// ⚠ Elsa 现在是「同进程双栈」：
//   AddElsaServicesForApi()  —— 引擎（AddElsa + WorkflowManagement/Runtime 的 EF store，本进程自执行）
//   AddPlugWebHost() 内部的 AddElsaServicesForWeb() —— Studio 前端（AddCore + RemoteBackend，仍走 HTTP 调自己）
//   两套共存的关键是「引擎用本地实现、Studio 用远程客户端实现」，类型名相同但命名空间不同。
//   ⚠ 回归重点：流程编辑器「组件库」是否仍能加载（历史上此处的 401 会让组件库空白）。
builder.AddPlugWebHost();

// Phase 3：把原 ElsaApiServer 的两个 SignalR 回调搬到本进程。
//   回调由 MainHub 广播触发；执行期才需要 DI，故用 captured 变量延迟绑定到 app.Services（Build 之后赋值）。
//   ResumeElsaProcess       —— 工位执行完插件后恢复工作流，消息格式 "前缀:instanceId-definitionId-correlationId"
//   CompleteActivityContext —— 插件执行完成回调，消息格式 "correlationId|plugId"
IServiceProvider? appServices = null;

dispatchHubConnection._hubConnection.On<string>("ResumeElsaProcess", async bookmarkInfo =>
{
    try
    {
        var info = bookmarkInfo.Split(':')[1];
        var instanceId = info.Split('-')[0];
        var definitionId = info.Split('-')[1];
        var correlationId = info.Split('-')[2];
        var bookmarkId = correlationId + definitionId;

        var sp = appServices ?? throw new InvalidOperationException("app.Services 尚未就绪");
        var workflowRuntime = sp.GetRequiredService<IWorkflowRuntime>();
        var client = await workflowRuntime.CreateClientAsync(instanceId);

        // 只保留原实现里**实际生效**的「方式一（RunInstanceAsync）」，其余两段是注释死码，不再搬运。
        await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmarkId });

        Log.Information("恢复流程执行: {InstanceId}", instanceId);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "继续执行流程失败: {Message}", ex.Message);
    }
});

dispatchHubConnection._hubConnection.On<string>("CompleteActivityContext", async activityContext =>
{
    try
    {
        if (string.IsNullOrEmpty(activityContext))
        {
            Log.Warning("CompleteActivityContext 收到空消息");
            return;
        }

        var parts = activityContext.Split('|');
        if (parts.Length < 2)
        {
            Log.Warning("CompleteActivityContext 消息格式错误: {Ctx}", activityContext);
            return;
        }

        var correlationId = parts[0];
        var plugId = parts[1];
        var bookmarkId = correlationId + plugId;

        var sp = appServices ?? throw new InvalidOperationException("app.Services 尚未就绪");

        var instanceStore = sp.GetRequiredService<IWorkflowInstanceStore>();
        var instances = await instanceStore.FindManyAsync(new Elsa.Workflows.Management.Filters.WorkflowInstanceFilter { CorrelationId = correlationId });
        var instance = instances?.FirstOrDefault();
        if (instance == null)
        {
            Log.Warning("未找到 CorrelationId={Cid} 的工作流实例", correlationId);
            return;
        }

        // 用 IWorkflowResumer 直接恢复书签，避免 RunInstanceAsync 触发工作流重新反序列化（原实现注释所述）。
        var resumer = sp.GetRequiredService<IWorkflowResumer>();
        var resumed = await resumer.ResumeAsync(
            new BookmarkFilter
            {
                BookmarkId = bookmarkId,
                WorkflowInstanceId = instance.Id,
                CorrelationId = correlationId
            },
            new ResumeBookmarkOptions { Input = null, Properties = null });

        // ⚠ 原实现写的是 result.ToString() == "True"，而 ResumeAsync 返回的是
        //   IEnumerable<RunWorkflowInstanceResponse> ⇒ ToString() 永远是非空字符串 ⇒ 日志恒为"成功"，属假信号。
        //   这里按实际匹配数量记日志。
        var resumedCount = resumed?.Count() ?? 0;
        Log.Information("书签恢复匹配 {Count} 个: {BookmarkId}", resumedCount, bookmarkId);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "CompleteActivityContext 处理失败: {Message}", ex.Message);
    }
});




var app = builder.Build();

// 标记「本进程已开始监听」，供启动期自调用判断使用（2026-10-29，文档 K.8.4）。
// 在此之前发起的 HTTP 自调用必然 Connection refused，调用方应跳过而不是硬试。
app.Lifetime.ApplicationStarted.Register(CJ.Plug.Models.Shared.HostStartupSignal.MarkStarted);

// Phase 3：回调的 DI 延迟绑定（上面的 ResumeElsaProcess / CompleteActivityContext 在运行期才解析服务）。
appServices = app.Services;

// ★ 注册插件能力到 CapabilityRegistry（供 AI Workflow Builder 使用）
try
{
    var capRegistry = app.Services.GetRequiredService<CJ.Plug.Models.MCPTools.CapabilityRegistry>();
    capRegistry.RegisterRange(new CJ.Plug.Models.MCPTools.IPluginCapability[]
    {
        new RESTPlug.Capabilities.HttpPluginCapability(),
        new PythonPlug.Capabilities.PythonPluginCapability(),
        new CMDPlug.Capabilities.CmdPluginCapability(),
        new AiAgentPlug.Capabilities.AiAgentPluginCapability(),
    });
    Console.WriteLine("[Startup] 插件能力注册完成");
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] 插件能力注册异常: {ex.Message}");
}

// ⚠ P5.7（2026-10-29）：原先这里在 Run() 之前对 MainDbContext 做 EnsureCreatedOnce
//   （目的是先建好 Identity 的 7 张身份表，再让 Data 引擎建业务表）。
//   旧身份四模块已删除、MainDbContext/User/UserRole 已移除 ⇒ 本段整体去掉。
//   身份/权限表的建表与播种完全交给 CJCore 三员模块（TRA_* + main_audit.db）。
//   业务表的建表 + 播种仍由 CJCore Data 引擎（AddCJCoreDataApi 的 InitAction）保证顺序。

// 种子数据**已移出**此处（P4 / 2026-10-29）：
//   原来在这里直接 SeedDataRunner.RunAllAsync(app.Services) —— 有两个问题：
//   ① 时序错：此刻业务表还没建（建表是 Data 引擎的 HostedService，Run() 才启动）；
//   ② 容器错：传的是根容器，而种子提供者会解析 Scoped 服务 ⇒ "Cannot resolve scoped service"。
//   现改由 AddCJCoreDataApi 的 InitAction 触发（WebHostStartup），引擎保证「结构就绪 → 播种」顺序，
//   并在内部自建 scope。

app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Phase 4：端口级路由闸门。
//   2026-10-29 第二轮（单端口收敛）后**已停用** —— 只剩一个端口时端口不再具备区分能力，
//   安全边界回落到路径（CJPlug 本来无鉴权，不构成能力降级）。类保留，便于回退到多端口形态。
//app.UsePlugPortGate();

//添加验证中间件
app.UseAuthentication();

// ── P5.8 方案 C2：内部密钥入站闸门（2026-10-29）──────────────────────────
// 只认「请求头 X-Internal-Key 是否等于配置密钥」，用于后续给业务 /api/* 收口。
// ⚠ 默认 Observe = **只统计不拦截**。Universe 顺序要点：
//   ① 必须在 UseAuthentication 之后（Enforce 时不会误伤已认证的用户请求，口径与后续授权一致）；
//   ② 必须在 UseRouting 之前 —— 本中间件不依赖 endpoint，早挂早统计，避免被端点级短路跳过；
//   ③ InternalApi:Key 为空时中间件**完全不挂**（零开销、零行为改变）。
// 收口依据：Observe 跑一轮后看日志里 "[InternalKey] 业务 API 请求未携带内部密钥" 的清单，
//   逐条补齐出站客户端后再把 GateMode 改成 Enforce。
app.UseInternalApiKeyGate(
    app.Configuration["InternalApi:GateMode"],
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("InternalApiKeyGate"));

// 必须先调用 UseRouting
app.UseRouting();

// 启用CORS
app.UseCors("AllowAll");

// 授权收敛（2026-10-29 第二轮：判据从「端口」改为「路径」）
//   背景一（Phase 3）：Elsa 端点带 [Authorize]，缺 AuthorizationMiddleware 直接 500；
//     但全局启用会把按 Roles 过滤的菜单筛成空（实测：首页 mud-nav 容器在、菜单项全丢）。
//   背景二（第二轮）：单端口后所有功能共用 5066，端口不再是区分手段 ⇒ 只能按路径判断。
//   判据见 WebHostStartup.NeedsIdentity：页面 / Elsa / CJCore 管理端点需要身份，
//     业务 API(/api/*) / 调度 Hub(/mainHub) / MCP(/mcp) 保持「无认证身份」的既有行为。
app.UseWhen(
    context => WebHostStartup.NeedsIdentity(context.Request.Path),
    branch => branch.UseAuthorization());

app.UseAntiforgery();
app.UseWebSockets(); // 添加 WebSocket 支持 (VNC/SSH 代理)

// ===== Web 宿主的中间件 + 端点（静态文件 / OutputCache / 静态资源 / CJCore 管理端点 / 远程桌面 API）=====
// ⚠ 必须在第一个业务 Map 之前调用：内部含 UseStaticFiles / UseOutputCache 两个中间件，
//   若在 Map 之后调用，中间件会被插到自动追加的 UseEndpoints 之后而失效。
app.MapPlugWebHost();

//添加各模块的注入端点
app.ConfigModuleApis();

// Phase 2：调度面端点（/mainHub + /api/dispatch/*）—— 原 DispatchServer 独立进程的全部暴露面。
//   ⚠ 工位机/Electron/Desktop 连的都是 /mainHub，合并后由本进程在同一端口提供。
app.MapDispatchServiceEndpoints();

// Phase 3：Elsa 引擎端点（原由 5001 的 ElsaApiServer 提供，路径仍是 /elsa/api）。
//   ⚠ 必须在 UseRouting 之后；端口级隔离由 PlugPortGate 中间件负责（5001 之外不暴露 /elsa/*）。
app.UseElsaEndpoints();

// Phase 4：MCP 面。
//   /api/plugbridge/* —— 给 CJCore 工具桥用的薄 REST 窗（清单 + 执行）
//   MapMcp("/mcp")    —— MCP 协议本体。⚠ 不能挂根路径：Blazor 首页占着 "/"，
//                        同 pattern 两个端点会抛 AmbiguousMatchException（实测 3001 根路径原为 405）。
app.MapPlugMcpBridge();
app.MapMcp("/mcp");

// ── P5.8 C2 观测端点 ──────────────────────────────────────────────────────
// 给出「业务 API 请求里带内部密钥的比例」。这是把 GateMode 从 Observe 推到 Enforce 的**唯一依据**：
// coverage 必须 100% 且 missingPaths 为空，否则开 Enforce 就是全站 401。
//
// ⚠ 不用 IsDevelopment() 做开关（2026-10-29 实测踩到）：本仓在 ASPNETCORE_ENVIRONMENT=Development 下
//   **根本起不来** —— 开发环境开启 DI 作用域验证后暴露两处存量生命周期缺陷
//   （AiWorkflowBuilderService[Singleton]→IDeepSeekService[Scoped]、PermissionFunctionsService[Singleton]→IMenuService[Scoped]），
//   容器构建即抛 AggregateException。也就是说 IsDevelopment() 分支在本仓是**永远走不到的死代码**。
// ⚠ 替代保护：端点自身要求携带内部密钥；不匹配一律 404（连"端点存在"都不告诉探测者）。
//   运维查覆盖率时用同一把钥匙 curl 一次即可。
app.MapGet("/api/diagnostics/internal-key", (HttpContext http) =>
{
    var presented = http.Request.Headers[InternalApiKey.HeaderName].ToString();
    if (!InternalApiKey.Matches(presented))
    {
        return Results.NotFound();
    }

    var snap = InternalApiKeyGate.Snapshot();
    var readTotal = snap.ReadWithKey + snap.ReadWithoutKey;
    var writeTotal = snap.WriteWithKey + snap.WriteWithoutKey;
    var missingWrite = snap.Missing.Where(kv => kv.Key.StartsWith("W:", StringComparison.Ordinal)).ToDictionary();
    var missingRead = snap.Missing.Where(kv => kv.Key.StartsWith("R:", StringComparison.Ordinal)).ToDictionary();

    return Results.Ok(new
    {
        gateMode = app.Configuration["InternalApi:GateMode"] ?? "(unset)",
        keyConfigured = InternalApiKey.IsEnabled,

        // 写侧是 EnforceWrite 的判定依据（必须 100% 且 missingWrite 为空）
        write = new
        {
            withKey = snap.WriteWithKey,
            withoutKey = snap.WriteWithoutKey,
            requestTotal = writeTotal,
            coverage = snap.WriteCoverage is null ? "n/a" : $"{snap.WriteCoverage:F2}%",
            missingPaths = missingWrite,
        },

        // 读侧是全量 Enforce 的判定依据
        read = new
        {
            withKey = snap.ReadWithKey,
            withoutKey = snap.ReadWithoutKey,
            requestTotal = readTotal,
            coverage = snap.ReadCoverage is null ? "n/a" : $"{snap.ReadCoverage:F2}%",
            missingPaths = missingRead,
        },
    });
}).AllowAnonymous();

// Phase 4：MCP 的动态工具缓存初始化 + 静态工具的 ApiServer 地址注入（原 McpServer 的启动段）。
//   ⚠ RefreshAsync 走 HTTP 调**本进程自己**，不能在 app.Run() 之前 await（Kestrel 未绑定必失败），
//     改由 PlugMcpToolRefreshService 在启动后延迟执行，并附带 60s 周期兜底（修既有「只刷新一次」缺陷）。
WorkflowTools.Configure(mcpApiUrl);
FileTools.Configure(mcpApiUrl);

// ⚠ 必须是最后一个端点映射：框架 NotFoundPage 的 catch-all（/{**path}）会吃掉所有未匹配路由。
app.MapPlugWebRazorComponents();

//aspire相关服务
app.MapDefaultEndpoints();


app.Run();

