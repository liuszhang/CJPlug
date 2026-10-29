using CJCore.Modules.SystemConfig.Abstractions;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// 把 CJPlug 唯一的业务开关「远程查看模式」贡献成 CJCore 通用配置页（<c>/system-config</c>）的一个页签。
///
/// <para>《（设计中）CJPlug接入CJCore实施计划-20261029》P7.1 / D8=B / Q10：
/// 系统与许可证合成一个页面两页签；这里只声明 <see cref="PlugSystemConfigKeys.RemoteViewGroup"/> 一个分组，
/// 框架CoreProvider 的外观/长任务等内置分组自动并列显示，不需要 CJPlug 重写一遍。</para>
///
/// <para><b>为什么用 <c>Select</c>（普通下拉）而不是 <c>Text</c></b>：这两档是<b>枚举</b>语义，
/// 旧页面也是下拉（<c>MudSelect</c>）。而且候选只有两项 ⇒ 按框架 2026-10-26 口径
/// 显式写 <c>Searchable: false</c>（候选少时不要“输入关键字”的假搜索框）。</para>
///
/// <para><b>为什么不用框架的 <c>AppearanceGroup</c> 等内置分组</b>：远程查看是 CJPlug 的私有旋钮，
/// 独立成页签比混进别人的页签更好找。</para>
/// </summary>
public sealed class PlugSystemConfigFieldProvider : ISystemConfigFieldProvider
{
    public IEnumerable<SystemConfigField> GetFields() =>
    [
        new SystemConfigField(
            Key: PlugSystemConfigKeys.RemoteViewMode,
            Label: "远程查看模式",
            Editor: SystemConfigEditorKind.Select,
            DefaultValue: PlugSystemConfigKeys.DefaultRemoteViewMode,
            Placeholder: "选择远程桌面的投射方式",
            HelpText: "控制工具执行时远程桌面的查看方式：\n\n**整桌面 VNC**（默认）—— 投射全部屏幕；\n**单窗口 RFB VNC** —— 仅投射目标程序窗体。\n\n⚠ 改动后对**下一次**启动的工具生效，正在执行的会话不变。",
            Order: 10,
            Group: PlugSystemConfigKeys.RemoteViewGroup,
            Options:
            [
                new SystemConfigOption("fullscreen", "整桌面 VNC（全部屏幕）"),
                new SystemConfigOption("window", "单窗口 RFB VNC（仅投射程序窗体）"),
            ],
            Searchable: false),
    ];
}
