using CJ.Plug.JobManageApi.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.JobManageApi.Apis
{
    public static class JobManageApi
    {
        public static IEndpointRouteBuilder MapJobManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/job").WithTags("作业管理");

            api.MapGet("/getJobs", async (IJobManageService service) => await service.GetJobs());
            //获取流程的任务，只获取一个，用于测试
            api.MapGet("/getJobByWorkflowId/{workflowId}", async (IJobManageService service, string workflowId) => await service.GetJobByWorkflowId(workflowId));
            //弃用，使用createNewJob统一处理不同的子类（TPH）
            api.MapPost("createJob", async (IJobManageService service, [FromBody] ProcessJob request) => await service.CreateJob(request));
            api.MapPost("createToolJob", async (IJobManageService service, [FromBody] ToolJob request) => await service.CreateToolJob(request));
            api.MapPost("createNewJob", async (IJobManageService service, [FromBody] BaseJob request) => await service.CreateNewJob(request));
            api.MapPut("updateJob", async (IJobManageService service, [FromBody] BaseJob request) => await service.UpdateJob(request));
            api.MapPut("updateToolJob", async (IJobManageService service, [FromBody] ToolJob request) => await service.UpdateToolJob(request));
            api.MapPut("updateProcessJob", async (IJobManageService service, [FromBody] ProcessJob request) => await service.UpdateProcessJob(request));
            api.MapGet("/getToolJobsByParentJob/{ParentJobCorrelationId}", async (IJobManageService service, string ParentJobCorrelationId) => await service.GetToolJobsByParentJob(ParentJobCorrelationId));

            api.MapGet("/getProcessJobByCorrelationId/{CorrelationId}", async (IJobManageService service, string CorrelationId) => await service.GetProcessJobByCorrelationId(CorrelationId));
            api.MapGet("/getJobByCorrelationId/{CorrelationId}", async (IJobManageService service, string CorrelationId) => await service.GetJobByCorrelationId(CorrelationId));
            api.MapGet("/getJobByDefinitionId/{DefinitionId}", async (IJobManageService service, string DefinitionId) => await service.GetJobByDefinitionId(DefinitionId));

            api.MapPost("getJobsByFilter", async (IJobManageService service, [FromBody] JobFilter request) => await service.GetJobsByFilter(request));

            //删除作业
            api.MapDelete("deleteJob/{jobId}", async (IJobManageService service, int jobId) => await service.DeleteJob(jobId));

            //同步作业历程数据
            api.MapGet("/SyncJournalData/{JobCorrelationId}", async (IJobManageService service, string JobCorrelationId) => await service.SyncJournalData(JobCorrelationId));


            return app;
        }

    }
}
