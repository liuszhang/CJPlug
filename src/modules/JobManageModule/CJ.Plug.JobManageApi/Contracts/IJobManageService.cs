using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.JobManageApi.Contracts
{
    public interface IJobManageService
    {
        Task<IEnumerable<BaseJob>> GetJobs();
        Task<ProcessJob?> CreateJob(ProcessJob job);
        Task<ToolJob?> CreateToolJob(ToolJob job);
        Task<BaseJob?> CreateNewJob(BaseJob job);
        Task<BaseJob?> UpdateJob(BaseJob job);
        Task<ToolJob?> UpdateToolJob(ToolJob job);
        Task<ProcessJob?> UpdateProcessJob(ProcessJob job);
        Task<IEnumerable<ToolJob>?> GetToolJobsByParentJob(string ParentJobCorrelationId);
        Task<bool?> DeleteJob(int jobId);
        Task<ProcessJob?> GetJobById(int jobId);
        Task<ProcessJob?> GetJobByName(string name);
        Task<ProcessJob?> GetJobByWorkflowId(string workflowDefinitionId);
        Task<BaseJob?> GetJobByCorrelationId(string CorrelationId);
        Task<BaseJob?> GetJobByDefinitionId(string DefinitionId);
        Task<List<BaseJob>> GetJobsByFilter(JobFilter filter);
        Task SyncJournalData(string JobCorrelationId);
        Task<ProcessJob?> GetProcessJobByCorrelationId(string CorrelationId);

        //Task ReportExecuteResult(ExecuteResultData executeReport);


    }
}
