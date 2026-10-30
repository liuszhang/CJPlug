using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.ModelManage.Menus
{
    public class OntologyManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.AccountTree,
                    Href = "/OntologyManage",
                    Text = "本体管理",
                    // P6.4：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   缺此键 = 模块管理页无该模块的功能开关项，且关模块对本菜单无效。
                    AccessKey = "plug-model-manage",
                }
            };
            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
