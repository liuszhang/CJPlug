using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Contracts
{
    public interface IVariableTypeService
    {
        List<string>? GetVariableTypeList();
        List<string>? GetBrowserableVariableTypeList();
        IVariableTypeHandler GetHandler(string type);
    }
}
