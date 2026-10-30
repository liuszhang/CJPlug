using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.MainPageContent.Shell.Menus;

/// <summary>
/// 宿主壳菜单（2026-10-29《CJPlug 接入 CJCore》P2-2.4）。
/// <para>
/// 承接原 <c>MyNavMenu.razor</c> 里<b>硬编码</b>的入口（它们不属于任何业务模块）：
/// VNC 测试（/remote/vnc/localhost）、VNC 测试（capture）（/remote/capture/localhost）。
/// </para>
/// <para>
/// ⚠ 「本地部署包」（/download-package）已于 <b>P7.1b</b> 移除：随 P7.1a 死代码清理把
/// <c>DownloadPackage.razor</c> 页面一并删了，但该菜单项当时**漏删** ⇒ 点了必 404
/// （<c>find -name "DownloadPackage*"</c> 全仓 0 命中）。
/// </para>
/// <para>
/// ⚠ 「作业列表2」（/JobManageList2，709 行重复作业列表页）随评审 Q9 下线，
/// <b>本菜单刻意不承接</b>；页面文件保留到 P5 一并删除，届时路由自然消失。
/// </para>
/// </summary>
public sealed class PlugShellMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IEnumerable<MenuItem>>(new List<MenuItem>
        {
            new()
            {
                Icon       = Icons.Material.Filled.DesktopWindows,
                Href       = "/remote/vnc/localhost",
                Text       = "VNC测试",
                GroupName  = "调试",
                Match      = "Prefix",
                Order      = 910,
                // P5.5：与远程桌面页面 [ModuleAccess("plug-guacamole")] 同键（原 plug-shell 会与页面口径漂移）
                AccessKey  = "plug-guacamole",
            },
            new()
            {
                Icon       = Icons.Material.Filled.DesktopWindows,
                Href       = "/remote/capture/localhost",
                Text       = "VNC测试（capture）",
                GroupName  = "调试",
                Match      = "Prefix",
                Order      = 920,
                AccessKey  = "plug-guacamole",
            },
        });
}
