using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    public class PlugVariable:BaseVariable
    {
        public int? PlugId { get; set; }

        [JsonIgnore]
        public Plug? Plug { get; set; }
    }

}
