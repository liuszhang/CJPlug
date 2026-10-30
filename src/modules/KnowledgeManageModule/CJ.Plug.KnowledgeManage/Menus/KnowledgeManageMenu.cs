using CJCore.Framework.Abstractions;
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
                // P6.4：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                //   缺此键 = 模块管理页无该模块的功能开关项，且关模块对本菜单无效。
                AccessKey = "plug-knowledge",
                //GroupName = "Skills管理"
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
