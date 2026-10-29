using CJCore.Framework.Abstractions;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManageModule.Menus
{
    public class ToolActionSettingMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Power,
                    Href = "/TAS",
                    Text = "插头管理",
                    GroupName = "管理",
                    // P5.5：与页面 [ModuleAccess] 同键
                    AccessKey = "plug-tool-action",
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
