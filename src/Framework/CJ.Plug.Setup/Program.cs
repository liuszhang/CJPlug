using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("=== CJ.Plug 一键安装打包工具 ===");
    Log.Information("开始打包...");

    var rootDir = AppContext.BaseDirectory;
    // 回退到仓库根目录
    var repoRoot = Path.GetFullPath(Path.Combine(rootDir, "..", "..", "..", ".."));
    var publishDir = Path.Combine(repoRoot, "02.Publish");
    var outputDir = Path.Combine(repoRoot, "05.Installer");

    if (!Directory.Exists(publishDir))
    {
        Log.Warning("发布目录不存在: {PublishDir}，请先执行各项目的 publish", publishDir);
        Log.Information("正在执行项目发布...");

        // 发布 ApiServer
        RunDotnetPublish(Path.Combine(repoRoot, "src", "PlugApiServer", "CJ.Plug.ApiServer"));
        // 发布 HostWebServer
        RunDotnetPublish(Path.Combine(repoRoot, "src", "PlugWebHost", "CJ.Plug.HostWebServer"));
        // 发布 StationAgent
        RunDotnetPublish(Path.Combine(repoRoot, "src", "PlugStation", "CJ.Plug.StationAgent"));
    }

    if (Directory.Exists(outputDir))
    {
        Directory.Delete(outputDir, true);
    }
    Directory.CreateDirectory(outputDir);

    Log.Information("复制发布文件到安装包目录: {OutputDir}", outputDir);
    CopyDirectory(publishDir, outputDir);

    Log.Information("打包完成! 安装包位于: {OutputDir}", outputDir);
}
catch (Exception ex)
{
    Log.Fatal(ex, "打包过程发生错误");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

static void RunDotnetPublish(string projectPath)
{
    var process = new System.Diagnostics.Process
    {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c Release --self-contained true -r win-x64",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }
    };
    process.Start();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        var err = process.StandardError.ReadToEnd();
        throw new Exception($"发布失败 ({projectPath}): {err}");
    }
    Log.Information("发布成功: {ProjectPath}", projectPath);
}

static void CopyDirectory(string sourceDir, string destDir)
{
    foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
    {
        Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
    }
    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var destFile = file.Replace(sourceDir, destDir);
        File.Copy(file, destFile, true);
    }
}
