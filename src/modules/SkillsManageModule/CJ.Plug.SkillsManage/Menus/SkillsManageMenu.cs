using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.SkillsManage.Menus;

public class SkillsManageMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Build,
                Href = "/SkillsList",
                Text = "Skills管理",
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}