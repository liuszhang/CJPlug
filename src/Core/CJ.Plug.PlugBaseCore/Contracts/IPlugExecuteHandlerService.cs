using CJ.Plug.VariableUIHandler.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugExecuteHandlerService
    {
        IPlugCommonExecute GetExecuteHandler(string? PlugTypeKey);
    }
}
