using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

// 设置 Environment
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15288");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:19275");
Environment.SetEnvironmentVariable("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL", "http://localhost:19276");
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
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
    ).WithUrl("http://localhost:6660");

builder.AddProject<Projects.CJ_Plug_ApiServer>("apiservice");
//builder.AddProject("apiservice", @"../PlugApiServer/CJ.Plug.ApiServer/CJ.Plug.ApiServer.csproj");
//builder.AddProject("apiservice", apiServer);
//builder.AddExecutable("apiservice", "dotnet", apiServerDirectory, apiServerDllName).WithUrl("http://localhost:6661");

//builder.AddProject<Projects.CJ_Plug_StationApiServer>("stationapi");
//builder.AddProject("stationapi", @"../PlugStation/CJ.Plug.StationApiServer/CJ.Plug.StationApiServer.csproj");
//builder.AddProject("stationapi", stationApiServer);
builder.AddExecutable("stationapi", "dotnet", stationApiServerDirectory, stationApiServerDllName).WithUrl("http://localhost:7660");


//builder.AddProject<Projects.CJ_Plug_ElsaApiServer>("elsaapiserver");
//builder.AddProject("elsaapiserver", @"../PlugApiServer/CJ.Plug.ElsaApiServer/CJ.Plug.ElsaApiServer.csproj");
//builder.AddProject("elsaapiserver", elsaApiServer);
builder.AddExecutable("elsaapiserver", "dotnet", elsaApiServerDirectory, elsaApiServerDllName).WithUrl("http://localhost:5001");

//builder.AddProject<Projects.CJ_Plug_ElsaStudio>("elsastudio");
//builder.AddProject("elsastudio", @"../PlugWebHost/CJ.Plug.ElsaStudio/CJ.Plug.ElsaStudio.csproj");
//builder.AddProject("elsastudio", elsaStudio);
builder.AddExecutable("elsastudio", "dotnet", elsaStudioDirectory, elsaStudioDllName).WithUrl("http://localhost:5010");

builder.AddProject<Projects.CJ_Plug_HostWebServer>("webfrontend");
//builder.AddProject("webfrontend", @"../PlugWebHost/CJ.Plug.HostWebServer/CJ.Plug.HostWebServer.csproj");
//builder.AddProject("webfrontend", webFrontend);
//builder.AddExecutable("webfrontend", "dotnet", webFrontendDirectory, webFrontendDllName).WithUrl("http://localhost:5066");

//builder.AddProject<Projects.CJ_Plug_HostWasm>("hostwasm");
//builder.AddProject("hostwasm", @"../PlugWebHost/CJ.Plug.HostWasm/CJ.Plug.HostWasm.csproj");

//builder.AddProject<Projects.MC_SLM_F>("mc-slm");
//builder.AddProject("mc-slm", mcSlm);

//builder.AddProject<Projects.CJ_Plug_McpServer>("cj-plug-mcpserver");
//builder.AddProject("cj-plug-mcpserver", @"../PlugApiServer/CJ.Plug.McpServer/CJ.Plug.McpServer.csproj");
//builder.AddProject("cj-plug-mcpserver", mcpServer);
builder.AddExecutable("mcpserver", "dotnet", mcpServerDirectory, mcpServerDllName).WithUrl("http://localhost:3001");



builder.AddExecutable("mcpInspector", "npx", mcpServerDirectory, "@modelcontextprotocol/inspector").WithUrl("http://localhost:6274");


//打开浏览器
try
{
    var url = new Uri("http://localhost:5066");
    var psi = new ProcessStartInfo
    {
        FileName = url.ToString(),
        UseShellExecute = true
    };
    Process.Start(psi);
}
catch (Exception ex)
{
    Console.WriteLine($"Error opening browser. {ex.Message}");
    Console.WriteLine($"Please manually open this URL");
}

builder.Build().Run();
