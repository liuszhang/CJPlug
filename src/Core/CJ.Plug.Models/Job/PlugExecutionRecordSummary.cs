using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class PlugExecutionRecordSummary
    {
        /// <summary>
        /// Gets or sets the workflow instance ID.
        /// </summary>
        public string WorkflowInstanceId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the activity ID.
        /// </summary>
        public string ActivityId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the activity node ID.
        /// </summary>
        public string ActivityNodeId { get; set; } = default!;

        /// <summary>
        /// The type of the activity.
        /// </summary>
        public string ActivityType { get; set; } = default!;

        /// <summary>
        /// The version of the activity type.
        /// </summary>
        public int ActivityTypeVersion { get; set; }

        /// <summary>
        /// The name of the activity.
        /// </summary>
        public string? ActivityName { get; set; }

        /// <summary>
        /// Gets or sets the time at which the activity execution began.
        /// </summary>
        public DateTimeOffset StartedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the activity has any bookmarks.
        /// </summary>
        public bool HasBookmarks { get; set; }

        /// <summary>
        /// Gets or sets the status of the activity.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the time at which the activity execution completed.
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

    }
}
