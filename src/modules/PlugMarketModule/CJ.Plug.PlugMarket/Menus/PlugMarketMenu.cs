using CJCore.Framework.Abstractions;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManageModule.Menus
{
    public class PlugMarketMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Apps,
                    Href = "/PlugMarket",
                    Text = "插头市场",
                    // P6.4：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   缺此键 = 模块管理页无该模块的功能开关项，且关模块对本菜单无效。
                    AccessKey = "plug-market",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
