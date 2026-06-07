using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public record JobFilter
    {
        public string? JobType { get; set; }
        public int? Id { get; set; }
        public string? CorrelationId { get; set; }
        public int? ProcessId { get; set; }
        public string? ProcessDefinitionId { get; set; }
        public int? PlugId { get; set; }
        public string? PlugDefinitionId { get; set; }
        public int? ToolId { get; set; }
        public string? ToolName { get; set; }
        public string? ToolVersion { get; set; }
        public string? ToolDisplayName { get; set; }
        public string? JobStatus { get; set; }
        public string? JobSubStatus { get; set; }
        public string? JobName { get; set; }
        public bool? IsRunning { get; set; }
        public bool? HasError { get; set; }

        /// <summary>
        /// 模糊搜索关键词，匹配 Name、JobCorrelationId、EngineInstanceId
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// 创建时间起始
        /// </summary>
        public DateTimeOffset? DateFrom { get; set; }

        /// <summary>
        /// 创建时间截止
        /// </summary>
        public DateTimeOffset? DateTo { get; set; }
    }
}
