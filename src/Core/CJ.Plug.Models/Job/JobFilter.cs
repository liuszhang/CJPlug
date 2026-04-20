using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public record JobFilter
    {
        public string? JobType;
        public int? Id;
        public string? CorrelationId;
        public int? ProcessId;
        public string? ProcessDefinitionId;
        public int? PlugId;
        public string? PlugDefinitionId;
        public int? ToolId;
        public string? ToolName;
        public string? ToolVersion;
        public string? ToolDisplayName;
        public string? JobStatus;
        public string? JobSubStatus;
        public string? JobName;
        public bool? IsRunning;
        public bool? HasError;
    }
}
