using System.Text;
using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.HostWebServer;
using Serilog;

// ============ 2026-10-29《CJPlug 服务架构简化》第二轮 ============
// 本文件已退化为**薄壳**：真实的服务注册与请求管道搬到了 WebHostStartup.cs。
//
// 为什么还要保留这个可执行入口：
//   合并后（CJ.Plug.ApiServer 引用本项目的三个扩展方法）它不再被 AppHost 启动，
//   但保留下来 = **回退路径**：AppHost 里把 webfrontend 资源取消注释即可恢复独立进程，
//   且走的是与合并形态完全相同的代码（不是另一份实现），所以回退不会引入行为差异。
//
// ⚠ 不要在这里加业务注册 —— 加在 WebHostStartup 里，否则合并形态收不到。

//设置.NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(new SignalRLogSink("Front"))
    .CreateLogger();

// 2026-10-07 删除：原先这里把配置键 env 覆盖到 ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT。
//   它发生在 CreateBuilder 之后（host 环境已定型）⇒ 对 host 环境无效，却让"配置里写着 Development"
//   看着能控制环境，并会污染子进程环境。环境名一律交给标准来源（launchSettings / 环境变量；缺省 Production）。
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production（默认）"}");

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// 2026-10-29 Phase 5：端口 = 架构契约，写死在代码里而不是 appsettings。
//   共享输出目录的 appsettings.json 属于「最后一个构建/打包的服务」，而 JSON 配置按键深度合并 ——
//   实测：Web 宿主读到主宿主的端点后会一起去抢，直接启动失败。ASPNETCORE_URLS 仍可覆盖。
const string ContractUrls = "http://*:5066";
var overriddenUrls = builder.Configuration["urls"];
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(overriddenUrls) ? ContractUrls : overriddenUrls);

// 全部业务注册（MudBlazor / CJCore 框架 / Elsa Studio / 模块壳 / A2A / Agent）
builder.AddPlugWebHost();

var app = builder.Build();

app.UseRouting();
app.UseWebSockets(); // WebSocket 支持 (VNC/SSH 远程桌面代理) — 必须在 endpoint 之前
// CJCore 端点（P6 /api/module-admin/*、P7 /api/system-config/*）带 RequireAuthorization 元数据，
//   缺这两个中间件时请求会直接抛 InvalidOperationException / 一律 401。
//   P5 之前认证由 PlugTempAuthHandler（恒 Admin 的过渡件）提供。
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDefaultEndpoints();

// 静态文件 + 输出缓存 + 静态资源 + CJCore 管理端点
app.MapPlugWebHost();

// 独立启动形态没有 ConfigModuleApis()（那是 ApiServer 的模块 API 扫描入口），
// 远程桌面代理端点（/api/remote/vnc、/api/remote/ssh 等）要自己补一次。
// ⚠ 合并形态下由 ApiServer 的 ConfigModuleApis() 负责，这里重复调会因端点名重复导致全站 500。
app.MapRemoteDesktopApi();

// ⚠ 最后：Razor 根组件（带 catch-all，必须压在所有业务端点之后）
app.MapPlugWebRazorComponents();

app.Run();
