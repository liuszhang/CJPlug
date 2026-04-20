using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.ProcessToExternal
{
    public record ProcessInput(string? Name,string? Type,bool Required=false,string? Value=null);
    
}
