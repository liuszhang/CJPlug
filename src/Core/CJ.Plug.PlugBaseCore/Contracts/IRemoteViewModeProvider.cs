namespace CJ.Plug.PlugBaseCore.Contracts;

/// <summary>
/// 远程查看模式（<c>RemoteViewMode</c>）读取抽象。
///
/// <para><b>为什么需要这层抽象</b>（P7.1a，2026-10-30）：本值原先由
/// <see cref="Services.ToolExecuteService"/> 经 <c>MainApiClient.SystemConfigApiClient</c>
/// 打 ApiServer 的 <c>/api/systemconfig/get/RemoteViewMode</c> 读 —— 那是 CJPlug 私有的系统配置模块，
/// 已被 CJCore 通用配置页 <c>/system-config</c> 取代（模块连同 <c>SystemConfigItems</c> 表一并退役）。
/// 读侧改由宿主注入实现：主宿主（<c>CJPlug.Server</c>）读 CJCore 的
/// <c>ISystemConfigProvider</c>（同进程直读，无 HTTP）；未注册实现的宿主按
/// <see cref="RemoteViewModes.Fullscreen"/> 兜底。</para>
///
/// <para><b>⚠ 纯加法</b>：<c>CJ.Plug.PlugBaseCore</c> 被 39 个插件项目引用 ⇒ 本文件只允许新增类型，
/// 不得改动该程序集里任何既有公共签名。</para>
/// </summary>
public interface IRemoteViewModeProvider
{
    /// <summary>
    /// 读取当前生效的远程查看模式；未配置 / 读取失败返回 <c>null</c>（调用方按
    /// <see cref="RemoteViewModes.Fullscreen"/> 兜底，避免配置面异常扩散到工具执行链）。
    /// </summary>
    Task<string?> GetRemoteViewModeAsync(CancellationToken cancellationToken = default);
}

/// <summary>远程查看模式的取值常量（与配置页下拉选项一一对应，勿改字符串）。</summary>
public static class RemoteViewModes
{
    /// <summary>整桌面 VNC（历史默认值，也是所有兜底路径的取值）。</summary>
    public const string Fullscreen = "fullscreen";

    /// <summary>单窗口 RFB VNC（仅投射 <c>Tool.RemoteViewProcessName</c> 指定的窗体，端口 5901）。</summary>
    public const string Window = "window";
}
