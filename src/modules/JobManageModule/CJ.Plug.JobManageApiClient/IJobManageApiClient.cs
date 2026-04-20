using CJ.Plug.Models.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.JobManageApiClient
{
    public interface IJobManageApiClient
    {
        Task<BaseJob?> CreateJobAsync(BaseJob request, CancellationToken cancellationToken = default);
        Task<ProcessJob?> CreateJobAsync(ProcessJob request, CancellationToken cancellationToken = default);
        Task<ToolJob?> CreateToolJobAsync(ToolJob request, CancellationToken cancellationToken = default);
        Task<bool> DeleteJobAsync(BaseJob request, CancellationToken cancellationToken = default);
        Task<IEnumerable<BaseJob?>> GetAllJobsAsync(CancellationToken cancellationToken = default);
        Task<BaseJob?> GetJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default);
        Task<BaseJob?> GetJobByDefinitionIdAsync(string DefinitionId, CancellationToken cancellationToken = default);
        Task<List<BaseJob>?> GetJobsByFilter(JobFilter filter, CancellationToken cancellationToken = default);
        Task<ProcessJob?> GetProcessJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default);
        Task<ToolJob?> GetToolJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default);
        Task<ExecuteResultData?> GetToolJobResultByCorrelationIdAsync(string? CorrelationId, CancellationToken cancellationToken = default);
        Task<List<ToolJob>?> GetToolJobsByParentJobAsync(string ParentJobCorrelationId, CancellationToken cancellationToken = default);
        Task<ExecuteResultData?> SubmitNewToolExecute(string stationIp, PlugExecutionRequest request);
        Task SyncJournalData(string JobCorrelationId, CancellationToken cancellationToken = default);
        Task<BaseJob?> UpdateJobAsync(BaseJob request, CancellationToken cancellationToken = default);
        Task<ProcessJob?> UpdateProcessJobAsync(ProcessJob request, CancellationToken cancellationToken = default);
        Task<ToolJob?> UpdateToolJobAsync(ToolJob request, CancellationToken cancellationToken = default);
    }
}
