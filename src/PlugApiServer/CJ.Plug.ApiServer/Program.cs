using CJ.Plug.ElsaIntegration.Contracts;
using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Login;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.ModuleConfig;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using Elsa.Extensions;
using Elsa.Studio.Agents.UI.Pages;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlugsBundle;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

// Add service defaults & Aspire components.
builder.AddServiceDefaults();


//builder.Services.AddSignalR();

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

//builder.AddNpgsqlDataSource("postgresdb");
//builder.AddNpgsqlDataSource("NpgsqlConnectionString");

builder.Services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());

// ★方案② 备选：基准目录 = 自定义输出目录（和运行时一致）
var apiRootDir = AppContext.BaseDirectory;

// ========== 读取连接字符串 + 拼接绝对路径 ==========
//var connStr = builder.Configuration.GetConnectionString("Sqlite");
var connStr = DbConnectionString.ConnectionString;
// 将「相对路径的连接字符串」转换为「绝对路径」
var absoluteConnStr = connStr.Replace("Data Source=", $"Data Source={Path.Combine(apiRootDir, "")}");


builder.Services.AddDbContext<MainDbContext>(options =>
                //options.UseSqlite(DbConnectionString.ConnectionString));
                options.UseSqlite(absoluteConnStr));

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

builder.Services.ConfigModuleApiServices();
//单独添加用户认证相关服务
builder.Services.AddLoginModuleApiServices();

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

// 添加认证服务
builder.Services.AddAuthentication();



var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

//app.UseAuthorization();

//添加验证中间件
app.UseAuthentication();
// 必须先调用 UseRouting
app.UseRouting();

app.UseAntiforgery();
app.UseWebSockets(); // 添加 WebSocket 支持 (VNC/SSH 代理)

//添加各模块的注入端点
app.ConfigModuleApis();

//aspire相关服务
app.MapDefaultEndpoints();


app.Run();

