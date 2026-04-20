using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.MCPToolsManage.Menus
{

    public class SkillsManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = @Icons.Material.Filled.DeviceHub,
                    Href = "/SkillsManage",
                    Text = "Skills管理",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
