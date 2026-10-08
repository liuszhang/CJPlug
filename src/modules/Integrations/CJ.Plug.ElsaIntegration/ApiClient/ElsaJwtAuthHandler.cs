using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.ElsaIntegration;

/// <summary>
/// 给 <c>ElsaApiClient</c> 的 <see cref="HttpClient"/> 挂上「管理员 JWT」通道：
/// 请求前惰性登录（进程内缓存、按令牌 exp 续期）、401 时强制重登并重发一次。
///
/// <para>
/// <b>为什么需要它</b>：Elsa 3.9 起 <c>AdminApiKeyProvider</c> 的语义是
/// 「未显式配置 = 全拒」（<c>UseAdminApiKey()</c> 无参重载的 XML 文档原话：
/// <i>"The provider denies all keys unless configured."</i>）。而 <c>ElsaApiClient</c> 原先只带
/// <c>Authorization: ApiKey 00000000-0000-0000-0000-000000000000</c>（<c>GlobalData.ElsaEngineApiKey</c> 的硬编码占位）
/// ⇒ 引擎侧每个管理端点都回 401，且保存/执行两处**不检查状态码**，于是静默失败：
/// 响应头里没有 <c>x-elsa-workflow-instance-id</c> → 实例 ID 为 null → 后续
/// <c>GET /elsa/api/workflow-instances//journal</c> 拼出空段 URL → 404 → <c>HttpRequestException</c> 冒到
/// 页面的 ErrorBoundary（实机症状：流程编辑器整页「出错了！…404 (Not Found)」）。
/// </para>
///
/// <para>
/// <b>为什么用 JWT 而不是补 ApiKey</b>：JWT 通道**已经跑通**——Studio 的
/// <c>CjElsaAutoLoginJwtAccessor</c> 走的就是「<c>ElsaAdminCredentialResolver</c> 派生的管理员凭据 →
/// <c>POST {backend}/identity/login</c>」，是当前唯一被实测证明可用的凭据通道；ApiKey 通道需要额外
/// 在引擎侧显式引导一把密钥并让 DispatchServer 同源返回（另一条路，本改动不选）。
/// </para>
///
/// <para>
/// <b>凭据同源</b>：用户名/口令取自 <see cref="ElsaAdminCredentialResolver"/>，与引擎侧
/// <c>identity.UseAdminUserProvider(...)</c> 用的是同一个解析器 ⇒ 构造性一致；口令的第三档来源是
/// 「每机签名密钥的纯函数」，而签名密钥落在 8 个服务共享的输出目录（<c>elsa-signing.key</c>），
/// 因此 ApiServer / ElsaApiServer 这些**独立进程**也能算出同一个口令（本文件不做任何落盘）。
/// </para>
///
/// <para>
/// <b>无递归</b>：登录用的是本类自带的「裸」静态 <see cref="HttpClient"/>（无本 handler），
/// 不会回调 <see cref="SendAsync"/>。
/// </para>
/// </summary>
internal sealed class ElsaJwtAuthHandler : DelegatingHandler
{
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(20);

    /// <summary>提前量：剩余寿命不足此值时视为将过期，先续期再发请求。</summary>
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    /// <summary>令牌里读不到 <c>exp</c> 时的保守缓存时长（不依赖具体版本的令牌寿命）。</summary>
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromMinutes(30);

    /// <summary>登录失败后的退避窗口：引擎不可达时不要每个请求都打两次登录。</summary>
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromSeconds(5);

    private static readonly HttpClient LoginClient = new() { Timeout = LoginTimeout };
    private static readonly SemaphoreSlim LoginGate = new(1, 1);
    private static readonly ConcurrentDictionary<string, CachedToken> Tokens = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastFailures = new(StringComparer.Ordinal);

    private readonly string _loginUrl;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _cacheKey;

    /// <param name="elsaEngineBaseUrl">Elsa 引擎根地址，如 <c>http://localhost:5001</c>（可带 <c>/elsa/api</c> 后缀）。</param>
    /// <param name="userName">管理员用户名（与引擎侧 <c>UseAdminUserProvider</c> 同源）。</param>
    /// <param name="password">管理员口令（与引擎侧同源）。</param>
    public ElsaJwtAuthHandler(string elsaEngineBaseUrl, string userName, string password)
    {
        _loginUrl = BuildLoginUrl(elsaEngineBaseUrl);
        _userName = userName;
        _password = password;
        _cacheKey = $"{_loginUrl}|{userName}";

        InnerHandler = new HttpClientHandler();
    }

    /// <summary>
    /// 由引擎根地址拼出登录端点。兼容两种写法：<c>http://host:5001</c> 与 <c>http://host:5001/elsa/api</c>
    /// （Studio 侧配置键 <c>Backend:Url</c> 是后者、DispatchServer 返回的 <c>GetElsaEngineServer</c> 是前者）。
    /// </summary>
    internal static string BuildLoginUrl(string? elsaEngineBaseUrl)
    {
        var baseUrl = (elsaEngineBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        return baseUrl.EndsWith("/elsa/api", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/identity/login"
            : $"{baseUrl}/elsa/api/identity/login";
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ApplyBearer(request, await GetTokenAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false));

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 令牌被拒（过期 / 引擎重启换了签名密钥 / 引擎刚完成引导）：强制重登并重发一次。
        if (response.StatusCode != HttpStatusCode.Unauthorized || !CanReplay(request))
            return response;

        Log.Information("[Elsa] ElsaApiClient 请求收到 401，强制重新登录后重试：{Method} {Uri}", request.Method, request.RequestUri);
        response.Dispose();

        var retry = Clone(request);
        ApplyBearer(retry, await GetTokenAsync(forceRefresh: true, cancellationToken).ConfigureAwait(false));
        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && TryGetFreshToken(out var token))
            return token;

        if (!forceRefresh && IsInBackoff())
            return null;

        await LoginGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 双检：并发首个请求里只有一个真正去登录。
            if (!forceRefresh && TryGetFreshToken(out token))
                return token;

            if (!forceRefresh && IsInBackoff())
                return null;

            var fresh = await LoginAsync(cancellationToken).ConfigureAwait(false);
            if (fresh is null)
            {
                // 失败不长期缓存（引擎后起也能自愈），只做秒级退避。
                LastFailures[_cacheKey] = DateTimeOffset.UtcNow;
                return null;
            }

            Tokens[_cacheKey] = fresh;
            LastFailures.TryRemove(_cacheKey, out _);
            return fresh.Token;
        }
        finally
        {
            LoginGate.Release();
        }
    }

    private bool TryGetFreshToken(out string? token)
    {
        if (Tokens.TryGetValue(_cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow + RefreshMargin)
        {
            token = cached.Token;
            return true;
        }

        token = null;
        return false;
    }

    private bool IsInBackoff()
        => LastFailures.TryGetValue(_cacheKey, out var last) && DateTimeOffset.UtcNow - last < FailureBackoff;

    private async Task<CachedToken?> LoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await LoginClient
                .PostAsJsonAsync(_loginUrl, new LoginRequest(_userName, _password), cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("[Elsa] ElsaApiClient 自动登录失败：{Url} 返回 {StatusCode}（未缓存，稍后自动重试）",
                    _loginUrl, (int)response.StatusCode);
                return null;
            }

            var result = await response.Content
                .ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result is null || !result.IsAuthenticated || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                Log.Warning("[Elsa] ElsaApiClient 自动登录失败：{Url} 返回 isAuthenticated=false（用户 {User}）。" +
                            "请核对引擎侧管理员引导凭据是否与 ElsaAdminCredentialResolver 同源" +
                            "（环境变量 / 配置键 / 由 elsa-signing.key 派生）",
                    _loginUrl, _userName);
                return null;
            }

            var expiresAt = TryReadExpiry(result.AccessToken!) ?? DateTimeOffset.UtcNow + FallbackLifetime;
            Log.Information("[Elsa] ElsaApiClient 已取得管理员 JWT（用户 {User}，{Url}，到期 {ExpiresAt:u}）",
                _userName, _loginUrl, expiresAt);

            return new CachedToken(result.AccessToken!, expiresAt);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Elsa] ElsaApiClient 自动登录异常（未缓存，稍后自动重试）：{Message}", ex.Message);
            return null;
        }
    }

    private static void ApplyBearer(HttpRequestMessage request, string? token)
        => request.Headers.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);

    /// <summary>重发前提：请求体可重复读取（内存态内容），流式/多部件内容不重试以免半截发送。</summary>
    private static bool CanReplay(HttpRequestMessage request)
        => request.Content is null
           or JsonContent
           or StringContent
           or ByteArrayContent
           or FormUrlEncodedContent;

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
            // 内存态内容（见 CanReplay）可安全复用同一实例：每次发送都会重新序列化/读取。
            Content = request.Content
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    /// <summary>从 JWT 载荷读 <c>exp</c>（读不到就返回 null，由调用方用保守寿命兜底）。</summary>
    private static DateTimeOffset? TryReadExpiry(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch
        {
            // 令牌形状异常不影响主流程：退回保守寿命。
        }

        return null;
    }

    private sealed record CachedToken(string Token, DateTimeOffset ExpiresAt);
}
