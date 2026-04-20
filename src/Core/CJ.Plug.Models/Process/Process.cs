using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.PlugProcess
{
    public class Process:Plug.Plug
    {
        //public string? TriggerPlugId { get; set; } //触发器活动ID
        //public string? TriggerPlugDefinitionId { get; set; } //触发器活动流程ID
        public int? Version { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
