using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
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
