using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IMenuService
    {

        ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default(CancellationToken));

        //ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
