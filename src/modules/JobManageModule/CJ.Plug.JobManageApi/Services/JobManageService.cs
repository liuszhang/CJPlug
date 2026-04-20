using CJ.Plug.JobManageApi.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

namespace CJ.Plug.JobManageApi.Services
{
    public partial class JobManageService : IJobManageService
    {
        private readonly MainDbContext _dbContext;

        private readonly ElsaApiClient ElsaApiClient;

        public JobManageService(MainDbContext dbContext, ElsaApiClient elsaApiClient)
        {
            _dbContext = dbContext;
            ElsaApiClient = elsaApiClient;
        }

        public async Task<IEnumerable<BaseJob>> GetJobs()
        {
            return await _dbContext.Set<BaseJob>().ToListAsync();
        }

        public async Task<ProcessJob?> CreateJob(ProcessJob job)
        {
            job.CreatedAt = DateTime.UtcNow.ToLocalTime();
            job.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ProcessJob>().Add(job);
            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<ToolJob?> CreateToolJob(ToolJob job)
        {
            job.CreatedAt = DateTime.UtcNow.ToLocalTime();
            job.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ToolJob>().Add(job);
            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<BaseJob?> CreateNewJob(BaseJob job)
        {
            job.CreatedAt = DateTime.UtcNow.ToLocalTime();
            job.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<BaseJob>().Add(job);
            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<bool?> DeleteJob(int jobId)
        {
            var job = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.Id == jobId);
            if (job != null)
            {
                _dbContext.Set<BaseJob>().Remove(job);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<ProcessJob?> GetJobById(int jobId)
        {
            var job = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.Id == jobId);
            return job;
        }

        public async Task<ProcessJob?> GetJobByName(string name)
        {
            var job = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.Name == name);
            return job;
        }

        public async Task<ProcessJob?> GetJobByWorkflowId(string workflowDefinitionId)
        {
            var job = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.ProcessDefinitionId == workflowDefinitionId);
            return job;
        }


        public async Task<BaseJob?> UpdateJob(BaseJob request)
        {
            var job = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.Id == request.Id);
            if (job == null)
            {
                return null;
            }

            //Log.Information($"接收到的作业完整数据: {JsonSerializer.Serialize(request)}");

            //job.Id = request.Id;
            job.Name = request.Name;
            job.JobStatus = request.JobStatus;
            job.JobSubStatus = request.JobSubStatus;
            job.UpdatedAt = request.UpdatedAt;
            job.CreatedAt = request.CreatedAt;
            job.FinishedAt = request.FinishedAt;
            job.IncidentCount = request.IncidentCount;
            job.EngineInstanceId = request.EngineInstanceId;
            job.UpdatedAt = DateTime.UtcNow.ToLocalTime();

            // 使用模式匹配直接处理request对象
            switch (request)
            {
                case ToolJob toolJobRequest:
                    Log.Information($"接收到的ToolJob的特殊属性{toolJobRequest.ExecuteResultData}");
                    if (job is ToolJob toolJob)
                    {
                        toolJob.ToolName = toolJobRequest.ToolName;
                        toolJob.ToolVersion = toolJobRequest.ToolVersion;
                        toolJob.StationIp = toolJobRequest.StationIp;
                        toolJob.ExecuteResultData = toolJobRequest.ExecuteResultData;
                    }
                    break;

                case ProcessJob processJobRequest:
                    Log.Information($"接收到的ProcessJob的特殊属性{processJobRequest.ProcessDefinitionId}");
                    if (job is ProcessJob processJob)
                    {
                        processJob.ProcessDefinitionId = processJobRequest.ProcessDefinitionId;
                    }
                    break;
            }


            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<ToolJob?> UpdateToolJob(ToolJob request)
        {
            var job0 = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.Id == request.Id);
            if (job0 == null)
            {
                return null;
            }

            //Log.Information($"接收到的作业完整数据: {JsonSerializer.Serialize(request)}");

            var job = job0 as ToolJob;

            //job.Id = request.Id;
            job.Name = request.Name;
            job.JobStatus = request.JobStatus;
            job.JobSubStatus = request.JobSubStatus;
            job.UpdatedAt = request.UpdatedAt;
            job.CreatedAt = request.CreatedAt;
            job.FinishedAt = request.FinishedAt;
            job.IncidentCount = request.IncidentCount;
            job.EngineInstanceId = request.EngineInstanceId;
            job.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            job.ToolName = request.ToolName;
            job.ToolVersion = request.ToolVersion;
            job.StationIp = request.StationIp;

            job.ExecuteResultData = request.ExecuteResultData;


            await _dbContext.SaveChangesAsync();
            return job;
        }

        public async Task<ProcessJob?> UpdateProcessJob(ProcessJob request)
        {
            var job0 = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.Id == request.Id);
            if (job0 == null)
            {
                return null;
            }
            _dbContext.Entry(job0).CurrentValues.SetValues(request);
            await _dbContext.SaveChangesAsync();
            return job0;
        }

        //获取指定父作业下的所有工具作业
        public async Task<IEnumerable<ToolJob>?> GetToolJobsByParentJob(string ParentJobCorrelationId)
        {
            var jobs = await _dbContext.Set<BaseJob>().OfType<ToolJob>()
                .Where(j => j.ParentJobCorrelationId == ParentJobCorrelationId)
                .ToListAsync();
            return jobs;
        }


        public async Task<BaseJob?> GetJobByCorrelationId(string CorrelationId)
        {
            var job = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.JobCorrelationId == CorrelationId);
            return job;
        }

        public async Task<ProcessJob?> GetProcessJobByCorrelationId(string CorrelationId)
        {
            var job = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.JobCorrelationId == CorrelationId);
            return job;
        }

        public async Task<BaseJob?> GetJobByDefinitionId(string DefinitionId)
        {
            var job = await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(j => j.JobDefinitionId == DefinitionId);
            return job;
        }

        public async Task<List<BaseJob>> GetJobsByFilter(JobFilter filter)
        {
            IQueryable<BaseJob> query = _dbContext.Set<BaseJob>();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(filter.JobName))
            {
                query = query.Where(j => j.Name.Contains(filter.JobName));
            }
            if (!string.IsNullOrEmpty(filter.CorrelationId))
            {
                query = query.Where(j => j.JobCorrelationId == filter.CorrelationId);
            }
            // 其他条件...

            return await query.ToListAsync(); // 自动包含所有子类
        }


        //同步流程引擎中的执行历程数据至Job中
        public async Task SyncJournalData(string JobCorrelationId)
        {
            try
            {
                var job = await _dbContext.Set<ProcessJob>().FirstOrDefaultAsync(j => j.JobCorrelationId == JobCorrelationId);
                if (job == null)
                {
                    CLog.Error($"未找到对应的作业，无法同步执行历程数据。CorrelationId: {JobCorrelationId}");
                    return;
                }
                // 获取流程引擎中的执行历程数据
                var instance = await ElsaApiClient.GetWorkflowInstanceByCorrelationIdAsync(JobCorrelationId);
                if (instance == null)
                {
                    CLog.Error($"未找到对应的工作流引擎实例，CorrelationId: {JobCorrelationId}");
                    return;
                }
                var journalData = await ElsaApiClient.GetJournalAsync(instance.DefinitionId);
                if (journalData == null)
                {
                    CLog.Error($"未找到对应的执行历程数据，CorrelationId: {JobCorrelationId}");
                    return;
                }
                Log.Information($"同步执行历程数据成功，CorrelationId: {JobCorrelationId}, JournalData Count: {JsonSerializer.Serialize(journalData)}");
                // 更新作业的执行历程数据
                job.JournalData = JsonSerializer.Serialize(journalData.Items);
                job.UpdatedAt = DateTime.UtcNow.ToLocalTime();
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                CLog.Error($"同步执行历程数据失败，CorrelationId: {JobCorrelationId}");
                CLog.Error(ex.StackTrace);
                CLog.Error(ex.Message);
            }



        }

    }

}
