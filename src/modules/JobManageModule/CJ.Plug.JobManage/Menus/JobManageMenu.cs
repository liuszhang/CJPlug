using CJCore.Framework.Abstractions;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManageModule.Menus
{
    public class JobManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Task,
                    Href = "/JobManageList",
                    Text = "作业列表",
                    // P6.3：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   作用有二：① 权限矩阵把它当「可见性行」扫描出来；② 模块管理页经此键反查到 plug-job-manage
                    //   归属，从而把本菜单项渲染成该模块下的功能开关。缺此键 = 该模块无任何功能开关项。
                    AccessKey = "plug-job-manage",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
