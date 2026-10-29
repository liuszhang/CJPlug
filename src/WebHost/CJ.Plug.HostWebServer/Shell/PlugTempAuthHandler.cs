using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CJ.Plug.HostWebServer.Shell;

/// <summary>
/// P2–P4 期间的<b>临时认证占位handler</b>（《（设计中）CJPlug接入CJCore实施计划-20261029》P5 前过渡件）。
///
/// <para><b>为什么必须存在</b>：CJCore 的 Minimum API 端点（P6 <c>/api/module-admin/*</c>、
/// P7 <c>/api/system-config/*</c>）都以 <c>RequireAuthorization(...)</c> 挂载了授权元数据；
/// 而 CJPlug 在 P5 之前<b>没有任何服务端认证方案</b>（原 <c>UseAuthentication/UseAuthorization</c>
/// 在 Program.cs 里被注释掉了）。结果是：① 端点带授权元数据却缺 <c>AuthorizationMiddleware</c>
/// ⇒ 请求进来直接抛 <c>InvalidOperationException</c>；② 就算补了中间件，没有 scheme ⇒ 一律 401，
/// 表现为「/admin/modules 页面打不开、系统配置保存 401」。</para>
///
/// <para><b>安全性没有因此变差</b>：CJPlug 现状是<b>完全无鉴权</b>（所有 HTTP 端点与页面裸奔），
/// 本 handler 只是把这一现状<b>形式化</b>成一个恒为 <c>Admin</c> 的匿名主体，
/// 让中间件链路完整 ⇒ 不新建安全口子，也没有把"已鉴权"降级为"未鉴权"。
/// 真正的准入仍由 <see cref="CJ.Plug.MainPageContent.Shell.PlugGuardModule"/> 的 localStorage 守卫承担。</para>
///
/// <para>⚠ <b>P5 必须删除</b>：接入 <c>AddCJCoreThreeRoleAdmin()</c> 后，本文件与 Program.cs 里的
/// 注册必须整段删除（届时也会同时删掉 <see cref="PlugTempAdminAuthStateProvider"/> —— 端点层与
/// 组件层两件<b>必须成对删除</b>，只删其中一个会让两者的权限口径再次错位）。</para>
/// </summary>
public sealed class PlugTempAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>方案名；与 Program.cs 的注册保持同一常量。</summary>
    public const string Scheme = "PlugTempAnonymousAdmin";

    /// <summary>过渡期赋予的角色；对齐 CJPlug 现行最高权限角色名。</summary>
    public const string TempRole = "Admin";

    public PlugTempAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 无人认领 = 恒成功，产出一个带 Admin 角色的主体。
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "plug-temp"),
            new Claim(ClaimTypes.NameIdentifier, "plug-temp"),
            new Claim(ClaimTypes.Role, TempRole),
        }, Scheme);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
