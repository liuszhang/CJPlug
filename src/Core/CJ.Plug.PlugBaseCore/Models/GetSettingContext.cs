using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Models
{
    public record GetSettingContext(string? PlugTypeKey, string? PlugDefinitionId, string? PDZId,Plug.Models.Plug.Plug? Plug=null);
}
