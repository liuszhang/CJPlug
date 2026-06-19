using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
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
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
