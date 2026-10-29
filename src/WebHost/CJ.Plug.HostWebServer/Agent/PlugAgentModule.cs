using CJCore.Framework.Abstractions;

namespace CJ.Plug.HostWebServer.Agent;

/// <summary>
/// 工作流 Agent 模块（P8.3.5 的<b>必要条件</b>，不只是"登记一个模块"）。
///
/// <para><b>为什么必须有它</b>：框架 <c>MapCJCoreRazorComponents()</c> 的路由发现 =
/// 「收集已注册 <see cref="IModule"/> 的<b>所属程序集</b>」。调试页写在<b>宿主程序集</b>里，
/// 而宿主此前没有任何 <see cref="IModule"/> 实现 ⇒ 该程序集不会被加进 AdditionalAssemblies
/// ⇒ <c>/debug/plug-agent</c> 一律 404。框架注释也明写了这一点：
/// 「宿主程序集自身含 @page 页面时，必须注册一个壳 IModule」。</para>
///
/// <para><b>为什么 Group 是"调试"</b>：与菜单项保持一致（<see cref="PlugAgentMenuService"/>）。</para>
///
/// <para><b>为什么 IsMandatory = false</b>：禁用它的后果是"调试页 404"，属于<b>功能下线</b>而不是炸站
/// （对照 <c>PlugShellModule</c>：壳被禁 = 宿主页面全 404 = 炸站，故为 true）。
/// 本模块允许在模块管理页里关掉，正好用来验证 P6 的启停能力。</para>
/// </summary>
public sealed class PlugAgentModule : ModuleBase
{
    public override string Name => "工作流 Agent（调试）";

    public override string ModuleId => "plug-agent";

    public override string Group => "调试";

    public override bool IsMandatory => false;
}
