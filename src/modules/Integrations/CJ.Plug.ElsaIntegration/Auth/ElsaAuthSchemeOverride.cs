using CJCore.Framework.Abstractions;

namespace CJ.Plug.ElsaIntegration.Auth;

/// <summary>
/// 把 <c>/elsa/api/*</c> 的认证交回 **Elsa 自己的身份体系**（D7=A，2026-10-30）。
/// </summary>
/// <remarks>
/// <para>
/// <b>症状</b>：<c>ElsaApiClient</c> 用 Elsa 管理员 JWT 调 <c>POST /elsa/api/workflow-definitions</c>，
/// 拿到的却是 <b>302 → 登录页 → 200 的 HTML</b>：保存请求被跟随重定向"成功"返回，引擎库里却查不到定义
/// （<c>main-elsa.db</c> 无该 DefinitionId），随后取实例 ID 失败 ⇒ 调用 500。日志里的
/// <c>'&lt;' is an invalid start of a value</c> 就是登录页 HTML 撞上 JSON 反序列化。
/// </para>
/// <para>
/// <b>根因</b>：CJCore 统一认证基座（<c>AddCJCoreAuth</c>）把全局 <c>DefaultScheme</c> 改写为自己的
/// PolicyScheme，其 <c>ForwardDefaultSelector</c> 只按 <c>Authorization: Bearer </c> 前缀分流 ⇒ Elsa 的 JWT
/// 被送进 CJCore 的 JwtBearer（密钥来自 <c>ITokenKeyProvider</c>，与 Elsa.Identity 的签名密钥不同）⇒
/// 验签失败 ⇒ Cookie challenge ⇒ 302 登录页。两套身份体系之间没有任何接缝。
/// </para>
/// <para>
/// <b>修法</b>：本选择器让 <c>/elsa/api</c> 前缀改走 Elsa 注册的方案，Elsa 自己再按同一个 Authorization 头
/// 选 <c>Bearer</c>（JWT）或 <c>ApiKey</c>。这样 <c>ElsaApiClient</c> 的管理员 JWT、Studio 的登录态、
/// 以及未来若启用的 ApiKey 通道全部由 Elsa.Identity 单点裁决，CJCore 不再参与。
/// </para>
/// <para>
/// ⚠ <b>scheme 名的来源</b>：<c>Elsa.Identity.Features.DefaultAuthenticationFeature.Apply()</c> 里
/// <c>Services.AddAuthentication(MultiScheme)</c>，<c>private const string MultiScheme = "Jwt-or-ApiKey"</c>。
/// 该常量未公开，升级 Elsa 大版本时必须复核（未命中会抛 "Scheme not registered"，属显性失败、不会静默）。
/// </para>
/// </remarks>
public sealed class ElsaAuthSchemeOverride : IAuthSchemeOverrideSelector
{
    /// <summary>Elsa API 的路径前缀（Elsa 侧 <c>UseWorkflowsApi</c> 默认挂在 <c>/elsa/api</c>）。</summary>
    public const string ApiPathPrefix = "/elsa/api";

    /// <summary>
    /// Elsa.Identity 的复合认证方案名（内部再选 <c>Bearer</c> / <c>ApiKey</c>）。
    /// 与 <c>DefaultAuthenticationFeature.MultiScheme</c> 严格同字面量。
    /// </summary>
    public const string ElsaScheme = "Jwt-or-ApiKey";

    /// <inheritdoc />
    public string? SelectScheme(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 按「段边界」比对：/elsa/api 与 /elsa/api/** 命中，/elsa/apixxx 不命中。
        if (path.StartsWith(ApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            (path.Length == ApiPathPrefix.Length || path[ApiPathPrefix.Length] == '/'))
        {
            return ElsaScheme;
        }

        return null;
    }
}
