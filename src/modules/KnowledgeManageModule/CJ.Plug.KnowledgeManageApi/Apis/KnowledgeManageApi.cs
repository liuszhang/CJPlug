using CJ.Plug.Models.Knowledge;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.KnowledgeManageApi;

/// <summary>
/// 知识管理 Minimal API 路由定义
/// </summary>
public static class KnowledgeManageApi
{
    /// <summary>
    /// 注册知识管理 API 路由，路由前缀 api/knowledge
    /// </summary>
    public static IEndpointRouteBuilder MapKnowledgeManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/knowledge").WithTags("知识管理");

        // ===== 知识库 CRUD =====

        // 获取所有知识库
        api.MapGet("/bases", async (IKnowledgeManageService service, CancellationToken ct) =>
            await service.GetAllBasesAsync(ct));

        // 根据 ID 获取知识库
        api.MapGet("/bases/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var kb = await service.GetBaseByIdAsync(id, ct);
            return kb is not null ? Results.Ok(kb) : Results.NotFound();
        });

        // 创建知识库
        api.MapPost("/bases", async ([FromBody] KnowledgeBase kb, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var created = await service.CreateBaseAsync(kb, ct);
            return Results.Created($"/api/knowledge/bases/{created.Id}", created);
        });

        // 更新知识库
        api.MapPut("/bases/{id:int}", async (int id, [FromBody] KnowledgeBase kb, IKnowledgeManageService service, CancellationToken ct) =>
        {
            kb.Id = id;
            var updated = await service.UpdateBaseAsync(kb, ct);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        });

        // 删除知识库（级联删除其下所有文件夹和条目）
        api.MapDelete("/bases/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteBaseAsync(id, ct);
            return deleted ? Results.Ok() : Results.NotFound();
        });

        // ===== 文件夹 CRUD =====

        // 根据知识库ID获取其下所有文件夹
        api.MapGet("/folders", async (int baseId, IKnowledgeManageService service, CancellationToken ct) =>
            await service.GetFoldersByBaseAsync(baseId, ct));

        // 根据 ID 获取文件夹
        api.MapGet("/folders/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var folder = await service.GetFolderByIdAsync(id, ct);
            return folder is not null ? Results.Ok(folder) : Results.NotFound();
        });

        // 创建文件夹
        api.MapPost("/folders", async ([FromBody] KnowledgeFolder folder, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var created = await service.CreateFolderAsync(folder, ct);
            return Results.Created($"/api/knowledge/folders/{created.Id}", created);
        });

        // 更新文件夹
        api.MapPut("/folders/{id:int}", async (int id, [FromBody] KnowledgeFolder folder, IKnowledgeManageService service, CancellationToken ct) =>
        {
            folder.Id = id;
            var updated = await service.UpdateFolderAsync(folder, ct);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        });

        // 删除文件夹（含级联删除）
        api.MapDelete("/folders/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteFolderAsync(id, ct);
            return deleted ? Results.Ok() : Results.NotFound();
        });

        // ===== 知识条目 CRUD =====

        // 根据文件夹ID获取条目列表（folderId 为 null 时返回所有）
        api.MapGet("/items", async (int? folderId, IKnowledgeManageService service, CancellationToken ct) =>
            await service.GetItemsByFolderAsync(folderId, ct));

        // 根据知识库ID获取所有条目
        api.MapGet("/items/byBase/{baseId:int}", async (int baseId, IKnowledgeManageService service, CancellationToken ct) =>
            await service.GetItemsByBaseAsync(baseId, ct));

        // 根据 ID 获取条目
        api.MapGet("/items/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var item = await service.GetItemByIdAsync(id, ct);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        });

        // 创建知识条目
        api.MapPost("/items", async ([FromBody] KnowledgeItem item, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var created = await service.CreateItemAsync(item, ct);
            return Results.Created($"/api/knowledge/items/{created.Id}", created);
        });

        // 更新知识条目
        api.MapPut("/items/{id:int}", async (int id, [FromBody] KnowledgeItem item, IKnowledgeManageService service, CancellationToken ct) =>
        {
            item.Id = id;
            var updated = await service.UpdateItemAsync(item, ct);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        });

        // 删除知识条目
        api.MapDelete("/items/{id:int}", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteItemAsync(id, ct);
            return deleted ? Results.Ok() : Results.NotFound();
        });

        // ===== 搜索 =====

        // 搜索知识条目（支持关键字、标签和知识库范围过滤）
        api.MapGet("/items/search", async (string? keyword, string? tag, int? baseId, IKnowledgeManageService service, CancellationToken ct) =>
            await service.SearchItemsAsync(keyword, tag, baseId, ct));

        // ===== 辅助 =====

        // 检查文件夹是否有子内容
        api.MapGet("/folders/{id:int}/hasContent", async (int id, IKnowledgeManageService service, CancellationToken ct) =>
            await service.HasChildContentAsync(id, ct));

        return app;
    }
}
