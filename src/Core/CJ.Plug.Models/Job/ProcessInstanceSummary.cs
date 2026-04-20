using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ProcessInstanceSummary
    {
        //
        // 摘要:
        //     The ID of the workflow instance.
        public string Id { get; set; }

        //
        // 摘要:
        //     The ID of the workflow definition.
        public string DefinitionId { get; set; }

        //
        // 摘要:
        //     The version ID of the workflow definition.
        public string DefinitionVersionId { get; set; }

        //
        // 摘要:
        //     The version of the workflow definition.
        public int Version { get; set; }

        //
        // 摘要:
        //     The status of the workflow instance.
        public string? Status { get; set; }

        //
        // 摘要:
        //     The sub-status of the workflow instance.
        public string? SubStatus { get; set; }

        //
        // 摘要:
        //     The ID of the workflow instance.
        public string? CorrelationId { get; set; }

        //
        // 摘要:
        //     The name of the workflow instance.
        public string? Name { get; set; }

        //
        // 摘要:
        //     The number of incidents associated with the workflow instance.
        public int IncidentCount { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was created.
        public DateTimeOffset CreatedAt { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was last executed.
        public DateTimeOffset? UpdatedAt { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was finished.
        public DateTimeOffset? FinishedAt { get; set; }
    }
}
