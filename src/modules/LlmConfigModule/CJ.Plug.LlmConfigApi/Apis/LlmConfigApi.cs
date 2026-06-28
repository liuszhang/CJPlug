using CJ.Plug.LlmConfigApi.Contracts;
using CJ.Plug.LlmConfigModel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.LlmConfigApi.Apis;

public static class LlmConfigApi
{
    public static IEndpointRouteBuilder MapLlmConfigApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/llm").WithTags("LLM 配置管理");

        // ---- 供应商 CRUD ----
        api.MapGet("/getAllProviders", async (
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.GetAllProvidersAsync(ct))
        .WithName("GetAllProviders")
        .WithDescription("获取所有 LLM 供应商");

        api.MapGet("/getProviderById/{id}", async (
            int id,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var result = await service.GetProviderByIdAsync(id, ct);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetProviderById")
        .WithDescription("根据 ID 获取供应商");

        api.MapPost("/createProvider", async (
            [FromBody] LlmProvider provider,
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.CreateProviderAsync(provider, ct))
        .WithName("CreateProvider")
        .WithDescription("创建供应商");

        api.MapPut("/updateProvider", async (
            [FromBody] LlmProvider provider,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateProviderAsync(provider, ct);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateProvider")
        .WithDescription("更新供应商");

        api.MapDelete("/deleteProvider/{id}", async (
            int id,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var success = await service.DeleteProviderAsync(id, ct);
            return success ? Results.Ok(true) : Results.NotFound();
        })
        .WithName("DeleteProvider")
        .WithDescription("删除供应商");

        // ---- 模型配置 CRUD ----
        api.MapGet("/getAllModelConfigs", async (
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.GetAllModelConfigsAsync(ct))
        .WithName("GetAllModelConfigs")
        .WithDescription("获取所有模型配置");

        api.MapGet("/getModelConfigsByProvider/{providerId}", async (
            int providerId,
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.GetModelConfigsByProviderAsync(providerId, ct))
        .WithName("GetModelConfigsByProvider")
        .WithDescription("按供应商获取模型配置");

        api.MapGet("/getModelConfigById/{id}", async (
            int id,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var result = await service.GetModelConfigByIdAsync(id, ct);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetModelConfigById")
        .WithDescription("根据 ID 获取模型配置");

        api.MapPost("/createModelConfig", async (
            [FromBody] LlmModelConfig config,
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.CreateModelConfigAsync(config, ct))
        .WithName("CreateModelConfig")
        .WithDescription("创建模型配置");

        api.MapPut("/updateModelConfig", async (
            [FromBody] LlmModelConfig config,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateModelConfigAsync(config, ct);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateModelConfig")
        .WithDescription("更新模型配置");

        api.MapDelete("/deleteModelConfig/{id}", async (
            int id,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var success = await service.DeleteModelConfigAsync(id, ct);
            return success ? Results.Ok(true) : Results.NotFound();
        })
        .WithName("DeleteModelConfig")
        .WithDescription("删除模型配置");

        // ---- 获取默认模型信息 ----
        api.MapGet("/getDefaultModelInfo", async (
            ILlmConfigService service,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("LlmConfigApi");
            var (provider, model) = await service.GetDefaultModelInfoAsync(ct);
            if (provider == null || model == null)
            {
                logger.LogWarning("GetDefaultModelInfo: no default model found");
                // 返回结构化空结果，避免客户端 JSON 反序列化崩溃
                return Results.Ok(new ApiResult<DefaultModelInfoResponse?>(0, null, "no default model found"));
            }
            logger.LogInformation("GetDefaultModelInfo: ProviderName={Name}, ApiBaseUrl={Url}, ModelName={Model}, ApiKeyLen={KeyLen}",
                provider.Name, provider.ApiBaseUrl, model.ModelName, provider.ApiKey?.Length ?? 0);
            return Results.Ok(new ApiResult<DefaultModelInfoResponse?>(0, new DefaultModelInfoResponse(
                provider.Id, provider.Name, provider.DisplayName,
                provider.ApiBaseUrl, provider.ApiKey,
                model.Id, model.ModelName, model.DisplayName,
                model.ModelType, model.MaxTokens, model.Temperature)));
        })
        .WithName("GetDefaultModelInfo")
        .WithDescription("获取当前默认模型及供应商信息");

        // ---- 设置默认模型 ----
        api.MapPost("/setDefaultModel/{modelConfigId}", async (
            int modelConfigId,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var success = await service.SetDefaultModelAsync(modelConfigId, ct);
            return success
                ? Results.Ok(new ApiResult<bool>(0, true))
                : Results.Ok(new ApiResult<bool>(1, false, "model config not found"));
        })
        .WithName("SetDefaultModel")
        .WithDescription("设置指定模型为默认模型");

        // ---- 测试连接 ----
        api.MapPost("/testConnection/{modelConfigId}", async (
            int modelConfigId,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var (success, message) = await service.TestConnectionAsync(modelConfigId, ct);
            return Results.Ok(new { success, message });
        })
        .WithName("TestLlmConnection")
        .WithDescription("测试 LLM 连接");

        // ---- MCP Server 配置 ----
        api.MapGet("/getMcpServerConfigs", async (
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var configs = await service.GetMcpServerConfigsAsync(ct);
            return Results.Ok(configs);
        })
        .WithName("GetMcpServerConfigs")
        .WithDescription("获取所有 MCP Server 配置");

        api.MapPost("/createMcpServerConfig", async (
            [FromBody] McpServerConfig config,
            ILlmConfigService service,
            CancellationToken ct) =>
            await service.CreateMcpServerConfigAsync(config, ct))
        .WithName("CreateMcpServerConfig")
        .WithDescription("创建 MCP Server 配置");

        api.MapPut("/updateMcpServerConfig", async (
            [FromBody] McpServerConfig config,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateMcpServerConfigAsync(config, ct);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateMcpServerConfig")
        .WithDescription("更新 MCP Server 配置");

        api.MapDelete("/deleteMcpServerConfig/{id}", async (
            int id,
            ILlmConfigService service,
            CancellationToken ct) =>
        {
            var success = await service.DeleteMcpServerConfigAsync(id, ct);
            return success ? Results.Ok(true) : Results.NotFound();
        })
        .WithName("DeleteMcpServerConfig")
        .WithDescription("删除 MCP Server 配置");

        return app;
    }
}

public record DefaultModelInfoResponse(
    int ProviderId, string ProviderName, string ProviderDisplayName,
    string ApiBaseUrl, string? ApiKey,
    int ModelId, string ModelName, string ModelDisplayName,
    string ModelType, int? MaxTokens, double? Temperature);

/// <summary>通用 API 响应包装，确保空结果也返回合法 JSON。</summary>
public record ApiResult<T>(int Code, T? Data, string? Message = null);
