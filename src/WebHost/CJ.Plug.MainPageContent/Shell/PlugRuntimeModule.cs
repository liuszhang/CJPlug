using CJCore.Framework.Abstractions;

namespace CJ.Plug.MainPageContent.Shell;

/// <summary>
/// 宿主运行时模块（2026-10-29《CJPlug 接入 CJCore》P2）：
/// 把原 <c>MainLayout.razor</c> / <c>EmbeddedLayout.razor</c> 里「不属于框架职责、但删掉就会坏」
/// 的运行时件搬到框架可挂载的位置。
/// <list type="bullet">
///   <item><see cref="PlugRuntimeHost"/>：Radzen 主题 + 组件宿主（缺失 ⇒ Radzen 控件静默失效）。</item>
///   <item><see cref="PlugExecLogDrawer"/>：执行日志抽屉（必须挂在 MudLayout 直接子级）。</item>
///   <item><see cref="PlugExecLogButton"/>：AppBar 上的抽屉触发按钮（带 AccessKey，支持 P6 功能门控）。</item>
/// </list>
/// </summary>
public sealed class PlugRuntimeModule(IAppBarService appBar) : ModuleBase
{
    public override string Name => "插座运行时宿主";

    public override string ModuleId => "plug-runtime";

    public override string Group => "宿主扩展";

    public override bool IsMandatory => true;

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBar.AddAppBarDrawer<PlugRuntimeHost>();
        appBar.AddAppBarDrawer<PlugExecLogDrawer>();

        // AccessKey 与模块 ModuleId 对齐（P6 模块启停 / 功能键口径）
        appBar.AddAppBarItem<PlugExecLogButton>("plug-runtime");

        return ValueTask.CompletedTask;
    }
}
