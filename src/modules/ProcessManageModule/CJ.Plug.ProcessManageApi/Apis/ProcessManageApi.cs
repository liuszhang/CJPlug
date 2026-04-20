using CJ.Plug.Models.Job;
using CJ.Plug.Models.PlugProcess;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

public static class ProcessManageApi
{
    
    public static IEndpointRouteBuilder MapProcessManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/process").WithTags("流程管理");

        api.MapGet("/getWorkflows", async (IProcessManageService service) => await service.GetAllWorkflowsAsync());
        api.MapGet("/getWorkflow/{Id}", async (IProcessManageService service, int Id) => await service.GetWorkflowById(Id));
        api.MapGet("/getWorkflowJson/{Id}", async (IProcessManageService service, int Id) => await service.GetWorkflowJsonAsync(Id));
        api.MapPost("/createWorkflow", async (IProcessManageService service, [FromBody] Process request) => await service.CreateWorkflowAsync(request));
        api.MapDelete("/deleteWorkflow/{id}", async (IProcessManageService service, int id) => await service.DeleteAsync(id));
        api.MapPut("/updateWorkflow/{id}", async (IProcessManageService service, int id, [FromBody] Process request) => await service.UpdateAsync(request)); // 编辑 API



        return app;
    }
}

