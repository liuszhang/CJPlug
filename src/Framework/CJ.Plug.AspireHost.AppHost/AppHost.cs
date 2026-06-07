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
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

// 强制所有子进程 (dotnet xxx.dll) 的控制台输出使用 UTF-8 编码
// Aspire Dashboard 日志收集管道按 UTF-8 解码，不设置的话 Windows 默认 GBK 会导致中文乱码
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_DEFAULT_ENCODING", "utf-8");

// UTF-8 编码环境变量，通过 .WithEnvironment 传递给各 AddExecutable 资源
const string Utf8EnvKey = "DOTNET_SYSTEM_CONSOLE_DEFAULT_ENCODING";
const string Utf8EnvVal = "utf-8";
Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); ;


// If running on Windows and not elevated, restart the entire application with elevation (UAC)
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                // Skip the first arg (program path)
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
                    // Exit current process so elevated instance continues
                    Environment.Exit(0);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // User cancelled elevation or elevation failed. Continue without elevation.
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


var builder = DistributedApplication.CreateBuilder(args);

// 重新设置 Console.OutputEncoding — CreateBuilder 内部可能重置了编码
Console.OutputEncoding = Encoding.UTF8;

//获取当前运行程序的路径
var currentDirectory = Directory.GetCurrentDirectory();
Console.WriteLine($"currentDirectory: {currentDirectory}");

var configure = builder.Configuration.GetSection("ResourceStrings");
//Console.WriteLine("test:"+configure.GetValue<string>("DispatchServer"));

var dispatchServerFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("DispatchServer")));
var dispatchServerDirectory = dispatchServerFileInfo.Directory?.FullName;
var dispatchServerDllName = dispatchServerFileInfo.Name;
//Console.WriteLine($"dispatchServerDirectory: {dispatchServerDirectory}");
//Console.WriteLine($"dispatchServerDllName: {dispatchServerDllName}");

var apiServerFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("ApiServer")));
var apiServerDirectory = apiServerFileInfo.Directory?.FullName;
var apiServerDllName = apiServerFileInfo.Name;

var stationApiServer = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("StationApiServer")));
var stationApiServerDirectory = stationApiServer.Directory?.FullName;
var stationApiServerDllName = stationApiServer.Name;

var webFrontendFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("WebFrontend")));
var webFrontendDirectory = webFrontendFileInfo.Directory?.FullName;
var webFrontendDllName = webFrontendFileInfo.Name;

var elsaApiServerFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("ElsaApiServer")));
var elsaApiServerDirectory = elsaApiServerFileInfo.Directory?.FullName;
var elsaApiServerDllName = elsaApiServerFileInfo.Name;

var elsaStudioFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("ElsaStudio")));
var elsaStudioDirectory = elsaStudioFileInfo.Directory?.FullName;
var elsaStudioDllName = elsaStudioFileInfo.Name;

//var mcSlm = Path.Combine(currentDirectory, configure.GetValue<string>("McSlm"),dllFolder);

var mcpServerFileInfo = new FileInfo(Path.Combine(currentDirectory, configure.GetValue<string>("McpServer")));
var mcpServerDirectory = mcpServerFileInfo.Directory?.FullName;
var mcpServerDllName = mcpServerFileInfo.Name;


//var postgresdb = builder.AddPostgres("pg")
//                        .AddDatabase("postgresdb");
//.WithPgAdmin();


//builder.AddProject<Projects.CJ_Plug_DispatchServer>("cj-plug-sds");
//builder.AddProject("cj-plug-sds", @"../PlugApiServer/CJ.Plug.DispatchServer/CJ.Plug.DispatchServer.csproj");
//builder.AddProject("cj-plug-sds", dispatchServer);
builder.AddExecutable(
        name: "sds",
        command: "dotnet",
        workingDirectory: dispatchServerDirectory,
        args: dispatchServerDllName // DLL 文件名
    ).WithEnvironment(Utf8EnvKey, Utf8EnvVal)
     .WithUrl("http://localhost:8686");

//builder.AddProject<Projects.CJ_Plug_ApiServer>("apiservice").WithEnvironment(Utf8EnvKey, Utf8EnvVal);
//builder.AddProject("apiservice", @"../PlugApiServer/CJ.Plug.ApiServer/CJ.Plug.ApiServer.csproj");
//builder.AddProject("apiservice", apiServer);
builder.AddExecutable("apiservice", "dotnet", apiServerDirectory, apiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:8687");

//builder.AddProject<Projects.CJ_Plug_StationApiServer>("stationapi");
//builder.AddProject("stationapi", @"../PlugStation/CJ.Plug.StationApiServer/CJ.Plug.StationApiServer.csproj");
//builder.AddProject("stationapi", stationApiServer);
builder.AddExecutable("stationapi", "dotnet", stationApiServerDirectory, stationApiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:7660");


//builder.AddProject<Projects.CJ_Plug_ElsaApiServer>("elsaapiserver");
//builder.AddProject("elsaapiserver", @"../PlugApiServer/CJ.Plug.ElsaApiServer/CJ.Plug.ElsaApiServer.csproj");
//builder.AddProject("elsaapiserver", elsaApiServer);
builder.AddExecutable("elsaapiserver", "dotnet", elsaApiServerDirectory, elsaApiServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5001");

//builder.AddProject<Projects.CJ_Plug_ElsaStudio>("elsastudio");
//builder.AddProject("elsastudio", @"../PlugWebHost/CJ.Plug.ElsaStudio/CJ.Plug.ElsaStudio.csproj");
//builder.AddProject("elsastudio", elsaStudio);
builder.AddExecutable("elsastudio", "dotnet", elsaStudioDirectory, elsaStudioDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5010");

//builder.AddProject<Projects.CJ_Plug_HostWebServer>("webfrontend").WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5066");
//builder.AddProject("webfrontend", @"../PlugWebHost/CJ.Plug.HostWebServer/CJ.Plug.HostWebServer.csproj");
//builder.AddProject("webfrontend", webFrontend);
builder.AddExecutable("webfrontend", "dotnet", webFrontendDirectory, webFrontendDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:5066");

//builder.AddProject<Projects.CJ_Plug_McpServer>("cj-plug-mcpserver");
//builder.AddProject("cj-plug-mcpserver", @"../PlugApiServer/CJ.Plug.McpServer/CJ.Plug.McpServer.csproj");
//builder.AddProject("cj-plug-mcpserver", mcpServer);
builder.AddExecutable("mcpserver", "dotnet", mcpServerDirectory, mcpServerDllName).WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:3001");


//MCP TOOL测试工具：mcp inspector，基于 @modelcontextprotocol/inspector 实现的 MCP 协议调试工具，提供可视化界面查看 MCP 消息交互
builder.AddExecutable("mcpInspector", "npx", mcpServerDirectory, "@modelcontextprotocol/inspector").WithEnvironment(Utf8EnvKey, Utf8EnvVal).WithUrl("http://localhost:6274");

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
