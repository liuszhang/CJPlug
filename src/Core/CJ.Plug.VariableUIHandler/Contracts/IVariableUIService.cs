using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Contracts
{
    public interface IVariableUIService
    {
        IVariableUIHandler GetHandler(string uiHint);
    }
}
