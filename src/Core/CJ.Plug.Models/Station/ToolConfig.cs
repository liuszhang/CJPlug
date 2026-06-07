using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class ToolConfig
    {
        public int? Id { get; set; }
        public string? ToolName { get; set; }
        public int? ToolId { get; set; }
        public string? ToolPath { get; set; }

        /// <summary>
        /// 覆盖默认的工具包根目录
        /// </summary>
        public string? ToolBasePath { get; set; }

        public string? ToolVersion { get; set; }
        public string? CommandParameter { get; set; }
        public bool? IsDisabled { get; set; } = false;


        public int? StationConfigTableId { get; set; }
        [JsonIgnore]
        public StationConfigTable? StationConfigTable { get; set; }
    }
}
