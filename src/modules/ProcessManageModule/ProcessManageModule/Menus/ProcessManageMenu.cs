using CJCore.Framework.Abstractions;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManageModule.Menus
{
    public class ProcessManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.AccountTree,
                    Href = "/ProcessManageList",
                    Text = "流程管理",
                    // P6.3：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   作用：① 权限矩阵扫描为可见性行；② 模块管理页经此键反查模块归属，生成功能开关项。
                    AccessKey = "plug-process-manage",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
