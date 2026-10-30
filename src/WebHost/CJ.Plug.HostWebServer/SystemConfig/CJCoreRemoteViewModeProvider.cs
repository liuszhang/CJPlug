using CJ.Plug.PlugBaseCore.Contracts;
using CJCore.Modules.SystemConfig.Abstractions;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// <see cref="IRemoteViewModeProvider"/> 的主宿主实现：直接读 CJCore 系统配置快照
/// （与 <c>/system-config</c> 页面同一份数据、同一进程，<b>不再有任何 HTTP 往返</b>）。
///
/// <para><b>P7.1a 背景</b>：本值原先由 <c>ToolExecuteService</c> 经
/// <c>MainApiClient.SystemConfigApiClient</c> 打 <c>/api/systemconfig/get/RemoteViewMode</c> 读取；
/// 那条通道随 CJPlug 私有配置模块一起退役，读写两侧统一收敛到 CJCore。</para>
///
/// <para><b>为什么取快照而不是再注入 <see cref="ISystemConfigStore"/></b>：
/// <see cref="ISystemConfigProvider.GetSnapshotAsync"/> 已是「启动时加载 + 保存后重建」的缓存视图，
/// 再走一次 store 会多一跳 DB 且绕开内存缓存；而配置页保存后缓存立即刷新
/// ⇒ 本实现天然是「保存即生效」，不需要额外的失效逻辑。</para>
///
/// <para><b>失败语义</b>：键不存在 / 值为空 / 读快照抛异常一律返回 <c>null</c>
/// （调用方按 <see cref="RemoteViewModes.Fullscreen"/> 兜底）。配置面故障不得外溢到工具执行链 ——
/// 这是本类唯一的对外契约。</para>
/// </summary>
public sealed class CJCoreRemoteViewModeProvider(
    ISystemConfigProvider provider,
    ILogger<CJCoreRemoteViewModeProvider> logger) : IRemoteViewModeProvider
{
    public async Task<string?> GetRemoteViewModeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await provider.GetSnapshotAsync(cancellationToken);
            var value = snapshot?.GetValue(PlugSystemConfigKeys.RemoteViewMode);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (Exception ex)
        {
            // 只记一次告警，不抛：工具执行链不因配置读取失败而中断。
            logger.LogWarning(ex, "[P7.1a] 读取 RemoteViewMode 失败，按 {Default} 处理",
                PlugSystemConfigKeys.DefaultRemoteViewMode);
            return null;
        }
    }
}
