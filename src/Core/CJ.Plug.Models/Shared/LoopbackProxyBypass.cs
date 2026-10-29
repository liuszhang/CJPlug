using System.Net;

namespace CJ.Plug.Models.Shared
{
    /// <summary>
    /// 进程级「环回地址绕过系统代理」一次性配置（幂等）。
    ///
    /// 背景（2026-10-29，文档 K.7）：
    ///   本机配了 <c>http_proxy=127.0.0.1:60819</c> 后，服务连**自己**的 <c>http://localhost:5066/</c>
    ///   也被送进代理 ⇒ 代理对环回无路由，返回 <c>502 Bad Gateway</c>。
    ///   表现为：SignalR Hub 前 3 次重试全 502、第 4 次才连上；curl 冒烟被误判成"服务崩了"。
    ///   客户现场配了上网行为管理/系统代理时同样会中招，因此按缺陷处理，不能算环境特例。
    ///
    /// 为什么改 <see cref="HttpClient.DefaultProxy"/> 而不是逐个改 AddHttpClient：
    ///   · 全仓 40+ 处 <c>AddHttpClient</c>、5 处 <c>new HubConnectionBuilder()</c>，逐个配 handler 面太大；
    ///   · <see cref="HttpClient.DefaultProxy"/> 是进程级默认，且 **SignalR 内部 <c>new HttpClient()</c> 也吃它**
    ///     ⇒ 一处设置覆盖全部，行为一致。
    ///
    /// 为什么不干脆 <c>UseProxy = false</c>：
    ///   · 那会让 LLM / 外部 API 等**本该走代理**的请求也直连，内网只能靠代理出网的环境会被打断。
    ///   · 这里只把环回地址加入 <c>BypassList</c>，其余地址的代理行为**完全不变**。
    /// </summary>
    public static class LoopbackProxyBypass
    {
        private static int _applied;

        /// <summary>
        /// 匹配环回目标的正则（<see cref="WebProxy"/> 对 BypassList 匹配的是 <c>Uri.AbsoluteUri</c>，
        /// 不是 Host —— 这一点很容易写错，写成 <c>^localhost$</c> 会完全失效）。
        /// </summary>
        private static readonly string[] LoopbackPatterns =
        {
            @"^https?://localhost([:/].*)?$",
            @"^https?://127\.0\.0\.1([:/].*)?$",
            @"^https?://\[::1\]([:/].*)?$",
            @"^https?://0\.0\.0\.0([:/].*)?$",
        };

        /// <summary>
        /// 应用环回绕过。可重复调用，只有第一次生效。任何异常都被吞掉 —— 它只是可用性优化，
        /// 绝不允许因为代理配置把服务启动搞挂。
        /// </summary>
        public static void Apply()
        {
            if (Interlocked.Exchange(ref _applied, 1) == 1)
            {
                return;
            }

            try
            {
                // 情况 1：当前默认代理已经是 WebProxy（Windows 上来自 WinINET 系统设置）
                //   → 克隆一份再补 BypassList，不动原对象（可能被别处共享）。
                if (HttpClient.DefaultProxy is WebProxy webProxy && webProxy.Address is not null)
                {
                    HttpClient.DefaultProxy = CloneWithLoopbackBypass(webProxy);
                    return;
                }

                // 情况 2：默认代理是 HttpEnvironmentProxy（internal，读 HTTP_PROXY 等环境变量）
                //   → 自己读环境变量重建，这是本机实际命中的分支。
                var proxyUri = TryReadProxyFromEnvironment();
                if (proxyUri is null)
                {
                    // 直连环境：环回本来就不走代理，无需处理。
                    return;
                }

                HttpClient.DefaultProxy = new WebProxy(proxyUri)
                {
                    // 刻意**不**设 BypassProxyOnLocal：那会让所有不含点的内网单标签主机名
                    // （如 http://fileserver）也绕过代理，改变既有行为。只精确放行环回。
                    BypassList = (string[])LoopbackPatterns.Clone(),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Proxy] 环回绕过代理配置失败（不影响启动）：{ex.Message}");
            }
        }

        private static WebProxy CloneWithLoopbackBypass(WebProxy source)
        {
            var clone = new WebProxy(source.Address!)
            {
                BypassProxyOnLocal = source.BypassProxyOnLocal,
                UseDefaultCredentials = source.UseDefaultCredentials,
                Credentials = source.Credentials,
            };

            var patterns = new List<string>(source.BypassList ?? Array.Empty<string>());
            foreach (var pattern in LoopbackPatterns)
            {
                if (!patterns.Contains(pattern, StringComparer.OrdinalIgnoreCase))
                {
                    patterns.Add(pattern);
                }
            }

            clone.BypassList = patterns.ToArray();
            return clone;
        }

        private static Uri? TryReadProxyFromEnvironment()
        {
            // 上/小写都读：Linux 与 Windows 的习惯不同，容器里常见小写。
            string[] keys =
            {
                "HTTP_PROXY", "http_proxy",
                "HTTPS_PROXY", "https_proxy",
                "ALL_PROXY", "all_proxy",
            };

            foreach (var key in keys)
            {
                var raw = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (Uri.TryCreate(raw.Contains("://", StringComparison.Ordinal) ? raw : $"http://{raw}", UriKind.Absolute, out var uri))
                {
                    return uri;
                }
            }

            return null;
        }
    }
}
