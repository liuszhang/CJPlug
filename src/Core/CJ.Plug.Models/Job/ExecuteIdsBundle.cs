using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ExecuteIdsBundle
    {
        public string? ProcessJobEngineId { get; set; }  //引擎ID
        //public string? ProcessJobCorrelationId { get; set; }
        //public string? PlugJobCorrelationId { get; set; }
        public string? ToolJobCorrelationId { get; set; }  //如果有工具作业，记录工具作业的ID
        //public string? ParentJobCorrelationId { get; set; }  //父作业业务ID
        public string? JobCorrelationId { get; set; } //作业业务ID
        public string? PlugDefinitionId { get; set; }
        //public string? ProcessDefinitionId { get; set; }
        //public string? BookmarkId { get; set; }

        public string? PDZId { get; set; }

        public List<string>? ExecuteTaskPlugIds { get; set; } = new List<string>();
    }
}
