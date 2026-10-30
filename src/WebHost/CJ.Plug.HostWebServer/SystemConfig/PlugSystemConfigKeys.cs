using CJCore.Modules.SystemConfig.Abstractions;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// CJPlug 贡献给通用配置页（<c>/system-config</c>）的配置键。
///
/// <para><b>键名必须沿用旧名 <c>RemoteViewMode</c></b>：现场存量值（旧
/// <c>SystemConfigItems</c> 表）用的就是这个名字，读侧 <c>IRemoteViewModeProvider</c>、
/// 写侧本配置页与工位机透传字段都按它取值；换名字会让存量值读不出来（静默回落默认全屏）。
/// P7.1a（2026-10-30）退役私有配置模块后，键名是唯一被刻意保留的“旧契约”。</para>
/// </summary>
public static class PlugSystemConfigKeys
{
    /// <summary>远程查看模式（fullscreen=整桌面 VNC；window=单窗口 RFB VNC 投射）。</summary>
    public const string RemoteViewMode = "RemoteViewMode";

    /// <summary>默认值：整桌面 VNC（与 CJPlug 迁移前完全一致）。</summary>
    public const string DefaultRemoteViewMode = "fullscreen";

    /// <summary>页签标题（<see cref="CJCore.Modules.SystemConfig.Abstractions.SystemConfigField.Group"/>）。</summary>
    public const string RemoteViewGroup = "远程查看";
}
