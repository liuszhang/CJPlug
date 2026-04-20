using Blazor.Diagrams.Core.Models;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.SharedPages.DataConnector.Models
{
    public class PlugVariablePort :PortModel
    {
        public PlugVariablePort(
            NodeModel parent, 
            PlugVariableItemModel variable, 
            PortAlignment alignment = PortAlignment.Bottom,
            string? identifier=null)
            : base(parent, alignment, null, null)
        {
            Variable = variable;
            Identifier = identifier;
        }

        public string? Identifier { get; set; }

        [JsonIgnore]
        public PlugVariableItemModel Variable { get; }

    }
}
