using CJCore.Framework.Abstractions;
using CJCore.Modules.ModuleAdmin;
using MudBlazor;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// 用 CJPlug 自己的菜单项<b>替换</b>框架 <c>ModuleAdminUiMenuService</c> 提供的那条
/// （2026-10-29《CJPlug 接入 CJCore》P6，冒烟实测后修正）。
///
/// <para><b>为什么必须替换</b>：<see cref="CJCore.Modules.ModuleAdmin.ModuleAdminUiMenuService"/>
/// 把 <c>GroupName</c> 写死成 “系统”，而 D3=A 已定 CJPlug 的菜单<b>归一为「通用 / 管理 / 调试」三组</b>。
/// 冒烟实测结果：菜单里出现了第 4 个分组「系统」，且该组下只有「模块管理」一条 ——
/// 正是抽屉里多出一个孤零零分区的现象，与 P7.1 处理 SystemConfig 菜单时预见的一致。</para>
///
/// <para><b>为什么保留 AccessKey</b>：框架那条 <c>module-admin</c> 键在 P5 之前就<b>实测可见</b>
/// （走策略路由器判定放行），说明该键的权限解析在本宿主当前状态下是通的，故原样沿用，
/// 避免把已知的可用行为改坏。P5 换 ThreeRoleAdmin 后随角色名一起复核。</para>
///
/// <para><b>为什么不能摘 <c>AddCJCoreModuleAdminUI()</c> 整体</b>：<c>/admin/modules</c> 页面就在
/// <c>CJCore.Modules.ModuleAdmin.UI</c> 程序集里，<c>ModuleAdminUiModule</c> 还要负责把它带进路由发现。
/// 因此只摘它的菜单注册实例（Program.cs 里 Remove），换成本类。</para>
/// </summary>
public sealed class PlugModuleAdminMenuService : IMenuService
{
    private readonly ModuleAdminOptions _options;

    public PlugModuleAdminMenuService(ModuleAdminOptions options) => _options = options;

    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        MenuItem menuItem = new()
        {
            Icon = Icons.Material.Filled.Extension,
            Href = "/admin/modules",
            Text = "模块管理",
            GroupName = "管理",     // ← D3=A 归一后的分组；框架原为“系统”
            Order = 40,             // 沿用框架原排序
            Roles = _options.AdminRoles,
            AccessKey = _options.AdminAccessKey,
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>([menuItem]);
    }
}
