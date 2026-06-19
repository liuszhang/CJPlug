using CJ.Plug.ApiServer;
using CJ.Plug.ElsaIntegration.ApiClient;
using CJ.Plug.ElsaIntegration.Contracts;
using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.Login;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Services;
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
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.EnableMultiplexing", false);

// 设置 .NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

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

// 全局 JSON 序列化配置：忽略循环引用
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

//builder.AddNpgsqlDataSource("postgresdb");
//builder.AddNpgsqlDataSource("NpgsqlConnectionString");

builder.Services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());

// ★方案② 备选：基准目录 = 自定义输出目录（和运行时一致）
var apiRootDir = AppContext.BaseDirectory;

// ========== 读取连接字符串 + 数据库类型，按类型动态注册 ==========
var connStr = DbConnectionString.ConnectionString;
var dbType = DbConnectionString.DbType;
// 仅 SQLite 需要将相对路径 Data Source= 拼接为绝对路径
var absoluteConnStr = connStr.Replace("Data Source=", $"Data Source={Path.Combine(apiRootDir, "")}");

builder.Services.AddDbContextPool<MainDbContext>(options =>
{
    switch (dbType)
    {
        case "PostgreSQL":
            options.UseNpgsql(connStr);
            break;
        case "SqlServer":
            options.UseSqlServer(connStr);
            break;
        default:
            options.UseSqlite(absoluteConnStr);
            break;
    }
}, poolSize: 128);

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

// 添加打包模块服务
builder.Services.AddPackageModuleServices();

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

//添加Elsa相关服务
builder.Services.ConfigElsaServicesWithOutDB();




var app = builder.Build();

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

// 先执行数据库迁移，确保表结构存在
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    // 使用 EnsureCreated 创建所有表（基于当前模型，包括 Identity 表）
    // 注意：如果后续需要正式迁移，应改回 MigrateAsync 并先生成包含 Identity 表的迁移
    if (await db.Database.EnsureCreatedAsync())
        Console.WriteLine("[SeedData] 数据库已创建");
    else
        Console.WriteLine("[SeedData] 数据库已存在");
}
catch (Exception ex)
{
    Console.WriteLine($"[SeedData] 数据库迁移异常：{ex.Message}");
    Log.Error(ex, "数据库迁移异常");
}

// 执行所有注册的种子数据提供者
try
{
    await SeedDataRunner.RunAllAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"[SeedData] 种子数据执行异常：{ex.Message}");
    Log.Error(ex, "种子数据执行异常");
}

app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

//app.UseAuthorization();

//添加验证中间件
app.UseAuthentication();
// 必须先调用 UseRouting
app.UseRouting();

// 启用CORS
app.UseCors("AllowAll");

app.UseAntiforgery();
app.UseWebSockets(); // 添加 WebSocket 支持 (VNC/SSH 代理)

//添加各模块的注入端点
app.ConfigModuleApis();

// 添加打包模块API
app.AddPackageModuleApi();

//aspire相关服务
app.MapDefaultEndpoints();


app.Run();

