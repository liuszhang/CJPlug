using CJ.Plug.Models.Plug;
using CJ.Plug.Models.ProcessToExternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.ProcessToExternal
{
    public record PlugExecuteContext(PlugExecutionRequest Request,CallbackContext CallbackContext);
}
