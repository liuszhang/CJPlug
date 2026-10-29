namespace CJ.Plug.Models.Shared
{
    /// <summary>
    /// 进程级「本进程是否已开始对外服务」标志。
    ///
    /// 为什么需要它（2026-10-29，文档 K.8.4）：
    ///   合并成单端口单进程后，很多组件仍在启动期**通过 HTTP 自调用本进程**的 API。但 Kestrel 要等
    ///   <c>IHostedService.StartAsync</c> 链跑完才真正监听，于是这类自调用必然拿到
    ///   <c>Connection refused</c>，并且 HttpClient 会把完整的 inner stack 打出来 —— 纯噪音，
    ///   还会白等超时（实测单次 4 秒）。
    ///
    /// 用静态标志而不是注入 <c>IHostApplicationLifetime</c>：
    ///   · 使用方（如 Elsa 的活动提供者）由第三方容器创建，构造函数签名改动有兼容风险；
    ///   · 这是纯**观测事实**，不携带行为，静态读取最直接，也不会引入生命周期依赖。
    ///
    /// 未标记时默认 <c>false</c> ⇒ 调用方应"跳过自调用"而不是"当作已就绪"。
    /// </summary>
    public static class HostStartupSignal
    {
        private static volatile bool _started;
        private static int _marked;

        /// <summary>本进程是否已完成启动（Kestrel 已在监听）。</summary>
        public static bool IsStarted => _started;

        /// <summary>
        /// 由宿主的 <c>ApplicationStarted</c> 回调调用。幂等。
        /// 打一条一次性日志：它同时是「守卫解除」的可观测标志（排查启动期问题时能直接看出分界点）。
        /// </summary>
        public static void MarkStarted()
        {
            _started = true;

            if (Interlocked.Exchange(ref _marked, 1) == 0)
            {
                Console.WriteLine("[Host] 已开始监听（ApplicationStarted）：启动期自调用守卫解除");
            }
        }
    }
}
