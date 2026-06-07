using CJ.Plug.Models.Skills;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace CJ.Plug.SkillsManageApi;

public static class SkillsManageApi
{
    public static IEndpointRouteBuilder MapSkillsManageApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/skills").WithTags("Skills管理");

        api.MapGet("/getSkills", async (ISkillsManageService service) =>
            await service.GetAllAsync());

        api.MapGet("/getActiveSkills", async (ISkillsManageService service) =>
            await service.GetActiveSkillsAsync());

        api.MapPost("/addSkill", async ([FromBody] Skill skill, ISkillsManageService service) =>
            await service.CreateAsync(skill));

        api.MapPut("/updateSkill", async ([FromBody] Skill skill, ISkillsManageService service) =>
            await service.UpdateAsync(skill));

        api.MapDelete("/deleteSkill/{skillId:int}", async (int skillId, ISkillsManageService service) =>
            await service.DeleteAsync(skillId));

        // 获取技能关联文件列表
        api.MapGet("/{skillId:int}/files", async (int skillId, ISkillsManageService service) =>
        {
            var files = await service.GetSkillFilesAsync(skillId);
            return Results.Ok(files);
        });

        // 下载单个技能文件
        api.MapGet("/{skillId:int}/files/download/{**fileName}", async (int skillId, string fileName, ISkillsManageService service) =>
        {
            var decodedFileName = Uri.UnescapeDataString(fileName);
            var (fileStream, name, contentType) = await service.DownloadSkillFileAsync(skillId, decodedFileName);
            if (fileStream == null)
                return Results.NotFound($"文件 '{decodedFileName}' 不存在");

            return Results.File(fileStream, contentType ?? "application/octet-stream", name ?? Path.GetFileName(decodedFileName));
        });

        // 打包下载技能所有文件
        api.MapGet("/{skillId:int}/files/archive", async (int skillId, ISkillsManageService service) =>
        {
            var (fileStream, zipFileName) = await service.DownloadSkillFilesArchiveAsync(skillId);
            if (fileStream == null)
                return Results.NotFound("没有可下载的文件");

            return Results.File(fileStream, "application/zip", zipFileName ?? $"skill_{skillId}.zip");
        });

        // 上传文件到技能目录
        api.MapPost("/{skillId:int}/files/upload", async (int skillId, HttpRequest request, ISkillsManageService service) =>
        {
            var form = await request.ReadFormAsync();
            var files = form.Files.Where(f => f.Length > 0).ToList();

            if (files.Count == 0)
                return Results.BadRequest("未选择任何文件");

            await service.UploadSkillFilesAsync(skillId, files);
            return Results.Ok(new { uploaded = files.Count });
        }).DisableAntiforgery();

        // 删除单个技能文件
        api.MapDelete("/{skillId:int}/files/{**fileName}", async (int skillId, string fileName, ISkillsManageService service) =>
        {
            var decodedFileName = Uri.UnescapeDataString(fileName);
            var success = await service.DeleteSkillFileAsync(skillId, decodedFileName);

            if (success)
                return Results.Ok(new { deleted = true, fileName = decodedFileName });
            else
                return Results.NotFound($"文件 '{decodedFileName}' 不存在或删除失败");
        });

        // 打包下载技能指定子文件夹
        api.MapGet("/{skillId:int}/files/folder-archive/{**folderPath}", async (int skillId, string folderPath, ISkillsManageService service) =>
        {
            var decodedFolderPath = Uri.UnescapeDataString(folderPath);
            var (fileStream, zipFileName) = await service.DownloadSkillFolderArchiveAsync(skillId, decodedFolderPath);
            if (fileStream == null)
                return Results.NotFound($"文件夹 '{decodedFolderPath}' 不存在或为空");

            return Results.File(fileStream, "application/zip", zipFileName ?? $"folder_{skillId}.zip");
        });

        // 递归删除技能指定子文件夹
        api.MapDelete("/{skillId:int}/files/folder/{**folderPath}", async (int skillId, string folderPath, ISkillsManageService service) =>
        {
            var decodedFolderPath = Uri.UnescapeDataString(folderPath);
            var success = await service.DeleteSkillFolderAsync(skillId, decodedFolderPath);

            if (success)
                return Results.Ok(new { deleted = true, folderPath = decodedFolderPath });
            else
                return Results.NotFound($"文件夹 '{decodedFolderPath}' 不存在或删除失败");
        });

        // 获取技能描述文件内容（{skillName}.md）
        api.MapGet("/{skillId:int}/readme", async (int skillId, ISkillsManageService service) =>
        {
            var content = await service.GetSkillReadmeAsync(skillId);
            if (content == null)
                return Results.NotFound();

            return Results.Ok(new { content });
        });

        // 创建技能描述文件（{skillName}.md）
        api.MapPost("/{skillId:int}/readme", async (int skillId, ISkillsManageService service, [FromBody] CreateReadmeRequest? request) =>
        {
            var content = await service.CreateSkillReadmeAsync(skillId, request?.Content);
            if (content == null)
                return Results.BadRequest("创建描述文件失败");

            return Results.Ok(new { content });
        }).DisableAntiforgery();

        // 导入 Skill（支持 zip 文件上传或 URL 下载）
        api.MapPost("/import", async (HttpRequest request, ISkillsManageService service) =>
        {
            var contentType = request.ContentType ?? "";

            if (contentType.Contains("multipart/form-data"))
            {
                // 从上传的 zip 文件导入
                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault(f => f.Length > 0 && f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (file == null)
                    return Results.BadRequest("请上传 .zip 文件");

                await using var stream = file.OpenReadStream();
                var skill = await service.ImportSkillFromZipAsync(stream);
                return Results.Ok(skill);
            }
            else if (contentType.Contains("application/json"))
            {
                // 从 URL 下载 zip 后导入
                var body = await request.ReadFromJsonAsync<ImportFromUrlRequest>();
                if (body == null || string.IsNullOrWhiteSpace(body.Url))
                    return Results.BadRequest("请提供有效的 URL");

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var response = await httpClient.GetAsync(body.Url);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                var skill = await service.ImportSkillFromZipAsync(stream);
                return Results.Ok(skill);
            }

            return Results.BadRequest("请使用 multipart/form-data 上传文件或 application/json 提供 URL");
        }).DisableAntiforgery();

        return app;
    }
}

public record CreateReadmeRequest(string? Content);
public record ImportFromUrlRequest(string Url);