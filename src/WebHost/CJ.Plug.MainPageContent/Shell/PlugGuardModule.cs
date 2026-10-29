using CJCore.Framework.Abstractions;

namespace CJ.Plug.MainPageContent.Shell;

/// <summary>
/// ⚠ 临时守卫（2026-10-29《CJPlug 接入 CJCore》P2-2.11）—— <b>P5 删除</b>。
/// <para>
/// 现状：CJPlug 的登录态只落在浏览器 <c>localStorage.userid</c>（经 Blazored.LocalStorage 读写），
/// 服务端没有认证体系。旧壳在 <c>MainLayout.OnAfterRenderAsync(firstRender)</c> 里用 JS 互操作
/// 读该键，为空就跳 <c>/login?returnUrl=…</c>。换用框架布局后该钩子随布局一起消失，
/// 守卫用一段 body 尾部脚本复刻<b>完全相同的语义</b>，保证 P2 期间不出现"换壳即裸奔"。
/// </para>
/// <para>
/// P5 接入 ThreeRoleAdmin 真认证后：删除本模块，改由 <c>FrameworkOptions.LoginPath</c>
/// + <c>ModuleAccessRouteView</c> 做服务端拦截（真撤销，而非"清 Cookie 就算"）。
/// </para>
/// </summary>
public sealed class PlugGuardModule(IAppHeadService head) : ModuleBase
{
    public override string Name => "登录守卫（临时）";

    public override string ModuleId => "plug-guard";

    public override string Group => "宿主扩展";

    public override bool IsMandatory => true;

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 语义与旧 MainLayout 一致：userid 为空 且 当前不在 /login → 强制跳登录页。
        // returnUrl 取「路径 + 查询」，根路径归一为 "/"，与旧实现的 EscapeDataString 口径相同。
        head.AddBodyHtml("""
<script>
(function () {
    try {
        var raw = window.localStorage.getItem('userid');
        var empty = raw === null || raw === undefined || raw === '' || raw === 'null' || raw === 'undefined';
        if (!empty) return;
        var p = location.pathname || '/';
        if (p === '/login' || p.indexOf('/login/') === 0) return;
        var back = (p === '/' ? '/' : p + (location.search || ''));
        location.replace('/login?returnUrl=' + encodeURIComponent(back));
    } catch (e) { /* localStorage 不可用时放行，交由服务端兜底 */ }
})();
</script>
""");

        return ValueTask.CompletedTask;
    }
}
