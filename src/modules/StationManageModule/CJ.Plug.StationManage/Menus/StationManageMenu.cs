using CJ.Plug.LicenseApiClient;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.StationManage.Menus
{
    public class StationManageMenu : IMenuService
    {
        private readonly ILicenseApiClient _licenseApiClient;

        public StationManageMenu(ILicenseApiClient licenseApiClient)
        {
            _licenseApiClient = licenseApiClient;
        }

        public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var status = await _licenseApiClient.GetStatusAsync(cancellationToken);
            if (!status.IsActivated || status.IsExpired)
                return Array.Empty<MenuItem>();

            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.Workspaces,
                    Href = "/StationManage",
                    Text = "图站管理",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return menuItems;
        }
    }
}
