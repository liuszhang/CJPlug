using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ToolJob : BaseJob
    {
        public string? ToolName { get; set; }
        public string? ToolVersion { get; set; }
        public string? StationIp { get; set; }

        /// <summary>
        /// 用于保存工具执行后的结果数据，由ExecuteResultData类序列化为JSON字符串，
        /// </summary>
        public string? ExecuteResultData { get; set; }
    }
}
