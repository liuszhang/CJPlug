using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IModule
    {
        ValueTask InitializeAsync(CancellationToken cancellationToken = default);
    }
}
