using Serilog;

namespace CJ.Plug.StationApiServer.Services;

/// <summary>
/// VNC 自启动服务
/// 在 StationApiServer 启动时，自动部署并启动 UltraVNC portable。
/// 通过 appsettings.json 中 "RemoteDesktop:AutoStartVnc" 控制（默认 true）。
/// </summary>
public class VncAutoStartService : BackgroundService
{
    private readonly UltraVncService _uvncService;
    private readonly IConfiguration _configuration;

    public VncAutoStartService(UltraVncService uvncService, IConfiguration configuration)
    {
        _uvncService = uvncService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 读取配置，默认为 true（自动启动）
        var autoStart = _configuration.GetValue<bool>("RemoteDesktop:AutoStartVnc", true);
        if (!autoStart)
        {
            Log.Information("VNC 自启动已禁用 (RemoteDesktop:AutoStartVnc = false)");
            return;
        }

        // 延迟一小段时间，等待 Kestrel 等核心服务先就绪
        await Task.Delay(2000, stoppingToken);

        try
        {
            Log.Information("=== VNC 自启动服务开始 ===");

            // 1. 检查是否已部署，未部署则自动部署
            var status = _uvncService.GetStatus();
            if (!status.IsDeployed)
            {
                Log.Information("UltraVNC 未部署，开始自动部署...");
                var (deploySuccess, deployMsg) = await _uvncService.DeployAsync();
                if (!deploySuccess)
                {
                    Log.Warning("UltraVNC 自动部署失败: {Message}。跳过 VNC 自启动。", deployMsg);
                    return;
                }
                Log.Information("UltraVNC 自动部署成功: {Message}", deployMsg);
            }
            else
            {
                Log.Information("UltraVNC 已部署于: {Path}", status.DeployPath);
            }

            // 2. 检查是否已在运行
            status = _uvncService.GetStatus();
            if (status.IsRunning)
            {
                Log.Information("UltraVNC 已在运行中 (PID: {Pid})，跳过启动", status.ProcessId);
                return;
            }

            // 3. 启动 UltraVNC
            var (startSuccess, startMsg) = await _uvncService.StartAsync();
            if (startSuccess)
            {
                Log.Information("UltraVNC 自启动成功: {Message}", startMsg);
            }
            else
            {
                Log.Warning("UltraVNC 自启动失败: {Message}", startMsg);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "VNC 自启动服务异常");
        }
    }
}
