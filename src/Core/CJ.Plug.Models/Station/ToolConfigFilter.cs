using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class ToolConfigFilter
    {
        public string? StationIP { get; set; }
        public string? ToolName { get; set; }
        public string? ToolVersion { get; set; }
        public bool? IsDisabled { get; set; } = false;

        public int? ToolId { get; set; }
        public int? StationId { get; set; }
    }
}
