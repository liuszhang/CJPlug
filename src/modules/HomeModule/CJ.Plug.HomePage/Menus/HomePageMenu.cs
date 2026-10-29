using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.HomePage.Menus
{
    /// <summary>
    /// 首页菜单（2026-10-29 接入 P1：改用 CJCore 的 IMenuService / MenuItem 契约）。
    /// <para>差异仅为类型来源；<c>Match</c> 在 CJCore 是 string（"Prefix"/"Exact"），此处不设置即默认 Prefix。</para>
    /// </summary>
    public class HomePageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Home,
                    Href = "/",
                    Text = "首页",
                    GroupName = "通用",
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
