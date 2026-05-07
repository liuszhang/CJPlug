using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;

namespace CJ.Plug.AuditUI
{
    public class AuditManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.History,
                    Href = "/AuditManage",
                    Text = "审计管理",
                    GroupName = MenuGroupEnum.管理.ToString(),
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }

    public class AuditModule : IModule
    {
        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
