using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.ApiClient.Contracts
{
    public interface INameGenerator
    {
        string GetNextName(string name,List<string>? existingNames);
        bool IsValidName(string name);
    }
}
