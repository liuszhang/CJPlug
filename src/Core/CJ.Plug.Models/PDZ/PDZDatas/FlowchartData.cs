using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class FlowchartData : BaseVariable
    {
        [JsonIgnore]
        public PlugDataZone? PlugDataZone { get; set; }
        public int PlugDataZoneId { get; set; }

        //标识该参数属于哪个插头
        public string? PlugDefinitionId { get; set; }
        
    }


