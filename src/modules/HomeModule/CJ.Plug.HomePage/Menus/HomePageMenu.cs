using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.HomePage.Menus
{
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
                //GroupName = MenuItemGroups.General.Name,
            }
        };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
