using System.Collections.Concurrent;
using System.Threading;

namespace CJ.Plug.PlugBaseCore.Services;

/// <summary>
/// 工具包下载互斥锁，按"图站IP + 工具名 + 版本"保证同一工具在同一图站上只下载一次。
/// </summary>
public class ToolDownloadGuard
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// 获取指定工具在图站的下载锁。
    /// 返回 IDisposable，dispose 时释放锁。
    /// key = "{stationIp}:{toolName}:{toolVersion}"
    /// </summary>
    public async Task<IDisposable> AcquireAsync(string stationIp, string toolName, string toolVersion, CancellationToken ct)
    {
        var key = $"{stationIp}:{toolName}:{toolVersion}";
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new Releaser(semaphore);
    }

    private class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        public Releaser(SemaphoreSlim s) => _semaphore = s;
        public void Dispose() => _semaphore.Release();
    }
}
