using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManageModule.Menus
{
    public class FMDMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.AccountTree,
                    Href = "/FMD",
                    Text = "对象管理",
                    GroupName = MenuGroupEnum.调试.ToString(),
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
