using CJ.Plug.LoginPages;
using CJ.Plug.LoginPages.Pages;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.LoginPages
{
    public class LoginModule(IAppBarService appBarService) : ModuleBase
    {
        public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            appBarService.AddAppBarItem<LoginState>();
            return base.InitializeAsync(cancellationToken);
        }
    }
}
