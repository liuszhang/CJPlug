using CJ.Plug.McpServer.Services;

namespace CJ.Plug.ApiServer.Infrastructure;

/// <summary>
/// MCP 动态工具缓存的启动器 + 周期兜底（2026-10-29《CJPlug 服务架构简化》Phase 4）。
/// <para>
/// 合并后 MCP 与 ApiServer 在**同一个进程**里，<see cref="DynamicToolRegistry.RefreshAsync"/>
/// 实际是「进程通过 HTTP 调自己 8687」。因此不能在 app.Run() 之前 await 刷新——那时 Kestrel
/// 还没绑定端口，请求必然失败并被回落成空缓存。
/// </para>
/// <para>
/// 同时修掉一个既有缺陷：原 McpServer 只在启动时刷新一次，若启动时 ApiServer 尚未就绪
/// （实测复现：先起 McpServer 再起 ApiServer ⇒ 永远 0 工具且不自愈），此后只靠 SignalR
/// 增量通知。这里加 60 秒周期刷新作为兜底。
/// </para>
/// </summary>
// P9（2026-10-30）：由 internal 改 public —— 装配入口搬到独立项目 CJPlug.Server 后，
//   `builder.Services.AddHostedService<PlugMcpToolRefreshService>()` 需要跨程序集可见。
//   本类只做「启动后延迟刷新 + 60s 兜底」，无内部状态泄漏风险。
public sealed class PlugMcpToolRefreshService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    private readonly DynamicToolRegistry _registry;
    private readonly ILogger<PlugMcpToolRefreshService> _logger;

    public PlugMcpToolRefreshService(DynamicToolRegistry registry, ILogger<PlugMcpToolRefreshService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 等 Kestrel 绑定 + 数据库种子跑完：本进程既是 MCP 服务方，也是自己 HTTP 拉取的客户端。
            await Task.Delay(StartupDelay, stoppingToken);

            await _registry.RefreshAsync(stoppingToken);
            await _registry.StartAsync(stoppingToken);

            _logger.LogInformation("[MCP] 动态工具首轮刷新完成，开始 {Interval}s 周期兜底刷新", RefreshInterval.TotalSeconds);

            using var timer = new PeriodicTimer(RefreshInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await _registry.RefreshAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停机
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP] 工具刷新后台任务异常退出");
        }
    }
}
