using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IHubConnectionManager
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task InvokeAsync<T>(string methodName, T? arguments);
    }
}
