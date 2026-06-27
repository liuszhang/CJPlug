using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.ApiServer.Services;

public class PackageService
{
    private readonly ILogger<PackageService> _logger;
    private readonly PackageProgressTracker _progressTracker;

    public PackageService(ILogger<PackageService> logger, PackageProgressTracker progressTracker)
    {
        _logger = logger;
        _progressTracker = progressTracker;
    }

    /// <summary>
    /// 生成本地启动包（异步版本，支持进度跟踪）
    /// </summary>
    public async Task<string> GenerateLocalPackageAsync(string platform = "win-x64", bool includeDocker = false)
    {
        var taskId = _progressTracker.CreateTask();
        
        _ = Task.Run(async () =>
        {
            try
            {
                _progressTracker.UpdateProgress(taskId, 5, "开始生成本地启动包...");
                _logger.LogInformation("开始生成本地启动包，平台: {Platform}, 包含Docker: {IncludeDocker}", platform, includeDocker);

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"CJPlug-Package-{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 1. 复制部署包（直接复制02.Publish下已发布的文件，无需编译）
                    _progressTracker.UpdateProgress(taskId, 10, "正在复制服务部署包...");
                    CopyMainServices(tempDir, platform, taskId);

                    // 2. 创建启动脚本
                    _progressTracker.UpdateProgress(taskId, 50, "正在创建启动脚本...");
                    CreateStartupScripts(tempDir, platform, taskId);

                    // 3. 创建配置文件
                    _progressTracker.UpdateProgress(taskId, 60, "正在创建配置文件...");
                    CreateConfigurationFiles(tempDir, taskId);

                    // 4. 创建默认数据库
                    _progressTracker.UpdateProgress(taskId, 70, "正在创建默认数据库...");
                    await CreateDefaultDatabases(tempDir, taskId);

                    // 5. 如果需要，创建Docker配置
                    if (includeDocker)
                    {
                        _progressTracker.UpdateProgress(taskId, 75, "正在创建Docker配置...");
                        CreateDockerConfiguration(tempDir, platform, taskId);
                    }

                    // 6. 创建README文档
                    _progressTracker.UpdateProgress(taskId, 85, "正在创建文档...");
                    CreateDocumentation(tempDir, taskId);

                    // 7. 打包成ZIP
                    _progressTracker.UpdateProgress(taskId, 90, "正在打包文件...");
                    var zipPath = Path.Combine(Path.GetTempPath(), $"CJPlug-Local-{platform}.zip");
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    ZipFile.CreateFromDirectory(tempDir, zipPath);
                    var zipBytes = await File.ReadAllBytesAsync(zipPath);

                    _progressTracker.UpdateProgress(taskId, 95, "正在完成...");
                    _progressTracker.CompleteTask(taskId, zipBytes);

                    _logger.LogInformation("本地启动包生成完成: {ZipPath}", zipPath);
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成启动包失败");
                _progressTracker.FailTask(taskId, ex.Message);
            }
        });

        return taskId;
    }

    /// <summary>
    /// 生成图站部署包（仅包含图站相关程序，不编译，直接复制02.Publish下已发布的文件）
    /// </summary>
    public async Task<string> GenerateStationPackageAsync(string platform = "win-x64")
    {
        var taskId = _progressTracker.CreateTask();

        _ = Task.Run(async () =>
        {
            try
            {
                _progressTracker.UpdateProgress(taskId, 5, "开始生成图站部署包...");
                _logger.LogInformation("开始生成图站部署包，平台: {Platform}", platform);

                var tempDir = Path.Combine(Path.GetTempPath(), $"CJPlug-Station-{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 1. 复制图站相关服务（直接复制，无需编译）
                    _progressTracker.UpdateProgress(taskId, 10, "正在复制图站服务...");
                    CopyStationServices(tempDir, platform, taskId);

                    // 2. 创建图站启动脚本
                    _progressTracker.UpdateProgress(taskId, 50, "正在创建启动脚本...");
                    CreateStationStartupScripts(tempDir, platform, taskId);

                    // 3. 创建图站配置文件
                    _progressTracker.UpdateProgress(taskId, 60, "正在创建配置文件...");
                    CreateStationConfigurationFiles(tempDir, taskId);

                    // 4. 创建README文档
                    _progressTracker.UpdateProgress(taskId, 85, "正在创建文档...");
                    CreateStationDocumentation(tempDir, taskId);

                    // 5. 打包成ZIP
                    _progressTracker.UpdateProgress(taskId, 90, "正在打包文件...");
                    var zipPath = Path.Combine(Path.GetTempPath(), $"CJPlug-Station-{platform}.zip");
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    ZipFile.CreateFromDirectory(tempDir, zipPath);
                    var zipBytes = await File.ReadAllBytesAsync(zipPath);

                    _progressTracker.UpdateProgress(taskId, 95, "正在完成...");
                    _progressTracker.CompleteTask(taskId, zipBytes);

                    _logger.LogInformation("图站部署包生成完成: {ZipPath}", zipPath);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成图站部署包失败");
                _progressTracker.FailTask(taskId, ex.Message);
            }
        });

        return taskId;
    }

    /// <summary>
    /// 同步生成图站部署包，直接在内存中打包并返回字节数组（不走进度跟踪器）
    /// </summary>
    public async Task<byte[]> GenerateStationPackageDirectAsync(string platform, CancellationToken ct = default)
    {
        _logger.LogInformation("开始同步生成图站部署包，平台: {Platform}", platform);

        var tempDir = Path.Combine(Path.GetTempPath(), $"CJPlug-Station-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            ct.ThrowIfCancellationRequested();

            // 1. 复制图站相关服务
            CopyStationServicesInternal(tempDir, platform);

            ct.ThrowIfCancellationRequested();

            // 2. 创建图站启动脚本
            CreateStationStartupScriptsInternal(tempDir, platform);

            ct.ThrowIfCancellationRequested();

            // 3. 创建图站配置文件
            CreateStationConfigurationFilesInternal(tempDir);

            ct.ThrowIfCancellationRequested();

            // 4. 创建README文档
            CreateStationDocumentationInternal(tempDir);

            ct.ThrowIfCancellationRequested();

            // 5. 打包成ZIP（在内存中完成，不写临时文件）
            using var memoryStream = new MemoryStream();
            ZipFile.CreateFromDirectory(tempDir, memoryStream);
            var zipBytes = memoryStream.ToArray();

            _logger.LogInformation("图站部署包同步生成完成，大小: {Size} bytes", zipBytes.Length);
            return zipBytes;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
                }
            }
        }
    }

    /// <summary>
    /// 复制主程序部署包（直接复制02.Publish下已发布的文件，无需编译）
    /// </summary>
    private void CopyMainServices(string outputDir, string platform, string taskId)
    {
        _progressTracker.AddLog(taskId, "开始复制服务部署包...");

        var repoRoot = GetRepositoryRoot();
        var publishBaseDir = Path.Combine(repoRoot, "02.Publish");
        var servicesDir = Path.Combine(outputDir, "services");
        Directory.CreateDirectory(servicesDir);

        // 主服务（ApiServer、HostWebServer、ElsaApiServer、McpServer、DispatchServer）共享输出目录 02.Publish/Services/
        // 将共享目录中的构建产物整体复制到 services/（排除 uvnc-portable，该目录仅图站使用）
        var sharedServicesDir = Path.Combine(publishBaseDir, "Services", "Release", "net10.0");

        if (Directory.Exists(sharedServicesDir))
        {
            _progressTracker.UpdateProgress(taskId, 20, "正在复制服务共享目录...");
            _progressTracker.AddLog(taskId, $"复制服务共享目录: {sharedServicesDir}");
            CopyDirectory(sharedServicesDir, servicesDir, IsPackagingFileIncluded,
                dirFilter: dirName => !dirName.Equals("uvnc-portable", StringComparison.OrdinalIgnoreCase));
            _progressTracker.AddLog(taskId, "服务共享目录复制完成");
        }
        else
        {
            _progressTracker.AddLog(taskId, $"共享服务目录不存在: {sharedServicesDir}，跳过", "Warning");
            _logger.LogWarning("共享服务目录不存在: {SourceDir}", sharedServicesDir);
        }

        _progressTracker.UpdateProgress(taskId, 45, "服务部署包复制完成");
    }

    /// <summary>
    /// 复制图站部署包（仅复制图站相关服务，无需编译）
    /// </summary>
    private void CopyStationServices(string outputDir, string platform, string taskId)
    {
        _progressTracker.AddLog(taskId, "开始复制图站服务...");
        CopyStationServicesCore(outputDir, platform,
            (progress, msg) => _progressTracker.UpdateProgress(taskId, progress, msg),
            (msg, level) => _progressTracker.AddLog(taskId, msg, level));
    }

    /// <summary>
    /// 复制图站服务（内部实现，不依赖进度跟踪器）
    /// </summary>
    private void CopyStationServicesInternal(string outputDir, string platform)
    {
        CopyStationServicesCore(outputDir, platform, null, null);
    }

    private void CopyStationServicesCore(string outputDir, string platform,
        Action<int, string>? onProgress, Action<string, string>? onLog)
    {
        onLog?.Invoke("开始复制图站服务...", "Info");

        var repoRoot = GetRepositoryRoot();
        var publishBaseDir = Path.Combine(repoRoot, "02.Publish");
        var sharedServicesDir = Path.Combine(publishBaseDir, "Services", "Release", "net10.0");
        var servicesDir = Path.Combine(outputDir, "services");
        Directory.CreateDirectory(servicesDir);

        // StationSettingUI 是桌面管理工具，放到 tools 目录
        var toolsDir = Path.Combine(outputDir, "tools");
        Directory.CreateDirectory(toolsDir);

        if (!Directory.Exists(sharedServicesDir))
        {
            onLog?.Invoke($"共享服务目录不存在: {sharedServicesDir}，跳过图站服务复制", "Warning");
            _logger.LogWarning("共享服务目录不存在: {SourceDir}", sharedServicesDir);
            return;
        }

        // 从 deps.json 解析图站服务所需的全部依赖文件
        var requiredFlatFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var requiredRelativeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stationServices = new[] { "CJ.Plug.StationAgent", "CJ.Plug.StationApiServer" };

        foreach (var service in stationServices)
        {
            var depsJsonPath = Path.Combine(sharedServicesDir, $"{service}.deps.json");
            if (File.Exists(depsJsonPath))
            {
                onLog?.Invoke($"解析依赖清单: {service}.deps.json", "Info");
                var (flatFiles, relativeFiles) = ExtractRequiredFilesFromDeps(depsJsonPath);
                requiredFlatFiles.UnionWith(flatFiles);
                requiredRelativeFiles.UnionWith(relativeFiles);
                onLog?.Invoke($"  {service} 需要 {flatFiles.Count + relativeFiles.Count} 个依赖文件（托管 {flatFiles.Count}，原生 {relativeFiles.Count}）", "Info");
            }
            else
            {
                onLog?.Invoke($"未找到 {service}.deps.json，跳过", "Warning");
            }
        }

        var totalRequired = requiredFlatFiles.Count + requiredRelativeFiles.Count;
        onProgress?.Invoke(15, $"正在复制图站依赖文件（共 {totalRequired} 个）...");

        // 按依赖清单从共享目录精确复制
        var copiedCount = 0;

        // 需跳过的文件扩展名（生产环境不需要）
        var skipExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdb",   // 调试符号
            ".map",   // JS source map
        };

        // 复制托管 DLL（扁平结构，在发布根目录）
        foreach (var fileName in requiredFlatFiles)
        {
            if (skipExtensions.Contains(Path.GetExtension(fileName)))
                continue;

            var sourceFile = Path.Combine(sharedServicesDir, fileName);
            if (File.Exists(sourceFile))
            {
                var destFile = Path.Combine(servicesDir, fileName);
                File.Copy(sourceFile, destFile, true);
                copiedCount++;
            }
        }

        // 复制原生库（保留 runtimes/ 子目录结构）
        foreach (var relativePath in requiredRelativeFiles)
        {
            if (skipExtensions.Contains(Path.GetExtension(relativePath)))
                continue;

            var sourceFile = Path.Combine(sharedServicesDir, relativePath);
            if (File.Exists(sourceFile))
            {
                var destFile = Path.Combine(servicesDir, relativePath);
                var destDir = Path.GetDirectoryName(destFile);
                if (destDir != null)
                    Directory.CreateDirectory(destDir);
                File.Copy(sourceFile, destFile, true);
                copiedCount++;
            }
        }

        // 复制服务自身的可执行文件和配置文件
        foreach (var service in stationServices)
        {
            foreach (var ext in new[] { ".exe", ".runtimeconfig.json", ".deps.json" })
            {
                var sourceFile = Path.Combine(sharedServicesDir, service + ext);
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, Path.Combine(servicesDir, service + ext), true);
                    copiedCount++;
                }
            }
        }

        // 复制 appsettings 配置文件
        foreach (var service in stationServices)
        {
            var configSource = Path.Combine(sharedServicesDir, $"appsettings.{service}.json");
            if (File.Exists(configSource))
            {
                File.Copy(configSource, Path.Combine(servicesDir, $"appsettings.{service}.json"), true);
                copiedCount++;
            }
        }

        // 复制 uvnc-portable 目录（VNC 远程桌面）
        var uvncSource = Path.Combine(sharedServicesDir, "uvnc-portable");
        if (Directory.Exists(uvncSource))
        {
            CopyDirectory(uvncSource, Path.Combine(servicesDir, "uvnc-portable"), IsPackagingFileIncluded);
            onLog?.Invoke("复制 uvnc-portable 目录", "Info");
        }

        onLog?.Invoke($"图站服务文件复制完成，共复制 {copiedCount} 个文件", "Info");

        // StationSettingUI 有独立的输出目录
        onProgress?.Invoke(35, "正在复制 StationSettingUI...");
        var stationSettingSource = Path.Combine(publishBaseDir, "StationSettingUI");
        var stationSettingDest = Path.Combine(toolsDir, "StationSettingUI");

        if (Directory.Exists(stationSettingSource))
        {
            onLog?.Invoke("复制图站工具: StationSettingUI", "Info");
            CopyDirectory(stationSettingSource, stationSettingDest, IsPackagingFileIncluded);
            onLog?.Invoke("StationSettingUI 复制完成", "Info");
        }
        else
        {
            onLog?.Invoke($"发布目录不存在: {stationSettingSource}，跳过", "Warning");
            _logger.LogWarning("发布目录不存在: {SourceDir}", stationSettingSource);
        }

        onProgress?.Invoke(45, "图站服务复制完成");
    }

    /// <summary>
    /// 从 deps.json 解析运行时所需的全部文件名
    /// 返回 (flatFiles, relativeFiles)：
    ///   flatFiles     - 托管 DLL，发布后在根目录（如 SQLitePCLRaw.core.dll）
    ///   relativeFiles - 原生库，发布后保留 runtimes/{rid}/native/ 子目录结构（如 runtimes/win-x64/native/e_sqlite3.dll）
    /// </summary>
    private (HashSet<string> flatFiles, HashSet<string> relativeFiles) ExtractRequiredFilesFromDeps(string depsJsonPath)
    {
        var flatFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var relativeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(depsJsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("targets", out var targets))
                return (flatFiles, relativeFiles);

            // .NET SDK publish 行为：
            //   runtime 条目（如 lib/netstandard2.0/Foo.dll）→ 发布后扁平化到输出根目录
            //   runtimeTargets / native 条目（如 runtimes/win-x64/native/bar.dll）→ 发布后保留子目录结构
            foreach (var target in targets.EnumerateObject())
            {
                foreach (var package in target.Value.EnumerateObject())
                {
                    // runtime 条目 → 托管 DLL，扁平化到根目录
                    if (package.Value.TryGetProperty("runtime", out var runtime))
                    {
                        foreach (var entry in runtime.EnumerateObject())
                        {
                            var fileName = Path.GetFileName(entry.Name);
                            if (!string.IsNullOrEmpty(fileName))
                                flatFiles.Add(fileName);
                        }
                    }

                    // native 条目 → 原生库，仅保留 win-x64 平台
                    if (package.Value.TryGetProperty("native", out var native))
                    {
                        foreach (var entry in native.EnumerateObject())
                        {
                            var relativePath = entry.Name; // 如 runtimes/win-x64/native/e_sqlite3.dll
                            if (!string.IsNullOrEmpty(relativePath) && relativePath.StartsWith("runtimes/win-x64/"))
                                relativeFiles.Add(relativePath);
                        }
                    }

                    // runtimeTargets 条目 → RID 特定原生库，仅保留 win-x64 平台
                    if (package.Value.TryGetProperty("runtimeTargets", out var runtimeTargets))
                    {
                        foreach (var entry in runtimeTargets.EnumerateObject())
                        {
                            var relativePath = entry.Name; // 如 runtimes/win-x64/native/e_sqlite3.dll
                            if (!string.IsNullOrEmpty(relativePath) && relativePath.StartsWith("runtimes/win-x64/"))
                                relativeFiles.Add(relativePath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 deps.json 失败: {Path}", depsJsonPath);
        }

        return (flatFiles, relativeFiles);
    }

    private void CreateStartupScripts(string outputDir, string platform, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建启动脚本...");

        // Windows启动脚本
        var batContent = @"@echo off
echo ========================================
echo    CJPlug 本地启动包
echo ========================================
echo.

:: 检查.NET运行时
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到.NET运行时，请先安装.NET 10.0
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

echo [信息] 正在启动CJPlug服务...
echo.

:: 启动服务（所有 DLL 在 services 共享目录下）
start ""CJPlug API Server"" /min services\CJ.Plug.ApiServer.exe
start ""CJPlug Web Server"" /min services\CJ.Plug.HostWebServer.exe
start ""CJPlug Elsa Server"" /min services\CJ.Plug.ElsaApiServer.exe
start ""CJPlug MCP Server"" /min services\CJ.Plug.McpServer.exe

echo [成功] CJPlug服务已启动！
echo.
echo 访问地址:
echo   - Web界面: http://localhost:8687
echo   - API服务: http://localhost:8687/api
echo   - MCP服务: http://localhost:8690/mcp
echo.
echo 按任意键停止所有服务...
pause >nul

echo [信息] 正在停止服务...
taskkill /F /IM CJ.Plug.ApiServer.exe >nul 2>&1
taskkill /F /IM CJ.Plug.HostWebServer.exe >nul 2>&1
taskkill /F /IM CJ.Plug.ElsaApiServer.exe >nul 2>&1
taskkill /F /IM CJ.Plug.McpServer.exe >nul 2>&1
echo [成功] 所有服务已停止
pause
";

        File.WriteAllText(Path.Combine(outputDir, "start.bat"), batContent);

        // Linux/macOS启动脚本
        var shContent = @"#!/bin/bash
echo ""========================================""
echo ""    CJPlug 本地启动包""
echo ""========================================""
echo """"

# 检查.NET运行时
if ! command -v dotnet &> /dev/null; then
    echo ""[错误] 未找到.NET运行时，请先安装.NET 10.0""
    echo ""下载地址: https://dotnet.microsoft.com/download/dotnet/10.0""
    exit 1
fi

echo ""[信息] 正在启动CJPlug服务...""
echo """"

# 启动服务（所有 DLL 在 services 共享目录下）
./services/CJ.Plug.ApiServer &
./services/CJ.Plug.HostWebServer &
./services/CJ.Plug.ElsaApiServer &
./services/CJ.Plug.McpServer &

echo ""[成功] CJPlug服务已启动！""
echo """"
echo ""访问地址:""
echo ""  - Web界面: http://localhost:8687""
echo ""  - API服务: http://localhost:8687/api""
echo ""  - MCP服务: http://localhost:8690/mcp""
echo """"
echo ""按Ctrl+C停止所有服务""

# 等待用户中断
trap ""killall CJ.Plug.ApiServer CJ.Plug.HostWebServer CJ.Plug.ElsaApiServer CJ.Plug.McpServer 2>/dev/null"" EXIT
wait
";

        File.WriteAllText(Path.Combine(outputDir, "start.sh"), shContent);

        // 设置执行权限（Linux/macOS）
        if (platform.StartsWith("linux") || platform.StartsWith("osx"))
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{Path.Combine(outputDir, "start.sh")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // 忽略权限设置错误
            }
        }
        
        _progressTracker.AddLog(taskId, "启动脚本创建完成");
    }

    private void CreateConfigurationFiles(string outputDir, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建配置文件...");

        var configDir = Path.Combine(outputDir, "config");
        Directory.CreateDirectory(configDir);

        // 主配置文件
        var appSettingsContent = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""ConnectionStrings"": {
    ""Sqlite"": ""Data Source=../data/main.db;Cache=Shared;"",
    ""Sqlite2"": ""Data Source=../data/plug.db;Cache=Shared;""
  },
  ""LocalMode"": {
    ""Enabled"": true,
    ""AutoStart"": true,
    ""Ports"": {
      ""ApiServer"": 8687,
      ""WebServer"": 8688,
      ""ElsaServer"": 8689,
      ""McpServer"": 8690
    }
  },
  ""Backend"": {
    ""Url"": ""http://localhost:8689/elsa/api""
  }
}";

        File.WriteAllText(Path.Combine(configDir, "appsettings.json"), appSettingsContent);

        // 插件配置文件
        var plugsConfigContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<UserPlugs>
  <!-- 用户自定义插件配置 -->
  <!-- 示例插件配置 -->
  <!--
  <Plug>
    <Name>MyCustomPlug</Name>
    <TypeKey>CustomPlug</TypeKey>
    <Assembly>MyCustomPlug.dll</Assembly>
    <Enabled>true</Enabled>
  </Plug>
  -->
</UserPlugs>";

        File.WriteAllText(Path.Combine(configDir, "user-plugs.xml"), plugsConfigContent);
        
        _progressTracker.AddLog(taskId, "配置文件创建完成");
    }

    private async Task CreateDefaultDatabases(string outputDir, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建默认数据库...");

        var dataDir = Path.Combine(outputDir, "data");
        Directory.CreateDirectory(dataDir);

        // 创建空的SQLite数据库文件
        var mainDbPath = Path.Combine(dataDir, "main.db");
        var plugDbPath = Path.Combine(dataDir, "plug.db");

        // 创建空文件（实际使用时会被EF Core初始化）
        await File.WriteAllBytesAsync(mainDbPath, Array.Empty<byte>());
        await File.WriteAllBytesAsync(plugDbPath, Array.Empty<byte>());
        
        _progressTracker.AddLog(taskId, "默认数据库创建完成");
    }

    private void CreateDockerConfiguration(string outputDir, string platform, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建Docker配置...");

        var dockerContent = @"version: '3.8'

services:
  cjplug-api:
    image: mcr.microsoft.com/dotnet/aspnet:10.0
    container_name: cjplug-api
    ports:
      - ""8687:8080""
    volumes:
      - ./services/ApiServer:/app
      - ./config:/app/config
      - ./data:/app/data
    working_dir: /app
    command: dotnet CJ.Plug.ApiServer.dll
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    restart: unless-stopped

  cjplug-web:
    image: mcr.microsoft.com/dotnet/aspnet:10.0
    container_name: cjplug-web
    ports:
      - ""8688:8080""
    volumes:
      - ./services/HostWebServer:/app
      - ./config:/app/config
    working_dir: /app
    command: dotnet CJ.Plug.HostWebServer.dll
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - cjplug-api
    restart: unless-stopped

  cjplug-elsa:
    image: mcr.microsoft.com/dotnet/aspnet:10.0
    container_name: cjplug-elsa
    ports:
      - ""8689:8080""
    volumes:
      - ./services/ElsaApiServer:/app
      - ./config:/app/config
    working_dir: /app
    command: dotnet CJ.Plug.ElsaApiServer.dll
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - cjplug-api
    restart: unless-stopped

  cjplug-mcp:
    image: mcr.microsoft.com/dotnet/aspnet:10.0
    container_name: cjplug-mcp
    ports:
      - ""8690:8080""
    volumes:
      - ./services/McpServer:/app
      - ./config:/app/config
    working_dir: /app
    command: dotnet CJ.Plug.McpServer.dll
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - cjplug-api
    restart: unless-stopped
";

        File.WriteAllText(Path.Combine(outputDir, "docker-compose.yml"), dockerContent);

        // 创建Docker启动脚本
        var dockerStartContent = @"@echo off
echo ========================================
echo    CJPlug Docker 启动包
echo ========================================
echo.

:: 检查Docker
docker --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到Docker，请先安装Docker Desktop
    echo 下载地址: https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

echo [信息] 正在启动CJPlug Docker容器...
echo.

:: 启动容器
docker-compose up -d

echo [成功] CJPlug Docker容器已启动！
echo.
echo 访问地址:
echo   - Web界面: http://localhost:8687
echo   - API服务: http://localhost:8687/api
echo   - MCP服务: http://localhost:8690/mcp
echo.
echo 管理命令:
echo   - 查看日志: docker-compose logs -f
echo   - 停止服务: docker-compose down
echo   - 重启服务: docker-compose restart
echo.
pause
";

        File.WriteAllText(Path.Combine(outputDir, "start-docker.bat"), dockerStartContent);
        
        _progressTracker.AddLog(taskId, "Docker配置创建完成");
    }

    private void CreateDocumentation(string outputDir, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建文档...");

        var readmeContent = @"# CJPlug 本地启动包

## 概述
本启动包包含CJPlug系统的所有核心服务，可以在本地环境中运行。

## 系统要求
- .NET 10.0 运行时（如果使用自包含版本则不需要）
- Windows 10/11, Linux 或 macOS
- 至少 4GB 可用内存
- 至少 2GB 可用磁盘空间

## 快速开始

### Windows 用户
1. 解压本压缩包到任意目录
2. 双击运行 `start.bat`
3. 等待服务启动完成
4. 访问 http://localhost:8687

### Linux/macOS 用户
1. 解压本压缩包到任意目录
2. 打开终端，进入解压目录
3. 运行命令：`./start.sh`
4. 等待服务启动完成
5. 访问 http://localhost:8687

### Docker 用户（可选）
1. 确保已安装 Docker 和 Docker Compose
2. 双击运行 `start-docker.bat`（Windows）或执行 `docker-compose up -d`
3. 等待容器启动完成
4. 访问 http://localhost:8687

## 服务端口
- **Web界面**: http://localhost:8687
- **API服务**: http://localhost:8687/api
- **Elsa工作流**: http://localhost:8689
- **MCP服务**: http://localhost:8690/mcp

## 目录结构
```
CJPlug-Local/
├── start.bat              # Windows启动脚本
├── start.sh               # Linux/macOS启动脚本
├── start-docker.bat       # Docker启动脚本
├── docker-compose.yml     # Docker配置文件
├── config/                # 配置文件目录
│   ├── appsettings.json   # 主配置文件
│   └── user-plugs.xml     # 插件配置文件
├── data/                  # 数据目录
│   ├── main.db            # 主数据库
│   └── plug.db            # 插件数据库
└── services/              # 服务程序目录（共享DLL）
    ├── CJ.Plug.ApiServer.exe          # API服务器
    ├── CJ.Plug.HostWebServer.exe      # Web服务器
    ├── CJ.Plug.ElsaApiServer.exe      # Elsa工作流引擎
    ├── CJ.Plug.McpServer.exe          # MCP服务器
    └── ...                            # 共享依赖
```

## 配置说明

### 端口配置
编辑 `config/appsettings.json` 文件中的 `LocalMode.Ports` 部分：

```json
""LocalMode"": {
  ""Ports"": {
    ""ApiServer"": 8687,
    ""WebServer"": 8688,
    ""ElsaServer"": 8689,
    ""McpServer"": 8690
  }
}
```

### 数据库配置
默认使用SQLite数据库，数据文件位于 `data/` 目录。

## 故障排除

### 1. 端口被占用
如果端口被占用，可以修改配置文件中的端口号，或者停止占用端口的程序。

### 2. 服务启动失败
- 检查是否安装了正确版本的.NET运行时
- 查看日志文件（位于各服务目录下的logs文件夹）
- 确保有足够的系统权限

### 3. 数据库错误
删除 `data/` 目录下的数据库文件，重新启动服务会自动创建新的数据库。

## 与AI Client集成

### MCP服务地址
```
http://localhost:8690/mcp
```

### 配置示例
在您的AI Client中配置MCP Server地址为上述地址即可。

## 技术支持
- 官方网站: https://github.com/liuszhang/CJPlug
- 问题反馈: https://github.com/liuszhang/CJPlug/issues
- 邮箱: liusz@liusz.com

## 许可证
本软件遵循 MIT 许可证
";

        File.WriteAllText(Path.Combine(outputDir, "README.md"), readmeContent);
        
        _progressTracker.AddLog(taskId, "文档创建完成");
    }

    /// <summary>
    /// 复制目录
    /// </summary>
    /// <param name="sourceDir">源目录</param>
    /// <param name="destDir">目标目录</param>
    /// <param name="fileFilter">可选的文件过滤器，返回 false 则跳过该文件</param>
    /// <param name="dirFilter">可选的目录过滤器，返回 false 则跳过该目录</param>
    private void CopyDirectory(string sourceDir, string destDir,
        Func<string, bool>? fileFilter = null,
        Func<string, bool>? dirFilter = null)
    {
        // 创建目标目录
        Directory.CreateDirectory(destDir);

        // 复制文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            if (fileFilter != null && !fileFilter(file))
                continue;

            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // 递归复制子目录
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            // 跳过 BlazorDebugProxy 目录（仅开发调试用，~11MB）
            var dirName = Path.GetFileName(subDir);
            if (dirName.Equals("BlazorDebugProxy", StringComparison.OrdinalIgnoreCase))
                continue;

            if (dirFilter != null && !dirFilter(dirName))
                continue;

            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(subDir, destSubDir, fileFilter, dirFilter);
        }
    }

    /// <summary>
    /// 部署包文件过滤器：排除仅用于开发的文件，减少发布包体积
    /// 排除项：
    /// - staticwebassets.endpoints.json / staticwebassets.runtime.json（~50MB，开发时 Blazor 静态资源映射）
    /// - PDB 调试符号文件（~6MB）
    /// </summary>
    private static bool IsPackagingFileIncluded(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        // 排除 staticwebassets 文件（开发时 Blazor 静态资源映射，生产部署不需要）
        if (fileName.Contains(".staticwebassets.", StringComparison.OrdinalIgnoreCase))
            return false;

        // 排除 PDB 调试符号文件
        if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private string GetRepositoryRoot()
    {
        // 尝试从当前目录向上查找解决方案文件
        var currentDir = Directory.GetCurrentDirectory();
        var dir = currentDir;

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "CJ.Plug-Aspire.sln")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        // 如果找不到，尝试从程序集位置推断
        var assemblyDir = AppContext.BaseDirectory;
        dir = assemblyDir;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "CJ.Plug-Aspire.sln")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        // 最后，返回当前目录的父目录（假设在src/PlugApiServer/CJ.Plug.ApiServer中）
        return Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
    }

    /// <summary>
    /// 创建图站启动脚本
    /// </summary>
    private void CreateStationStartupScripts(string outputDir, string platform, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建图站启动脚本...");
        CreateStationStartupScriptsCore(outputDir, platform);
        _progressTracker.AddLog(taskId, "图站启动脚本创建完成");
    }

    private void CreateStationStartupScriptsInternal(string outputDir, string platform)
    {
        CreateStationStartupScriptsCore(outputDir, platform);
    }

    private void CreateStationStartupScriptsCore(string outputDir, string platform)
    {

        // Windows启动脚本
        var batContent = @"@echo off
echo ========================================
echo    CJPlug 图站部署启动包
echo ========================================
echo.

echo [信息] 正在启动图站服务...
echo.

:: 启动图站服务（所有 DLL 在 services 共享目录下）
start ""CJPlug Station Agent"" /min services\CJ.Plug.StationAgent.exe
start ""CJPlug Station ApiServer"" /min services\CJ.Plug.StationApiServer.exe

echo [成功] 图站服务已启动！
echo.
echo 图站服务将在后台运行，与主服务器建立连接
echo.
echo 提示: tools\StationSettingUI 目录下有图站桌面管理工具，可双击 StationSettingUI.exe 使用
echo.
echo 按任意键停止所有服务...
pause >nul

echo [信息] 正在停止服务...
taskkill /F /IM CJ.Plug.StationAgent.exe >nul 2>&1
taskkill /F /IM CJ.Plug.StationApiServer.exe >nul 2>&1
echo [成功] 所有图站服务已停止
pause
";

        File.WriteAllText(Path.Combine(outputDir, "start.bat"), batContent);

        // Linux/macOS启动脚本
        var shContent = @"#!/bin/bash
echo ""========================================""
echo ""    CJPlug 图站部署启动包""
echo ""========================================""
echo """"

echo ""[信息] 正在启动图站服务...""
echo """"

# 启动图站服务（所有 DLL 在 services 共享目录下）
./services/CJ.Plug.StationAgent &
./services/CJ.Plug.StationApiServer &

echo ""[成功] 图站服务已启动！""
echo """"
echo ""图站服务将在后台运行，与主服务器建立连接""
echo """"
echo ""提示: tools/StationSettingUI 目录下有图站桌面管理工具""
echo """"
echo ""按Ctrl+C停止所有服务""

trap ""killall CJ.Plug.StationAgent CJ.Plug.StationApiServer 2>/dev/null"" EXIT
wait
";

        File.WriteAllText(Path.Combine(outputDir, "start.sh"), shContent);

        // 设置执行权限（Linux/macOS）
        if (platform.StartsWith("linux") || platform.StartsWith("osx"))
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{Path.Combine(outputDir, "start.sh")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // 忽略权限设置错误
            }
        }
    }

    /// <summary>
    /// 创建图站配置文件
    /// </summary>
    private void CreateStationConfigurationFiles(string outputDir, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建图站配置文件...");
        CreateStationConfigurationFilesCore(outputDir);
        _progressTracker.AddLog(taskId, "图站配置文件创建完成");
    }

    private void CreateStationConfigurationFilesInternal(string outputDir)
    {
        CreateStationConfigurationFilesCore(outputDir);
    }

    private void CreateStationConfigurationFilesCore(string outputDir)
    {
        var configDir = Path.Combine(outputDir, "config");
        Directory.CreateDirectory(configDir);

        // 图站配置文件 - 需要配置主服务器连接地址
        var appSettingsContent = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""Station"": {
    ""StationName"": ""MyStation"",
    ""StationCategory"": ""Standard"",
    ""StationDescription"": """",
    ""StationBasePath"": ""C:\\tmp""
  },
  ""MainServer"": {
    ""Url"": ""http://localhost:8687"",
    ""SignalRHub"": ""http://localhost:8687/hubs/station""
  },
  ""ConnectionStrings"": {
    ""Sqlite"": ""Data Source=../data/station.db;Cache=Shared;""
  }
}";

        File.WriteAllText(Path.Combine(configDir, "appsettings.json"), appSettingsContent);
    }

    /// <summary>
    /// 创建图站文档
    /// </summary>
    private void CreateStationDocumentation(string outputDir, string taskId)
    {
        _progressTracker.AddLog(taskId, "创建图站文档...");
        CreateStationDocumentationCore(outputDir);
        _progressTracker.AddLog(taskId, "图站文档创建完成");
    }

    private void CreateStationDocumentationInternal(string outputDir)
    {
        CreateStationDocumentationCore(outputDir);
    }

    private void CreateStationDocumentationCore(string outputDir)
    {
        var readmeContent = @"# CJPlug 图站部署包

## 概述
本部署包包含CJPlug图站服务的所有核心程序，用于在计算节点上部署图站服务，与主服务器建立连接。同时包含 StationSettingUI 桌面管理工具。

## 系统要求
- .NET 10.0 运行时
- Windows 10/11, Linux 或 macOS
- 至少 2GB 可用内存
- 至少 1GB 可用磁盘空间

## 快速开始

### Windows 用户
1. 解压本压缩包到任意目录
2. 编辑 `config/appsettings.json`，配置主服务器地址
3. 双击运行 `start.bat`
4. 等待图站服务启动完成
5. 进入 `tools\StationSettingUI` 目录，双击 `StationSettingUI.exe` 启动管理工具

### Linux/macOS 用户
1. 解压本压缩包到任意目录
2. 编辑 `config/appsettings.json`，配置主服务器地址
3. 打开终端，进入解压目录
4. 运行命令：`./start.sh`
5. 等待图站服务启动完成

## 目录结构
```
CJPlug-Station/
├── start.bat              # Windows启动脚本
├── start.sh               # Linux/macOS启动脚本
├── config/                # 配置文件目录
│   └── appsettings.json   # 图站配置文件
├── services/              # 服务程序目录（共享DLL）
│   ├── CJ.Plug.StationAgent.exe       # 图站代理服务
│   ├── CJ.Plug.StationApiServer.exe   # 图站API服务
│   └── ...                            # 共享依赖
└── tools/                 # 管理工具目录
    └── StationSettingUI/         # 图站桌面管理工具
```

### 使用管理工具
进入 `tools\StationSettingUI` 目录，双击 `StationSettingUI.exe` 即可启动图站桌面管理工具，用于管理图站的工具注册、配置等。

## 配置说明

### 主服务器连接
编辑 `config/appsettings.json` 文件中的 `MainServer` 部分：

```json
""MainServer"": {
  ""Url"": ""http://your-server-ip:8687"",
  ""SignalRHub"": ""http://your-server-ip:8687/hubs/station""
}
```

### 图站信息配置
```json
""Station"": {
  ""StationName"": ""MyStation"",
  ""StationCategory"": ""Standard"",
  ""StationDescription"": """",
  ""StationBasePath"": ""C:\\tmp""
}
```

- `StationName`: 图站名称，用于在主服务器上标识该图站
- `StationCategory`: 图站类型
- `StationBasePath`: 图站工作目录，工具文件存放的根路径

## 故障排除

### 1. 图站无法连接到主服务器
- 确保主服务地址配置正确
- 检查网络连接和防火墙设置
- 确认主服务器端口可访问

### 2. 服务启动失败
- 检查是否安装了正确版本的.NET运行时
- 查看日志文件
- 确保有足够的系统权限

## 技术支持
- 官方网站: https://github.com/liuszhang/CJPlug
- 问题反馈: https://github.com/liuszhang/CJPlug/issues
";

        File.WriteAllText(Path.Combine(outputDir, "README.md"), readmeContent);
    }
}