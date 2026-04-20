using CJ.Plug.Models.Plug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Models
{
    public record ExecuteServiceContext(CJ.Plug.Models.Plug.Plug? plugToExecute=null, PlugExecutionRequest? plugExecutionRequest = null);
}
