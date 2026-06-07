using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.Desktop.Services;

public class AppHostLauncher : IDisposable
{
    private const string DashboardUrl = "http://localhost:15288";
    private const string DllName = "CJ.Plug.AspireHost.AppHost.dll";
    private const int ReadyTimeoutSeconds = 30;

    private readonly ILogger<AppHostLauncher> _logger;
    private readonly StringBuilder _outputBuffer = new();
    private Process? _process;
    private bool _ownsProcess;
    private bool _disposed;

    /// <summary>AppHost 进程输出一行时触发（stdout 或 stderr）。</summary>
    public event Action<string>? OutputReceived;

    /// <summary>AppHost 进程是否正在运行（包括外部已有实例）。</summary>
    public bool IsRunning => (_process != null && !_process.HasExited) || _externalInstanceDetected;

    /// <summary>当前 AppHost 是否为本程序启动（false 表示是外部已有实例）。</summary>
    public bool OwnsProcess => _ownsProcess;

    private volatile bool _externalInstanceDetected;

    public AppHostLauncher(ILogger<AppHostLauncher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 启动 AppHost 子进程并等待 Dashboard 就绪。
    /// 如果外部已有 AppHost 实例在运行，则跳过启动，直接复用。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 先清除陈旧的外部实例标记，再重新检测
        _externalInstanceDetected = false;

        // 检查是否已有外部 AppHost 在运行（避免端口冲突）
        if (await IsDashboardReachableAsync(cancellationToken))
        {
            _logger.LogInformation("检测到 AppHost 已在运行 ({Url})，跳过启动", DashboardUrl);
            _externalInstanceDetected = true;
            _ownsProcess = false;
            OutputReceived?.Invoke("[INFO] 检测到 AppHost 服务已在运行，直接复用现有实例。");
            return;
        }

        var dllPath = FindAppHostDll();
        if (dllPath == null)
        {
            _logger.LogError("未能找到 {DllName}，请确认 AppHost 项目已编译", DllName);
            throw new FileNotFoundException($"找不到 {DllName}，请先编译 CJ.Plug.AspireHost.AppHost 项目。");
        }

        _logger.LogInformation("启动 AppHost 进程: {DllPath}", dllPath);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _process = Process.Start(psi);
        if (_process == null)
        {
            _logger.LogError("无法启动 AppHost 进程");
            throw new InvalidOperationException("无法启动 AppHost 进程。");
        }

        _ownsProcess = true;

        // 后台读取输出流，避免子进程输出缓冲区满导致阻塞
        _ = Task.Run(() => ReadOutputStream(_process.StandardOutput), cancellationToken);
        _ = Task.Run(() => ReadErrorStream(_process.StandardError), cancellationToken);

        await WaitForAppHostReady(cancellationToken);
    }

    /// <summary>
    /// 快速 ping Dashboard URL，检查是否已有 AppHost 实例在运行。
    /// </summary>
    private static async Task<bool> IsDashboardReachableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await httpClient.GetAsync(DashboardUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 轮询 Dashboard URL 直到返回 200，超时 30 秒。
    /// </summary>
    private async Task WaitForAppHostReady(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(ReadyTimeoutSeconds);

        while (sw.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_process?.HasExited == true)
            {
                _logger.LogError("AppHost 进程已退出，退出码: {ExitCode}", _process.ExitCode);
                throw new InvalidOperationException($"AppHost 进程意外退出，退出码: {_process.ExitCode}");
            }

            try
            {
                var response = await httpClient.GetAsync(DashboardUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AppHost Dashboard 已就绪 (耗时 {Elapsed}ms)", sw.ElapsedMilliseconds);
                    return;
                }
            }
            catch
            {
                // 忽略连接失败，继续轮询
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"等待 AppHost Dashboard 就绪超时 ({ReadyTimeoutSeconds} 秒)。");
    }

    /// <summary>
    /// 返回自 AppHostLauncher 创建以来缓冲的全部控制台输出文本。
    /// </summary>
    public string GetBufferedOutput() => _outputBuffer.ToString();

    /// <summary>
    /// 重启 AppHost 进程：先 Stop 再 Start。
    /// </summary>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync();
        await StartAsync(cancellationToken);
    }

    /// <summary>
    /// 停止 AppHost 进程（本程序启动的或外部已有实例）。
    /// </summary>
    public async Task StopAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            _logger.LogInformation("正在关闭 AppHost 进程树 (PID: {Pid})...", _process.Id);
            try
            {
                _process.Kill(entireProcessTree: true);
                await Task.Run(() => _process.WaitForExit(10000));
                _logger.LogInformation(_process.HasExited
                    ? "AppHost 进程树已终止"
                    : "AppHost 进程树终止等待超时");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止 AppHost 进程树时发生错误");
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
        else if (_externalInstanceDetected)
        {
            // 外部实例：通过端口反查进程 PID 并终止
            _logger.LogInformation("正在终止外部 AppHost 实例 (端口 {Port})...", 15288);
            try
            {
                await KillProcessByPortAsync(15288);
                OutputReceived?.Invoke("[INFO] 外部 AppHost 实例已停止。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止外部 AppHost 实例时发生错误");
                OutputReceived?.Invoke($"[ERROR] 终止外部 AppHost 失败: {ex.Message}");
            }
        }

        _externalInstanceDetected = false;
        _ownsProcess = false;
    }

    /// <summary>
    /// 根据端口号反查占用进程并强制终止（Windows: netstat）。
    /// </summary>
    private static async Task KillProcessByPortAsync(int port)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c netstat -ano | findstr :{port}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var netstatProc = Process.Start(psi);
        if (netstatProc == null)
            throw new InvalidOperationException("无法启动 netstat 查询进程。");

        var output = await netstatProc.StandardOutput.ReadToEndAsync();
        await netstatProc.WaitForExitAsync();

        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException($"未找到占用端口 {port} 的进程。");

        // 解析 netstat 输出: "  TCP    0.0.0.0:15288    ...    LISTENING       12345"
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains("LISTENING"))
                continue;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
                continue;

            var pidStr = parts[^1]; // 最后一列是 PID
            if (int.TryParse(pidStr, out var pid) && pid > 0)
            {
                try
                {
                    using var targetProc = Process.GetProcessById(pid);
                    targetProc.Kill(entireProcessTree: true);
                    await Task.Run(() => targetProc.WaitForExit(10000));
                    return; // 找到并终止后退出
                }
                catch (ArgumentException)
                {
                    // 进程已退出，继续检查下一行
                    continue;
                }
            }
        }

        throw new InvalidOperationException($"未能终止占用端口 {port} 的进程。");
    }

    /// <summary>
    /// 按优先级自动探测 AppHost DLL 路径。
    /// 项目设置了 BaseOutputPath → 02.Publish 目录，优先探测该路径。
    /// </summary>
    private string? FindAppHostDll()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Desktop 输出: src/WebHost/CJ.Plug.Desktop/bin/Debug/net10.0-windows/
        // AppHost 输出: 02.Publish/CJ.Plug.AspireHost.AppHost/Debug/net10.0/
        // 向上 6 层到达项目根 (D:\Pro\CJ.Plug-Aspire)
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", ".."));

        var candidates = new List<string>
        {
            // 优先级 1: BaseOutputPath → 02.Publish (Debug)
            Path.Combine(projectRoot, "02.Publish", "CJ.Plug.AspireHost.AppHost", "Debug", "net10.0", DllName),
            // 优先级 2: BaseOutputPath → 02.Publish (Release)
            Path.Combine(projectRoot, "02.Publish", "CJ.Plug.AspireHost.AppHost", "Release", "net10.0", DllName),
            // 优先级 3: 标准 bin/Debug/net10.0
            Path.Combine(projectRoot, "src", "Framework", "CJ.Plug.AspireHost.AppHost", "bin", "Debug", "net10.0", DllName),
            // 优先级 4: 标准 bin/Release/net10.0
            Path.Combine(projectRoot, "src", "Framework", "CJ.Plug.AspireHost.AppHost", "bin", "Release", "net10.0", DllName),
        };

        foreach (var path in candidates)
        {
            var fullPath = Path.GetFullPath(path);
            _logger.LogDebug("探测 AppHost DLL: {Path}", fullPath);
            if (File.Exists(fullPath))
            {
                _logger.LogInformation("找到 AppHost DLL: {Path}", fullPath);
                return fullPath;
            }
        }

        return null;
    }

    private void ReadOutputStream(StreamReader reader)
    {
        try
        {
            while (reader.ReadLine() is { } line)
            {
                _logger.LogInformation("[AppHost] {Line}", line);
                _outputBuffer.AppendLine(line);
                OutputReceived?.Invoke(line);
            }
        }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    private void ReadErrorStream(StreamReader reader)
    {
        try
        {
            while (reader.ReadLine() is { } line)
            {
                _logger.LogError("[AppHost:ERR] {Line}", line);
                _outputBuffer.AppendLine(line);
                OutputReceived?.Invoke(line);
            }
        }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_process is { HasExited: false })
            {
                _logger.LogInformation("Dispose 中终止 AppHost 进程树 (PID: {Pid})", _process.Id);
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dispose 中终止 AppHost 进程树时发生错误");
        }

        _process?.Dispose();
        _process = null;
    }
}
