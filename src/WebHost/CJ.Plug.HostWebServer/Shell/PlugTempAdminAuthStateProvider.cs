using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CJ.Plug.HostWebServer.Shell;

/// <summary>
/// 过渡期<b>恒等「本地管理员」</b>的认证态提供器 —— ⚠ <b>P5 上线当天必须连同
/// <c>PlugTempAuthHandler</c> 一起删除</b>（届时由 ThreeRoleAdmin 提供真认证）。
///
/// <para><b>为什么必须有一个</b>：框架布局 <c>FrameworkLayout</c> 与路由视图
/// <c>ModuleAccessRouteView</c> 都<b>直接注入</b> <see cref="AuthenticationStateProvider"/>
/// （不是 <c>GetService</c>），而 CJPlug 在 P5 之前没有任何服务端认证体系。
/// 不注册 ⇒ 打开任意页面即 DI 解析失败（整个壳起不来）。</para>
///
/// <para><b>为什么是「已认证的 Admin」而不是匿名</b>（2026-10-29 冒烟实证修正）：
/// <see cref="CJCore.Framework.UI.ModuleAccessRouteView"/> 有一条专门的兜底 ——
/// 「宿主未注册任何认证 scheme」时，仅带 <c>[ModuleAccess]</c> 的页面对匿名用户放行。
/// 但 CCSWiki のために…<P5 前为了让受保护的 API 端点（<c>/admin/modules</c>、<c>/system-config</c>
/// 的写接口，框架内部均挂了 <c>RequireAuthorization</c>）能够工作，本轮已经注册了
/// <c>PlugTempAuthHandler</c> scheme ⇒ 「无认证体系」判定翻转为 false ⇒ 那条兜底分支失效，
/// 结果是 <c>/admin/modules</c> 与 <c>/system-config</c> 一律 302 到 <c>/login</c>（页面打不开）。
/// 因此两层必须口径一致：<b>端点层恒 Admin（handler）+ 组件层恒 Admin（本类）</b>。</para>
///
/// <para><b>安全性说明</b>：CJPlug 迁移前本来就完全没有鉴权，本类并未降低安全性，
/// 只是把「事实上的全开放」在框架的两套判定里显式化（否则会出现「菜单看得见、页面点不开」的错乱态）。
/// 真正的收口在 P5。</para>
/// </summary>
public sealed class PlugTempAdminAuthStateProvider : AuthenticationStateProvider
{
/// 与 <see cref="PlugTempAuthHandler"/> 保持完全一致的主体（同一 scheme、同一用户名、同一角色集合），
/// 否则端点层与组件层会出现「同一用户两种权限」的错乱态。
private static readonly ClaimsPrincipal TempAdmin = BuildPrincipal();

private static ClaimsPrincipal BuildPrincipal()
{
    var identity = new ClaimsIdentity(
        authenticationType: PlugTempAuthHandler.Scheme,
        nameType: ClaimTypes.Name,
        roleType: ClaimTypes.Role);

    identity.AddClaim(new Claim(ClaimTypes.Name, "plug-temp"));
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "plug-temp"));
    identity.AddClaim(new Claim(ClaimTypes.Role, PlugTempAuthHandler.TempRole));

    return new ClaimsPrincipal(identity);
}

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(TempAdmin));
}
