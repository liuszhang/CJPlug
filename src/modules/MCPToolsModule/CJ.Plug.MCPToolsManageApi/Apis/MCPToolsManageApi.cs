using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.Models.MCPTools;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.MCPToolsManageApi.Apis
{
    public static class MCPToolsManageApi
    {
        public static IEndpointRouteBuilder MapMCPToolsManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/mcptools").WithTags("MCPTools管理");

            api.MapGet("/getTools", async (IMCPToolsManageService service) => await service.GetAllAsync());
            api.MapGet("/getActiveTools", async (IMCPToolsManageService service) => await service.GetActiveToolsAsync());
            api.MapPost("/addTool", async (IMCPToolsManageService service, [FromBody] MCPTool request) => await service.CreateAsync(request));
            api.MapPut("/updateTool", async (IMCPToolsManageService service, [FromBody] MCPTool request) => await service.UpdateAsync(request));
            api.MapDelete("/deleteTool/{toolId}", async (IMCPToolsManageService service, int toolId) => await service.DeleteAsync(toolId));
            ////获取流程的任务，只获取一个，用于测试
            //api.MapGet("/getJobByWorkflowId/{workflowId}", async (IJobManageService service, string workflowId) => await service.GetJobByWorkflowId(workflowId));
            ////弃用，使用createNewJob统一处理不同的子类（TPH）
            //api.MapPost("createJob", async (IJobManageService service, [FromBody] ProcessJob request) => await service.CreateJob(request));
            //api.MapPost("createToolJob", async (IJobManageService service, [FromBody] ToolJob request) => await service.CreateToolJob(request));
            //api.MapPost("createNewJob", async (IJobManageService service, [FromBody] BaseJob request) => await service.CreateNewJob(request));
            //api.MapPut("updateJob", async (IJobManageService service, [FromBody] BaseJob request) => await service.UpdateJob(request));
            //api.MapPut("updateToolJob", async (IJobManageService service, [FromBody] ToolJob request) => await service.UpdateToolJob(request));
            //api.MapPut("updateProcessJob", async (IJobManageService service, [FromBody] ProcessJob request) => await service.UpdateProcessJob(request));
            //api.MapGet("/getToolJobsByParentJob/{ParentJobCorrelationId}", async (IJobManageService service, string ParentJobCorrelationId) => await service.GetToolJobsByParentJob(ParentJobCorrelationId));

            //api.MapGet("/getProcessJobByCorrelationId/{CorrelationId}", async (IJobManageService service, string CorrelationId) => await service.GetProcessJobByCorrelationId(CorrelationId));
            //api.MapGet("/getJobByCorrelationId/{CorrelationId}", async (IJobManageService service, string CorrelationId) => await service.GetJobByCorrelationId(CorrelationId));
            //api.MapGet("/getJobByDefinitionId/{DefinitionId}", async (IJobManageService service, string DefinitionId) => await service.GetJobByDefinitionId(DefinitionId));

            //api.MapPost("getJobsByFilter", async (IJobManageService service, [FromBody] JobFilter request) => await service.GetJobsByFilter(request));

            ////删除作业
            //api.MapDelete("deleteJob/{jobId}", async (IJobManageService service, int jobId) => await service.DeleteJob(jobId));

            ////同步作业历程数据
            //api.MapGet("/SyncJournalData/{JobCorrelationId}", async (IJobManageService service, string JobCorrelationId) => await service.SyncJournalData(JobCorrelationId));


            return app;
        }
    }
}
