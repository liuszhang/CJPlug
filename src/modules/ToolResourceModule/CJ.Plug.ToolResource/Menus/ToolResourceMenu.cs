using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.ToolResource.Menus
{
    //public class ToolResourceMenu
    //{
    //    public const string Title = "工具资源";
    //    public const string Icon = "Build";
    //    public const string Route = "/ToolResource";
    //}

    public class ToolResourceMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Build,
                    Href = "/ToolResource",
                    Text = "工具资源",
                    // P6.4：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   缺此键 = 模块管理页无该模块的功能开关项，且关模块对本菜单无效。
                    AccessKey = "plug-tool-resource",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
