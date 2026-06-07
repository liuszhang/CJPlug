using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.KnowledgeManage.Menus;

/// <summary>
/// 知识管理菜单注册，归组到 "Skills管理"
/// </summary>
public class KnowledgeManageMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.MenuBook,
                Href = "/KnowledgeManage",
                Text = "知识管理",
                //GroupName = "Skills管理"
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
