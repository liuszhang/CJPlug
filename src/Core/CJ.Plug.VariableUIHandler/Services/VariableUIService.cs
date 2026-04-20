using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.VariableUIHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Services
{
    public class VariableUIService(IEnumerable<IVariableUIHandler> handlers) : IVariableUIService
    {
        public IVariableUIHandler GetHandler(string uiHint)
        {
            var handler = handlers.FirstOrDefault(x => x.GetSupportsUIHint(uiHint));
            return handler ?? new DefaultVariableUIHandler();
        }
    }
    
}
