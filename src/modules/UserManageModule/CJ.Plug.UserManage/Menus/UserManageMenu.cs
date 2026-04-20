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
    public class UserManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.VerifiedUser,
                    Href = "/UserManageList",
                    Text = "人员管理",
                    GroupName = MenuGroupEnum.调试.ToString(),
                },
                new()
                {
                    Icon = Icons.Material.Filled.VerifiedUser,
                    Href = "/RoleManageList",
                    Text = "角色管理",
                    GroupName = MenuGroupEnum.调试.ToString(),
                },
                new()
                {
                    Icon = Icons.Material.Filled.DepartureBoard,
                    Href = "/DepartmentManage",
                    Text = "部门管理",
                    GroupName = MenuGroupEnum.调试.ToString(),
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
