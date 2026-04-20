using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class StationConfigTable
    {
        public int? Id { get; set; }
        public string? StationIp { get; set; }
        public string? StationStatus { get; set; }
        public List<ToolConfig>? ToolConfigs { get; set; }
    }
}
