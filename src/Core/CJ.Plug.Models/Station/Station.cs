using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class Station
    {
        public int? Id { get; set; }
        public string? StationIp { get; set; }
        public string? StationName { get; set; }
        public string? StationStatus { get; set; }
        public string? UpdateTime { get; set; }
        public bool IsStarted { get; set; } = false;
        /// <summary>
        /// 图站分类，可能是Windows、Linux等
        /// </summary>
        public string? StationCategory { get; set; } = "Windows";
        /// <summary>
        /// 图站存放数据的基础路径，可能是Windows的C:\PlugStation\或Linux的/home/plugstation/
        /// </summary>
        public string? StationBasePath { get; set; } = @"C:\PlugStationData";
    }
}
