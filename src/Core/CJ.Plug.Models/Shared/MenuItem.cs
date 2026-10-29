using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    /// <summary>
    /// 已废弃：2026-10-29《CJPlug 接入 CJCore》P1 起菜单项统一使用
    /// <c>CJCore.Framework.Abstractions.MenuItem</c>（<c>Match</c> 由 <c>NavLinkMatch</c>
    /// 改为字符串 <c>"Prefix"/"Exact"</c>，<c>GroupName</c> 默认值由 <c>"General"</c> 改为 <c>"通用"</c>）。
    /// 本类型不再被使用，P2 删除。
    /// </summary>
    [Obsolete("改用 CJCore.Framework.Abstractions.MenuItem")]
    public class MenuItem
    {
        //
        // 摘要:
        //     The icon to use in SVG format.
        public string? Icon { get; set; }

        //
        // 摘要:
        //     The path to navigate to.
        public string Href { get; set; }

        //
        // 摘要:
        //     The Microsoft.AspNetCore.Components.Routing.NavLinkMatch to use.
        public NavLinkMatch Match { get; set; }

        //
        // 摘要:
        //     The text to display.
        public string Text { get; set; }

        //
        // 摘要:
        //     The order of the menu item.
        public float Order { get; set; }

        //
        // 摘要:
        //     A list of sub menu items.
        public ICollection<MenuItem> SubMenuItems { get; set; } = new List<MenuItem>();


        //
        // 摘要:
        //     The name of the group this menu item belongs to.
        public string GroupName { get; set; } = "General";

    }
}
