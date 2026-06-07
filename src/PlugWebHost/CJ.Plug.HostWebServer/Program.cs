using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.Login;
using CJ.Plug.MainPageContent;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using Elsa.Extensions;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using MudExtensions.Services;
using PlugsBundle;
using Radzen;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;


//设置.NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    //.WriteTo.File("FrontLogs/log.txt",
    //    rollingInterval: RollingInterval.Day,
    //    rollOnFileSizeLimit: true)
    //.WriteTo.Sink(new InMemorySink())
    //.WriteTo.Sink(new SignalRLogSink(builder.Services.BuildServiceProvider().GetRequiredService<IHubContext<MainHub>>()))
    .WriteTo.Sink(new SignalRLogSink("Front"))
    //.WriteTo.Sink(builder.Services.BuildServiceProvider().GetService<ILogEventSink>())

    .CreateLogger();
//builder.Services.AddSingleton<IErrorBoundaryLogger, CustomErrorBoundaryLogger>();


var configuration = builder.Configuration;
//builder.WebHost.UseStaticWebAssets();
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

// 显式加载静态 Web 资产清单（Build 模式而非 Publish 时需要）
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();


// Add service defaults & Aspire components.
builder.AddServiceDefaults();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    options.RootComponents.RegisterCustomElsaStudioElements();
});



// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize to handle large workflows.
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

//builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
//builder.Services.AddMediatR(typeof(Program).Assembly);

// 注册 EventAggregator 服务
builder.Services.AddEventAggregator();

//添加ELSA流程引擎相关服务注册
builder.AddElsaServicesForWeb();

builder.Services.AddScoped<CJ.Plug.Models.Contracts.IAppBarService, DefaultAppBarService>();
builder.Services.ConfigModulePageServices();

// VNC/SSH 远程桌面 WebSocket 代理服务
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.VncWebSocketProxy>();
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.SshWebSocketProxy>();
builder.Services.AddSingleton<CJ.Plug.GuacamoleApi.Services.CaptureWebSocketProxy>();

//添加插头前端集合包依赖
builder.Services.AddPlugsBundle();
builder.Services.AddXmlConfiguredServices();
builder.Services.AddXmlConfiguredExecuteServices();


//添加Radzen控件服务
builder.Services.AddRadzenComponents();


builder.Services.AddOutputCache();

//其他服务配置
builder.Services.ConfigOtherServices();



// 开启详细错误信息
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
});



var app = builder.Build();

var serviceProvider = builder.Services.BuildServiceProvider();
// 获取所有注册的 IModule 实例
var modules = serviceProvider.GetRequiredService<IEnumerable<IModule>>().ToList();
var moduleAssemblies = modules.Select(m => m.GetType().Assembly).Distinct().ToList();
foreach (var module in modules)
{
    await module.InitializeAsync();
    Console.WriteLine($"Load Module:{module.GetType()}");
}


if (!app.Environment.IsDevelopment())
{
    Console.WriteLine("------------Production mode--------------");
    //app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseRouting();
app.UseWebSockets(); // WebSocket 支持 (VNC/SSH 远程桌面代理) — 必须在 endpoint 之前
//app.UseAuthentication(); // If you are using authentication
//app.UseAuthorization();  // If you are using authorization
app.UseAntiforgery();    // Add this line
app.MapDefaultEndpoints();
//app.UseHttpsRedirection();

// 1. 判断当前操作系统并定义基础路径
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
var basePlugPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../PlugConfig/Plugs");

var baseWebFilePath = isWindows
    ? GlobalData.MainWebFileServer // Windows使用现有配置
    : "/cj/apps"; // Linux对应的web文件路径

// 2. 确保目录存在（关键步骤：自动创建文件夹）
Directory.CreateDirectory(basePlugPath);
Directory.CreateDirectory(baseWebFilePath);

Console.WriteLine($"Base Web Path: {baseWebFilePath}");

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    FileProvider = new PhysicalFileProvider(baseWebFilePath),
    RequestPath = "/webFiles"
});
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    FileProvider = new PhysicalFileProvider(basePlugPath),
    RequestPath = "/plugconfig"
});

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App_All>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(moduleAssemblies.ToArray());

// VNC/SSH 远程桌面代理 API 端点
app.MapRemoteDesktopApi();

//app.UseAntiforgery();

app.Run();
