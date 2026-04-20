using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ExecuteSetting
    {
        public string? CorrelationId { get; set; }
        public bool IsCreateJob { get; set; } = true;

        //
        // 摘要:
        //     An optional activity ID to trigger.
        public string? TriggerActivityId { get; set; }

        //
        // 摘要:
        //     Optional input to pass to the workflow instance.
        public object? Input { get; set; }
    }
}
