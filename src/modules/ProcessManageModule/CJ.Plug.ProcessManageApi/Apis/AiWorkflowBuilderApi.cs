using CJ.Plug.ProcessManageApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.ProcessManageApi.Apis;

/// <summary>
/// AI 工作流生成 API — 接收自然语言描述，返回 LLM 生成的工作流定义
/// </summary>
public static class AiWorkflowBuilderApi
{
    public static IEndpointRouteBuilder MapAiWorkflowBuilderApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/process/ai").WithTags("AI工作流生成");

        api.MapPost("/generate", async (
            AiWorkflowBuilderService service,
            [FromBody] AiWorkflowRequest request,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return Results.BadRequest("Prompt is required");

            var result = await service.GenerateAsync(request.Prompt, ct);

            // 验证
            var errors = service.Validate(result);
            result.ValidationErrors = errors;

            return Results.Ok(result);
        });

        api.MapPost("/validate", (
            AiWorkflowBuilderService service,
            [FromBody] WorkflowGenerationResult result) =>
        {
            var errors = service.Validate(result);
            return Results.Ok(new { isValid = errors.Count == 0, errors });
        });

        api.MapPost("/save", async (
            WorkflowGenerationResult result,
            WorkflowTranslator translator,
            IProcessManageService processService,
            HttpContext httpContext) =>
        {
            // 获取当前用户名
            var userName = httpContext.User?.Identity?.Name ?? "AI";

            // 验证
            var errors = translator.ValidateForTranslation(result);
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

            // 翻译并保存
            var (process, entryVariables) = translator.Translate(result, userName);

            try
            {
                var saved = await processService.CreateWorkflowAsync(process);
                return Results.Ok(new
                {
                    id = saved.Id,
                    definitionId = saved.DefinitionId,
                    name = saved.Name,
                    variables = entryVariables.Count,
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"保存失败: {ex.Message}");
            }
        });

        return app;
    }
}

/// <summary>
/// AI 工作流生成请求
/// </summary>
public record AiWorkflowRequest(string Prompt);
