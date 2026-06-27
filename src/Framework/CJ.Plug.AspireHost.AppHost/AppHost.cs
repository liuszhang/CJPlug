using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

// ============ 中文乱码修复 (三层编码设置) ============

// 第1层: 注册 CodePages 编码提供程序，让 .NET 运行时能正确处理各种编码（含 UTF-8）
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 第2层: 设置 .NET Console 编码为 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 第3层: 通过 P/Invoke 设置 Windows 原生控制台代码页为 UTF-8 (65001)
// 这是最关键的一步——Serilog.Sinks.Console 和底层 Windows API 走的是原生代码页，
// 不受 Console.OutputEncoding 影响，必须同时设置才能彻底解决中文乱码
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    SetConsoleOutputCP(65001);
    SetConsoleCP(65001);
}

[DllImport("kernel32.dll")]
static extern bool SetConsoleOutputCP(uint wCodePageId);

[DllImport("kernel32.dll")]
static extern bool SetConsoleCP(uint wCodePageId);

// 设置 Environment
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15288");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:19275");
Environment.SetEnvironmentVariable("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL", "http://localhost:19276");
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

// 强制所有子进程 (dotnet xxx.dll) 的控制台输出使用 UTF-8 编码
// Aspire Dashboard 日志收集管道按 UTF-8 解码，不设置的话 Windows 默认 GBK 会导致中文乱码
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_DEFAULT_ENCODING", "utf-8");

// UTF-8 编码环境变量，通过 .WithEnvironment 传递给各 AddExecutable 资源
const string Utf8EnvKey = "DOTNET_SYSTEM_CONSOLE_DEFAULT_ENCODING";
const string Utf8EnvVal = "utf-8";

// 共享输出目录配置：每个服务的 appsettings.json 以 appsettings.{项目名}.json 命名，
// 通过 ASPNETCORE_CONFIG_PATH 环境变量指定各自的配置文件，避免互相覆盖
const string ConfigPathEnvKey = "ASPNETCORE_CONFIG_PATH";

// Kestrel 端口环境变量：优先级最高，确保每个服务绑定到正确端口
// 当 ASPNETCORE_CONFIG_PATH 未生效时，此变量作为兜底覆盖 Kestrel EndPoints 配置
const string KestrelUrlEnvKey = "Kestrel__EndPoints__Http__Url";

// If running on Windows and not elevated, restart the entire application with elevation (UAC)
// --no-elevate: 由 Desktop 服务管理器启动时跳过自身提权，Desktop 会自行处理管理员权限
var skipElevation = args.Any(a => a.Equals("--no-elevate", StringComparison.OrdinalIgnoreCase));
if (!skipElevation && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    try
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(currentExe))
            {
                args = Environment.GetCommandLineArgs();
                var arguments = string.Empty;
                if (args.Length > 1)
                {
                    arguments = string.Join(" ", args.Skip(1).Select(a => a.Contains(' ') ? '"' + a + '"' : a));
                }

                var psi = new ProcessStartInfo
                {
                    FileName = currentExe,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    Process.Start(psi);
                    Environment.Exit(0);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine("Elevation was cancelled or failed. Continuing without administrator privileges.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to check/request elevation: {ex.Message}");
    }
}

// AppHost 输出目录：02.Publish/CJ.Plug.AspireHost.AppHost/{Configuration}/net10.0/
var appHostDir = AppContext.BaseDirectory;
Console.WriteLine($"AppHost directory: {appHostDir}");

// 加载配置（仅 appsettings.json，不再依赖 appsettings.Development.json）
var configure = new ConfigurationBuilder()
    .SetBasePath(appHostDir)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build()
    .GetSection("ResourceStrings");

// 动态计算服务目录：AppHost -> 02.Publish/ -> Services/{Configuration}/net10.0/
// 自动适配 Debug/Release 构建配置，无需手动修改路径
var publishDir = Path.GetFullPath(Path.Combine(appHostDir, "..", "..", ".."));
var configName = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
var servicesDir = Path.Combine(publishDir, "Services", configName, "net10.0");
Console.WriteLine($"Services directory: {servicesDir}");

// 服务路径解析辅助方法：获取 DLL 文件名
string ServiceDll(string configKey, string defaultDll) =>
    configure.GetValue<string>(configKey) ?? defaultDll;

// 所有服务共享同一个输出目录作为工作目录
var dispatchServerDllName = ServiceDll("DispatchServer", "CJ.Plug.DispatchServer.dll");
var apiServerDllName = ServiceDll("ApiServer", "CJ.Plug.ApiServer.dll");
var stationApiServerDllName = ServiceDll("StationApiServer", "CJ.Plug.StationApiServer.dll");
var elsaApiServerDllName = ServiceDll("ElsaApiServer", "CJ.Plug.ElsaApiServer.dll");
var webFrontendDllName = ServiceDll("WebFrontend", "CJ.Plug.HostWebServer.dll");
var mcpServerDllName = ServiceDll("McpServer", "CJ.Plug.McpServer.dll");

var builder = DistributedApplication.CreateBuilder(
    args.Where(a => !a.Equals("--no-elevate", StringComparison.OrdinalIgnoreCase)).ToArray());

// 重新设置 Console.OutputEncoding — CreateBuilder 内部可能重置了编码
Console.OutputEncoding = Encoding.UTF8;


//var postgresdb = builder.AddPostgres("pg")
//                        .AddDatabase("postgresdb");
//.WithPgAdmin();


//builder.AddProject<Projects.CJ_Plug_DispatchServer>("cj-plug-sds");
//builder.AddProject("cj-plug-sds", @"../PlugApiServer/CJ.Plug.DispatchServer/CJ.Plug.DispatchServer.csproj");
//builder.AddProject("cj-plug-sds", dispatchServer);
builder.AddExecutable(
        name: "sds",
        command: "dotnet",
        workingDirectory: servicesDir,
        args: dispatchServerDllName // DLL 文件名
    ).WithEnvironment(Utf8EnvKey, Utf8EnvVal)
     .WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.DispatchServer.json"))
     .WithEnvironment(KestrelUrlEnvKey, "http://*:8686")
     .WithUrl("http://localhost:8686");

//builder.AddProject<Projects.CJ_Plug_ApiServer>("apiservice").WithEnvironment(Utf8EnvKey, Utf8EnvVal);
//builder.AddProject("apiservice", @"../PlugApiServer/CJ.Plug.ApiServer/CJ.Plug.ApiServer.csproj");
//builder.AddProject("apiservice", apiServer);
builder.AddExecutable("apiservice", "dotnet", servicesDir, apiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.ApiServer.json")).WithEnvironment(KestrelUrlEnvKey, "http://*:8687").WithUrl("http://localhost:8687");

//builder.AddProject<Projects.CJ_Plug_StationApiServer>("stationapi");
//builder.AddProject("stationapi", @"../PlugStation/CJ.Plug.StationApiServer/CJ.Plug.StationApiServer.csproj");
//builder.AddProject("stationapi", stationApiServer);
builder.AddExecutable("stationapi", "dotnet", servicesDir, stationApiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.StationApiServer.json")).WithEnvironment(KestrelUrlEnvKey, "http://*:7660").WithUrl("http://localhost:7660");


//builder.AddProject<Projects.CJ_Plug_ElsaApiServer>("elsaapiserver");
//builder.AddProject("elsaapiserver", @"../PlugApiServer/CJ.Plug.ElsaApiServer/CJ.Plug.ElsaApiServer.csproj");
//builder.AddProject("elsaapiserver", elsaApiServer);
builder.AddExecutable("elsaapiserver", "dotnet", servicesDir, elsaApiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.ElsaApiServer.json")).WithEnvironment(KestrelUrlEnvKey, "http://*:5001").WithUrl("http://localhost:5001");

//builder.AddProject<Projects.CJ_Plug_ElsaStudio>("elsastudio");
//builder.AddProject("elsastudio", @"../PlugWebHost/CJ.Plug.ElsaStudio/CJ.Plug.ElsaStudio.csproj");
//builder.AddProject("elsastudio", elsaStudio);
//builder.AddExecutable("elsastudio", "dotnet", elsaStudioDirectory, elsaStudioDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5010");

//builder.AddProject<Projects.CJ_Plug_HostWebServer>("webfrontend").WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5066");
//builder.AddProject("webfrontend", @"../PlugWebHost/CJ.Plug.HostWebServer/CJ.Plug.HostWebServer.csproj");
//builder.AddProject("webfrontend", webFrontend);
builder.AddExecutable("webfrontend", "dotnet", servicesDir, webFrontendDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.HostWebServer.json")).WithEnvironment(KestrelUrlEnvKey, "http://*:5066").WithUrl("http://localhost:5066");

//builder.AddProject<Projects.CJ_Plug_McpServer>("cj-plug-mcpserver");
//builder.AddProject("cj-plug-mcpserver", @"../PlugApiServer/CJ.Plug.McpServer/CJ.Plug.McpServer.csproj");
//builder.AddProject("cj-plug-mcpserver", mcpServer);
builder.AddExecutable("mcpserver", "dotnet", servicesDir, mcpServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithEnvironment(ConfigPathEnvKey, Path.Combine(servicesDir, "appsettings.CJ.Plug.McpServer.json")).WithEnvironment(KestrelUrlEnvKey, "http://*:3001").WithUrl("http://localhost:3001");


//MCP TOOL测试工具：mcp inspector，基于 @modelcontextprotocol/inspector 实现的 MCP 协议调试工具，提供可视化界面查看 MCP 消息交互
builder.AddExecutable("mcpInspector", "npx", servicesDir, "@modelcontextprotocol/inspector", "--server-port", "6277")
    .WithEnvironment(Utf8EnvKey, Utf8EnvVal)
    .WithEnvironment("HOST", "localhost")
    .WithEnvironment("NODE_OPTIONS", "--dns-result-order=ipv4first")
    .WithUrl("http://localhost:6274");

//打开浏览器
try
{
    var url = new Uri("http://localhost:5066");
    var psi = new ProcessStartInfo
    {
        FileName = url.ToString(),
        UseShellExecute = true
    };
    //Process.Start(psi);
}
catch (Exception ex)
{
    Console.WriteLine($"Error opening browser. {ex.Message}");
    Console.WriteLine($"Please manually open this URL");
}

// 最后一次确保 Console.OutputEncoding 为 UTF-8
// Aspire hosting 内部的 Logger 创建、gRPC 启动等可能重置编码
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Console.OutputEncoding = " + Console.OutputEncoding.EncodingName);

builder.Build().Run();
