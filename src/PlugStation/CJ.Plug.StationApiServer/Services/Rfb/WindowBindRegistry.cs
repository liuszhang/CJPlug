using Serilog;

namespace CJ.Plug.StationApiServer.Services.Rfb
{
    /// <summary>
    /// 窗口绑定注册表：管理"单窗口 VNC 目标"（PID 主 / 进程名辅）。
    /// 供绑定 API、StationAgent PID 上报调用；RfbWindowVncServer 消费当前目标。
    /// </summary>
    public class WindowBindRegistry
    {
        private readonly object _lock = new();
        private WindowTarget? _current;
        private string? _sessionKey;
        private DateTime _boundAt = DateTime.MinValue;

        /// <summary>当前绑定目标（可能为 null）。</summary>
        public WindowTarget? Current
        {
            get { lock (_lock) return _current; }
        }

        /// <summary>当前绑定会话键（PDZ/任务关联）。</summary>
        public string? SessionKey
        {
            get { lock (_lock) return _sessionKey; }
        }

        public bool HasBinding
        {
            get { lock (_lock) return _current != null; }
        }

        /// <summary>
        /// 设置/覆盖绑定。重复调用幂等覆盖。
        /// </summary>
        public void SetTarget(WindowTarget target, string? sessionKey = null)
        {
            lock (_lock)
            {
                _current = target;
                if (!string.IsNullOrEmpty(sessionKey))
                    _sessionKey = sessionKey;
                _boundAt = DateTime.Now;
                Log.Information("VNC 窗口绑定已设置: PID={Pid}, 进程名={Name}, 会话={Key}",
                    target.ProcessId, target.ProcessName, _sessionKey);
            }
        }

        /// <summary>清除绑定（工具退出/手动解绑）。</summary>
        public void Clear(string? sessionKey = null)
        {
            lock (_lock)
            {
                if (sessionKey != null && _sessionKey != null && sessionKey != _sessionKey)
                    return; // 会话不匹配不清理
                _current = null;
                _sessionKey = null;
                Log.Information("VNC 窗口绑定已清除");
            }
        }

        /// <summary>绑定是否已超时失效（工具启动等待上限）。</summary>
        public bool IsExpired(TimeSpan timeout)
        {
            lock (_lock)
            {
                return _boundAt != DateTime.MinValue && DateTime.Now - _boundAt > timeout;
            }
        }
    }
}
