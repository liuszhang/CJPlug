using CJ.Plug.Models.Contracts;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Abstractions
{
    public abstract class ModuleBase:IModule
    {
        public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
