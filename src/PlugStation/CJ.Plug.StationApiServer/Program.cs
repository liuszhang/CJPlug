using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Login;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJ.Plug.StationApiServer.Apis;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug.StationApiServer.Services;
using CJ.Plug.StationApiService.Contracts;
using CJ.Plug.StationApiService.Services;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services;
using CJ.Plug_Aspire.StationApiService.StationApi;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System.Text;

// 设置 .NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 解析命令行 --port 参数
int? commandLinePort = null;
var builderArgs = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
    {
        commandLinePort = p;
        i++; // 跳过端口值
    }
    else
    {
        builderArgs.Add(args[i]);
    }
}

// 确定服务端口：命令行 --port > SQLite配置 > appsettings默认7660
int servicePort = commandLinePort ?? 7660;
if (commandLinePort == null)
{
    var dbPortStr = StationConfigHelper.ReadStationApiPort();
    if (int.TryParse(dbPortStr, out var dbPort))
        servicePort = dbPort;
}

var builder = WebApplication.CreateBuilder(builderArgs.ToArray());
var configuration = builder.Configuration;
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

// 从 StationSettingUI 共享的 SQLite 配置读取用户设置的平台服务地址
// 必须在 Serilog/SignalRLogSink 初始化之前完成 GlobalData 覆盖，
// 否则 SignalRLogSink 会在构造时捕获默认的 localhost:8686 地址
StaticData.MainServerUrl = StationConfigHelper.ReadMainServerUrl() ?? GlobalData.MainDispatcherServer;
Console.WriteLine("the main serverUrl is:" + StaticData.MainServerUrl);

// 将用户配置的主服务地址同步到 GlobalData，确保远程图站的 HTTP API 回调能正确路由到主服务
// 若不覆盖，远程图站会尝试向 localhost:8686/8687 回调，导致执行结果永远无法上报
if (!string.IsNullOrEmpty(StaticData.MainServerUrl) && StaticData.MainServerUrl != GlobalData.MainDispatcherServer)
{
    GlobalData.MainDispatcherServer = StaticData.MainServerUrl;
    try
    {
        var uri = new Uri(StaticData.MainServerUrl);
        // 主服务典型架构：Dispatcher 8686，API Server 8687
        GlobalData.MainApiServer = $"{uri.Scheme}://{uri.Host}:8687";
        Console.WriteLine($"[GlobalData] MainDispatcherServer -> {GlobalData.MainDispatcherServer}");
        Console.WriteLine($"[GlobalData] MainApiServer -> {GlobalData.MainApiServer}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GlobalData] Failed to override with configured URL: {ex.Message}");
    }
}
else
{
    // SQLite 无配置时，尝试从环境变量 MAIN_SERVER_HOST 获取主服务地址
    // 用于简化远程部署：无需依赖 StationSettingUI 写入 SQLite
    var envMainServerHost = Environment.GetEnvironmentVariable("MAIN_SERVER_HOST");
    if (!string.IsNullOrEmpty(envMainServerHost))
    {
        try
        {
            var uri = new Uri(envMainServerHost);
            StaticData.MainServerUrl = envMainServerHost;
            GlobalData.MainDispatcherServer = envMainServerHost;
            GlobalData.MainApiServer = $"{uri.Scheme}://{uri.Host}:8687";
            Console.WriteLine($"[GlobalData] 从环境变量 MAIN_SERVER_HOST 获取主服务地址: {envMainServerHost}");
            Console.WriteLine($"[GlobalData] MainApiServer -> {GlobalData.MainApiServer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GlobalData] 环境变量 MAIN_SERVER_HOST 格式无效: {envMainServerHost}, 错误: {ex.Message}");
        }
    }
}

//StaticData.MainServerHostIp = configuration.GetSection("MainServer").GetSection("Url").Value;
StaticData.MainServerHostIp = GlobalData.MainDispatcherServer;
Console.WriteLine("the main serverIp is:" + StaticData.MainServerHostIp);

// Serilog 初始化（含 SignalRLogSink）：必须在 GlobalData 覆盖之后，
// 确保 SignalRLogSink 使用用户配置的远程主服务地址而非默认的 localhost:8686
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("StationLogs/log.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .WriteTo.Sink(new SignalRLogSink("Station"))
    .CreateLogger();
//StaticData.ToolAgentServerHttpsPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Https").GetSection("Url").Value.Split(':')[2];
//StaticData.ToolAgentServerHttpScheme = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[0];
//StaticData.ToolAgentServerHttpPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[2];
StaticData.ToolAgentServer = configuration.GetSection("FileServer").GetSection("ToolAgentServer").Value ?? "";

// 从 StationSettingUI 共享的 SQLite 配置读取用户设置的工具安装根目录
StaticData.ToolsRootPath = StationConfigHelper.ReadToolsRootPath() ?? "";
Console.WriteLine("the ToolsRootPath is:" + StaticData.ToolsRootPath);

StaticData.ToolAgentServerHttpPort = servicePort.ToString();
Console.WriteLine("the service port is:" + servicePort);

// 覆盖 appsettings.json 中的 Kestrel 端口配置
builder.WebHost.UseUrls($"http://*:{servicePort}");





builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddOpenApiDocument(configure =>
{
    configure.Title = "Station API";
});

builder.Services.AddSingleton<StationHubService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StationHubService>());

// 图站任务本地存储
builder.Services.AddSingleton<StationTaskStore>(sp =>
{
    var store = new StationTaskStore();
    store.Init();
    return store;
});


builder.Services.AddScoped<IStationExecuteService, DefaultStationExecuteService>();
builder.Services.AddScoped<IStationFileService, StationFileService>();

// 远程桌面服务 (UltraVNC portable + SSH)
builder.Services.AddSingleton<UltraVncService>();
builder.Services.AddSingleton<RemoteDesktopService>();

// VNC 自启动服务：StationApiServer 启动时自动部署并启动 UltraVNC
builder.Services.AddHostedService<VncAutoStartService>();

// 窗口捕获服务
builder.Services.AddSingleton<WindowCaptureService>();

builder.Services.ConfigModuleApiServices();


builder.Services.AddSingleton<MainApiClient>();
//builder.Services.AddHttpClient<MainApiClient>(client =>
//{
//    //client.BaseAddress = new("https+http://apiservice");
//    client.BaseAddress = new(StaticData.MainServerHostIp);
//    client.Timeout = TimeSpan.FromSeconds(60);
//});


var app = builder.Build();

app.UseWebSockets();

app.MapDefaultEndpoints();

app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    Console.WriteLine("---Development Mode---");
    //app.UseOpenApi();
    //app.UseSwaggerUi();
    //app.UseOpenApi(settings =>
    //{
    //    settings.Path = "/swagger/station/swagger.json";
    //});
    //app.UseSwaggerUi(settings =>
    //{
    //    settings.Path = "/swagger/station";
    //    settings.DocumentPath = "/swagger/station/swagger.json";
    //});
}

//app.UseHttpsRedirection();


app.MapConnectionApi();
app.MapRemoteDesktopApi();

app.Run();
