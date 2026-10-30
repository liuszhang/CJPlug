using CJCore.Framework.Abstractions;

namespace CJ.Plug.MainPageContent.Shell;

/// <summary>
/// 宿主壳模块（2026-10-29《CJPlug 接入 CJCore》P2）。
/// <para>
/// 唯一职责：让 <c>CJ.Plug.MainPageContent</c> 程序集进入框架路由发现。
/// 该程序集内含 3 个 <c>@page</c>（Error / JobManageList2 / JobMonitor2），
/// 切换到 <c>FrameworkApp</c> 后 AppAssembly 变成 <c>CJCore.Framework.UI</c>，不注册就会 404。
/// </para>
/// </summary>
public sealed class PlugShellModule : ModuleBase
{
    public override string Name => "插座宿主壳";

    public override string ModuleId => "plug-shell";

    public override string Group => "宿主扩展";

    /// <summary>壳模块被禁用 ⇒ 宿主自带页面全部 404，等同于炸站：不给关。</summary>
    public override bool IsMandatory => true;
}
