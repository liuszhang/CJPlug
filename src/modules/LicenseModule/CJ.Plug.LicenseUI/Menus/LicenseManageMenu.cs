using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.LicenseUI
{
    public class LicenseManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Security,
                    Href = "/LicenseManage",
                    Text = "许可证管理",
                    GroupName = MenuGroupEnum.管理.ToString(),
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }

    public class LicenseModule : IModule
    {
        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
