using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// 用 CJPlug 自己的菜单项<b>替换</b>框架 <c>SystemConfigUiMenuService</c> 提供的那条。
///
/// <para><b>为什么必须替换</b>：<see cref="SystemConfigUiMenuService"/> 把 GroupName 写死成 “系统”，
/// 而 D3=A 已定 CJPlug 的菜单<b>归一为「通用 / 管理 / 调试」三组</b>，
/// 多一个“系统”组等于多出一个孤零零的抽屉分区（只有一条菜单）。</para>
///
/// <para><b>为什么不全盘否定框架菜单</b>：路由发现要靠 <c>SystemConfigUiModule</c>（<c>/system-config</c> 页面就在
/// 那个程序集里）⇒ 仍然要调 <c>AddCJCoreSystemConfigUI()</c>，然后在 Program.cs 里<b>摘掉</b>
/// 它的菜单注册、换成本类（同一 Href、同一 <c>AccessKey</c>，仅分组与排序不同）。</para>
///
/// <para>Group 用<b>字面量</b>而非枚举 —— P1 已把 CJPlug 20 个菜单类的 <c>MenuGroupEnum</c> 全部展平成
/// 字面量（“通用 / 管理 / 调试”），这里保持一致。</para>
/// </summary>
public sealed class PlugSystemConfigMenuService : IMenuService
{
    private readonly CJCore.Modules.SystemConfig.SystemConfigModuleOptions _options;

    public PlugSystemConfigMenuService(CJCore.Modules.SystemConfig.SystemConfigModuleOptions options)
        => _options = options;

    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        // ⚠ 2026-10-29 冒烟实证：本项曾<b>不在菜单里</b>，但本方法确实被调用了两次 ⇒
        //   不是 DI 未注册，而是被 CoreMenu 的「角色可见性」过滤掉的。
        //   根因：<see cref="SystemConfigModuleOptions.AccessRoles"/> 默认 ["Admin","SystemAdmin"]，
        //   而 P5 之前 CJPlug 没有真认证体系，AuthenticationStateProvider 恒匿名 ⇒ 角色判定恒否。
        //   对照实验：同结构的 <c>PlugModuleAdminMenuService</c> 不挂 Roles 就实测可见，两者唯一差异即在此。
        //
        //   P5 接 ThreeRoleAdmin + Permission 矩阵（D9=A）后，应改回：
        //     Roles     = _options.AccessRoles,
        //     AccessKey = SystemConfigModuleOptions.AccessKey,
        //   届时才真正实现「按权限显示菜单」。
        MenuItem menu = new()
        {
            Text = "系统配置",
            Href = "/system-config",
            Icon = Icons.Material.Filled.Settings,
            GroupName = "管理",          // ← D3=A 归一后的分组；框架原为“系统”
            Order = 900,                 // 管理组内的相对位置（同 MyNavMenu 时代的“垫底”观感）
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>([menu]);
    }
}
