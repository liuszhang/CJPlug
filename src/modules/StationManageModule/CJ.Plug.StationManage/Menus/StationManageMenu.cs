using CJCore.Framework.Abstractions;
using CJ.Plug.LicenseApiClient;
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
                    // P6.4：与 Module.ModuleId / IModuleAccessDeclaration.AccessKey 三处逐字一致。
                    //   缺此键 = 模块管理页无该模块的功能开关项，且关模块对本菜单无效。
                    AccessKey = "plug-station",
                    //GroupName = MenuItemGroups.General.Name,
                }
            };

            return menuItems;
        }
    }
}
