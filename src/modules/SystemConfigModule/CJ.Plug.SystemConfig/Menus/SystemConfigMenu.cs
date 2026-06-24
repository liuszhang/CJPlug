using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.SystemConfig.Menus;

public class SystemConfigMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Settings,
                Href = "/SystemConfig",
                Text = "系统配置",
                GroupName = MenuGroupEnum.管理.ToString(),
            }
        };
        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
