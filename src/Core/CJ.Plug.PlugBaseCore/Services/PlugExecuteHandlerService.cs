using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.VariableUIHandler.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Services
{
    public class PlugExecuteHandlerService(IEnumerable<IPlugCommonExecute> handlers) : IPlugExecuteHandlerService
    {
        public IPlugCommonExecute GetExecuteHandler(string? PlugTypeKey)
        {
            var handler = handlers.FirstOrDefault(x => x.IsThisPlugTypeKey(PlugTypeKey));
            return handler ?? throw new InvalidOperationException($"No execute handler found for PlugTypeKey: {PlugTypeKey}");
        }
    }
    
}
