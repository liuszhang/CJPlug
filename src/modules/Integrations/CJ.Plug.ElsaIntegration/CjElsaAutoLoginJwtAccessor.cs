using System.Collections.Concurrent;
using System.Net.Http.Json;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login;
using Elsa.Studio.Login.Contracts;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CJ.Plug.ElsaIntegration;

/// <summary>
/// Elsa Studio 的 <see cref="IJwtAccessor"/> 替换实现：**内存缓存 + 惰性自动登录**。
///
/// <para>
/// <b>为什么需要它</b>：Studio 的 HTTP（<c>AuthenticatingApiHttpMessageHandler</c>）、SignalR 实时
/// （<c>LoginAuthHttpConnectionOptionsConfigurator</c>）与登录态（<c>AccessTokenAuthenticationStateProvider</c>）
/// 三处统一从 <see cref="IJwtAccessor"/> 取令牌。CJPlug 的流程编辑器是**内嵌在自己的页面里**的，
/// 从不显示 Studio 的登录页，因此默认实现（<c>BlazorServerJwtAccessor</c>，读写浏览器 localStorage）
/// 里永远没有令牌 ⇒ 对 Elsa 引擎的每个请求都是匿名 ⇒ 401（实机症状：流程编辑器「组件库」空白）。
/// </para>
///
/// <para>
/// <b>为什么不用 <c>ICredentialsValidator</c></b>：它经 <c>IBackendApiClientProvider</c> 取 API 客户端，
/// 而那条 HttpClient 挂着 <c>AuthenticatingApiHttpMessageHandler</c> ⇒ 会回调本类的
/// <see cref="ReadTokenAsync"/> ⇒ **无限递归**。这里刻意改用「裸」<see cref="IHttpClientFactory"/> 默认客户端
/// 直连 identity 端点（与 Elsa 自己的 <c>ElsaIdentityRefreshTokenService</c> 同款写法）切断该环。
/// </para>
///
/// <para>
/// <b>无竞态</b>：令牌在首次读取时惰性取得，早于任何业务请求；不依赖 JS interop、不依赖预渲染时机、
/// 也不依赖"哪个子组件先渲染"。<b>失败不缓存</b>：ElsaApiServer 尚未就绪时下一次请求会自动重试。
/// </para>
/// </summary>
internal sealed class CjElsaAutoLoginJwtAccessor : IJwtAccessor
{
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(20);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;
    private readonly IConfiguration? _configuration;
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _loginGate = new(1, 1);

    public CjElsaAutoLoginJwtAccessor(
        IHttpClientFactory httpClientFactory,
        IRemoteBackendAccessor remoteBackendAccessor,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _remoteBackendAccessor = remoteBackendAccessor;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async ValueTask<string?> ReadTokenAsync(string name)
    {
        if (TryGetToken(name, out var cached))
            return cached;

        await EnsureLoggedInAsync().ConfigureAwait(false);

        return TryGetToken(name, out var token) ? token : null;
    }

    /// <inheritdoc />
    public ValueTask WriteTokenAsync(string name, string token)
    {
        // 刷新令牌流程（ElsaIdentityRefreshTokenService）会把新令牌写回这里。
        _tokens[name] = token;
        return ValueTask.CompletedTask;
    }

    private bool TryGetToken(string name, out string token)
    {
        if (_tokens.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            token = value;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task EnsureLoggedInAsync()
    {
        if (TryGetToken(TokenNames.AccessToken, out _))
            return;

        await _loginGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // 双检：并发首个请求里只有一个真正去登录。
            if (TryGetToken(TokenNames.AccessToken, out _))
                return;

            await LoginAsync().ConfigureAwait(false);
        }
        finally
        {
            _loginGate.Release();
        }
    }

    private async Task LoginAsync()
    {
        var backendUrl = _remoteBackendAccessor.RemoteBackend.Url.ToString().TrimEnd('/');
        var loginUrl = $"{backendUrl}/identity/login";
        var userName = ElsaAdminCredentialResolver.ResolveUserName(_configuration);

        try
        {
            // ⚠ 必须是「裸」客户端（默认命名客户端，未挂 Elsa 的认证 handler），否则会递归，见类注释。
            var client = _httpClientFactory.CreateClient();
            var password = ElsaAdminCredentialResolver.ResolvePassword(_configuration);

            using var cts = new CancellationTokenSource(LoginTimeout);
            using var response = await client
                .PostAsJsonAsync(loginUrl, new LoginRequest(userName, password), cts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // 失败不缓存 ⇒ 下一次 ReadTokenAsync 会重试（ElsaApiServer 尚未就绪时可自愈）。
                Log.Warning("[Elsa] 自动登录失败：{Url} 返回 {StatusCode}（未缓存，下次请求自动重试）",
                    loginUrl, (int)response.StatusCode);
                return;
            }

            var result = await response.Content
                .ReadFromJsonAsync<LoginResponse>(cancellationToken: cts.Token)
                .ConfigureAwait(false);

            if (result is null || !result.IsAuthenticated || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                Log.Warning("[Elsa] 自动登录失败：{Url} 返回 isAuthenticated=false（用户 {User}）。" +
                            "请核对 Elsa 引擎侧的管理员引导凭据是否与 ElsaAdminCredentialResolver 同源" +
                            "（环境变量 / 配置键 / 由签名密钥派生）", loginUrl, userName);
                return;
            }

            _tokens[TokenNames.AccessToken] = result.AccessToken!;
            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                _tokens[TokenNames.RefreshToken] = result.RefreshToken!;

            Log.Information("[Elsa] 已自动登录 Elsa 引擎：{Url}（用户 {User}）", loginUrl, userName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Elsa] 自动登录异常（未缓存，下次请求自动重试）：{Message}", ex.Message);
        }
    }
}
