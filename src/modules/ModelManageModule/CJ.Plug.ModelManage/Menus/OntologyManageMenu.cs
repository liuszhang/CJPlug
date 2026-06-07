using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
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
                }
            };
            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }
}
