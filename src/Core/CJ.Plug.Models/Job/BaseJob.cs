using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class BaseJob
    {
        public int? Id { get; set; }

        //作业唯一识别号
        public string? JobDefinitionId { get; set; }

        ///自定义用于跟踪作业的ID，通常是用于和引擎及PDZ关联的ID
        public string? JobCorrelationId { get; set; }

        /// <summary>
        /// 用于标识作业的类别，默认是Process
        /// </summary>
        public string JobCategory { get; set; } = JobCategoryEnum.ProcessJob.ToString(); //默认是Process
        /// <summary>
        /// 父作业ID
        /// </summary>
        public int? ParentJobId { get; set; }
        /// <summary>
        /// 父作业关联ID，用于不便获取ParentJobId时使用
        /// </summary>
        public string? ParentJobCorrelationId { get; set; }

        //
        // 摘要:
        //     The status of the workflow instance.
        public string? JobStatus { get; set; }

        //
        // 摘要:
        //     The sub-status of the workflow instance.
        public string? JobSubStatus { get; set; } = Job.JobSubStatus.执行中.ToString();

        //
        // 摘要:
        //     The ID of the workflow engine instance.
        public string? EngineInstanceId { get; set; }

        //
        // 摘要:
        //     The name of the workflow instance.
        public string? Name { get; set; }

        //
        // 摘要:
        //     The number of incidents that have occurred during execution, if any.
        public int IncidentCount { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was created.
        public DateTimeOffset CreatedAt { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was last executed.
        public DateTimeOffset UpdatedAt { get; set; }

        //
        // 摘要:
        //     The timestamp when the workflow instance was finished.
        public DateTimeOffset? FinishedAt { get; set; }


        /// <summary>
        /// 执行结果数据存放路径，默认是[PDZ基础路径+JobId]。
        /// 可能为空，只在需要存储执行结果数据时才会使用。
        /// </summary>
        public string? JobDataFolderPath { get; set; }
    }

}
