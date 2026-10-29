using System.Security.Cryptography;
using System.Text;

namespace CJ.Plug.Models.Shared;

/// <summary>
/// 内部信任密钥（2026-10-29，P5.8 方案 C 的 **C2 地基**）。
///
/// <para><b>解决什么问题</b>：业务面 <c>/api/*</c> 目前完全匿名，而服务端监听 <c>http://*:5066</c>
/// ⇒ 同网段任何主机都能直接调读写业务数据。但不能"直接给 /api/* 加 RequireAuthorization"——
/// 内部调用方（Blazor 电路的 <c>MainApiClient</c>、53 个 <c>AddHttpClient</c> 注册点、
/// 工位机跨进程、Elsa/MCP）**全部零凭证**，一加就是全站 401。</para>
///
/// <para><b>本类的职责只有一半</b>：<b>出站</b>自动给「打向本站」的请求附加 <c>X-Internal-Key</c>。
/// 入站校验在 <c>CJ.Plug.HostWebServer/Shell/InternalApiKeyGate.cs</c>，且默认 <b>Observe 模式</b>
/// （只统计不拦截）——先量出「到底有多少内部调用带得上密钥」，再决定收口。</para>
///
/// <para><b>为什么先地基后收口</b>：方案文档原排「C1 写操作收口 → C2 内部密钥」，但 C1 一旦生效，
/// 任何漏串 handler 的内部调用都会立刻 401。顺序必须是「先让所有出站都带密钥（不强制）→ 验证覆盖率
/// 100% → 再逐个端点 RequireAuthorization」，否则爆炸半径不可控。</para>
///
/// <para><b>为什么按端口判定本站</b>：单端口收敛后全站只有 <see cref="GlobalData.ServerPort"/>
/// 一个端口，外部服务（DeepSeek/OpenRouter/工位机自身端口）不会用 5066。
/// 只按 host 判定会漏掉「工位机用主机 IP 调主服务」的形态；只按 GlobalData 字符串比会漏掉
/// 换 host 的部署。⚠ 绝不能无条件加头——那会把内部密钥发给外部 LLM 服务商。</para>
/// </summary>
public static class InternalApiKey
{
    /// <summary>密钥头名（入站中间件与出站 handler 共用，禁止各写一份字面量）。</summary>
    public const string HeaderName = "X-Internal-Key";

    /// <summary>
    /// 当前进程使用的密钥。<c>null</c> = 未启用（出站不加头、入站不统计）。
    /// <para>由宿主启动时 <see cref="Configure"/> 注入；信创离线环境下密钥只能来自显式配置，
    /// <b>不做"首次启动自动生成并回写 appsettings"</b> —— 那属于未经用户同意的配置改写。</para>
    /// </summary>
    public static string? Value { get; private set; }

    /// <summary>是否已配置密钥（出站是否附加、入站是否统计的开关）。</summary>
    public static bool IsEnabled => !string.IsNullOrWhiteSpace(Value);

    private static int _targetPort = -1;

    /// <summary>配置密钥。空值 = 关闭本机制（保留既有全匿名行为）。</summary>
    public static void Configure(string? key)
    {
        Value = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    /// <summary>
    /// 目标是否「本站」（应附加内部密钥）。
    /// <para>判据：端口 == <see cref="GlobalData.ServerPort"/>。相对 URI（BaseAddress 拼接后仍是相对）不算。</para>
    /// </summary>
    public static bool IsTarget(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri) return false;

        if (_targetPort < 0)
        {
            // GlobalData.ServerPort 是 const 字符串 → int.Parse 一次即可；失败退化为 5066。
            _targetPort = int.TryParse(GlobalData.ServerPort, out var p) ? p : 5066;
        }

        return uri.Port == _targetPort;
    }

    /// <summary>
    /// 定长比较，避免按字符提前返回泄漏前缀长度（时序侧信道）。
    /// 空候选 / 未配置一律 false。
    /// </summary>
    public static bool Matches(string? candidate)
    {
        if (!IsEnabled || string.IsNullOrEmpty(candidate)) return false;

        var expected = Encoding.UTF8.GetBytes(Value!);
        var actual = Encoding.UTF8.GetBytes(candidate);

        // FixedTimeEquals 要求等长；长度本身不是秘密（密钥长度固定），先按长度快速排除。
        if (expected.Length != actual.Length) return false;
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <summary>
    /// 给**裸 <c>new HttpClient()</c>** 用的工厂。
    /// <para>⚠ 必须自带 <see cref="HttpClientHandler"/> 作为最内层：<see cref="DelegatingHandler"/>
    /// 只有被 <c>IHttpMessageHandlerBuilder</c> 串起来时才由框架补 InnerHandler，手工 new 不补就抛
    /// <c>InvalidOperationException: The inner handler has not been assigned</c>。</para>
    /// <para>覆盖范围：<c>AddHttpClient</c> 创建的 53 个注册点由
    /// <c>InternalApiKeyHttpFilter</c>（IHttpMessageHandlerBuilderFilter）统一覆盖，**不需要**用本方法；
    /// 本方法只服务 <c>BaseApiClient.httpClient</c> 这类手 new 的字段。</para>
    /// </summary>
    public static HttpClient CreateClient(HttpMessageHandler? inner = null)
        => new(new InternalApiKeyHandler { InnerHandler = inner ?? new HttpClientHandler() });

    /// <summary>
    /// 出站附加 <c>X-Internal-Key</c>。两种挂法都支持：
    /// <list type="bullet">
    /// <item>factory（<c>AddHttpClient</c>）→ 经 <c>IHttpMessageHandlerBuilderFilter</c> 加入
    ///   <c>AdditionalHandlers</c>，此时<b>不要</b>设 InnerHandler（框架会串）；</item>
    /// <item>手工 <c>new HttpClient(handler)</c> → 用 <see cref="CreateClient"/>，由它补 InnerHandler。</item>
    /// </list>
    /// </summary>
    public sealed class InternalApiKeyHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 未配置密钥 / 目标不是本站 ⇒ 原样放行（外部 LLM、图站部署包下载等绝不能带这个头）
            if (IsEnabled && IsTarget(request.RequestUri))
            {
                // ⚠ TryAddWithoutValidation：值本身是 base64，不需要按 RFC 7230 的 token 规则校验；
                //   用 Add 遇非法字符会抛，反而把正常请求搞挂。
                request.Headers.TryAddWithoutValidation(HeaderName, Value);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
