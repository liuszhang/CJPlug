using CJ.Plug.Models.Shared;
using Microsoft.Extensions.Http;

namespace CJ.Plug.HostWebServer.Shell;

/// <summary>
/// 给 <c>IHttpClientFactory</c> 创建的**全部** HttpClient 统一追加 <see cref="InternalApiKey.InternalApiKeyHandler"/>
/// （2026-10-29，P5.8 方案 C 的 C2 地基）。
///
/// <para><b>为什么用 IHttpMessageHandlerBuilderFilter 而不是逐个改 <c>AddHttpClient</c></b>：
/// 全仓 53 个注册点散在 30 个文件（18 个模块的 <c>ServiceCollectionExtensions</c> + 5 个宿主 <c>Program.cs</c>
/// + A2A/Plugin 若干），逐个串 handler 既改不完也保不住（新增模块必漏）。
/// 本接口是 .NET 官方给"所有工厂客户端统一加工"留的钩子 —— <b>一处注册覆盖全部</b>，且对新增注册点自动生效。</para>
///
/// <para><b>为什么不直接把 handler 塞进 <c>ConfigurePrimaryHttpMessageHandler</c></b>：那会替换掉模块
/// 各自可能已配的 primary handler（如代理/证书回调），属于破坏性改动。加入 <c>AdditionalHandlers</c>
/// 是**追加**在管道里，不与任何现有配置冲突。</para>
///
/// <para>⚠ <b>覆盖不到的两类</b>（必须单独接 <see cref="InternalApiKey.CreateClient"/>）：
/// ① <c>new HttpClient()</c> 裸实例（<c>BaseApiClient.httpClient</c>、<c>MainApiClient</c> 的审计客户端等）；
/// ② 手工 <c>new HttpClient(自定义DelegatingHandler)</c>（如 <c>ElsaApiClient</c> 的 <c>ElsaJwtAuthHandler</c> 包装）。</para>
/// </summary>
public sealed class InternalApiKeyHttpFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            // 先让其它 filter（框架内置 + 第三方）跑完，保持既有管道顺序不变
            next(builder);

            // 未配置密钥 ⇒ 完全不介入（保留全匿名行为，零风险）
            if (!InternalApiKey.IsEnabled) return;

            // ⚠ 幂等守卫：本 filter 可能被多次注册（多宿主/重复 AddPlugWebHost），
            //   重复追加会让请求带两遍同一个头。
            if (builder.AdditionalHandlers.Any(h => h is InternalApiKey.InternalApiKeyHandler)) return;

            builder.AdditionalHandlers.Add(new InternalApiKey.InternalApiKeyHandler());
        };
    }
}
