using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.AuthUI
{
    public class AuthManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.AdminPanelSettings,
                    Href = "/AuthManage",
                    Text = "授权管理",
                    GroupName = MenuGroupEnum.管理.ToString(),
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }

    public class AuthModule : IModule
    {
        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
