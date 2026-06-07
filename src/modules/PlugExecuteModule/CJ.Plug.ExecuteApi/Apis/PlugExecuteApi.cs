using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Mvc;

public static class PlugExecuteApi
{
    public static IEndpointRouteBuilder MapPlugExecuteApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/plug").WithTags("插头执行");

        api.MapGet("/executePlugByDefinitionId/{DefinitionId}", async (IPlugExecuteService service, string DefinitionId) => await service.ExecutePlug(DefinitionId));

        api.MapPost("/executePlug/{DefinitionId}", async (IPlugExecuteService service, string DefinitionId, PlugExecutionRequest? request) => await service.ExecutePlug(DefinitionId,request));

        //统一使用PlugExecutionRequest进行执行
        api.MapPost("/executePlug", async (IPlugExecuteService service, PlugExecutionRequest? request) => await service.StartExecutePlug(request));

        api.MapPost("/executeMcpTool", async (IPlugExecuteService service, [FromBody] CJ.Plug.Models.MCPTools.McpToolExecutionRequest request) => await service.ExecuteMcpTool(request));

        api.MapGet("/executionStatus/{workflowInstanceId}", async (IPlugExecuteService service, string workflowInstanceId) =>
        {
            var result = await service.GetExecutionStatus(workflowInstanceId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        });

        api.MapPost("/ReportExecuteResult", async (IPlugExecuteService service, [FromBody] ExecuteResultData executeReport) => await service.ReportExecuteResult(executeReport));


        return app;
    }

}

