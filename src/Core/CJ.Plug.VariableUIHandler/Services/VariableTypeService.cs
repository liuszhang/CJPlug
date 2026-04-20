using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.VariableUIHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Services
{
    public class VariableTypeService(IEnumerable<IVariableTypeHandler> handlers) : IVariableTypeService
    {
        public List<string>? GetVariableTypeList()
        {
            return handlers.Select(x => x.GetTypeName()).ToList();
        }
        public List<string>? GetBrowserableVariableTypeList()
        {
            return handlers.Where(x => x.GetTypeBrowserable()).Select(x => x.GetTypeName()).ToList();
        }
        public IVariableTypeHandler GetHandler(string type)
        {
            var handler = handlers.FirstOrDefault(x => x.GetHandlerOfType(type));
            return handler ?? new StringTypeHandler();
        }
    }

}
