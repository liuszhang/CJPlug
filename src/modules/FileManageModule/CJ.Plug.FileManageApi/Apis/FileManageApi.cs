using CJ.Plug.FileManageApi.Contracts;

using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.FileManageApi.Apis
{
    public static class FileManageApi
    {
        public static IEndpointRouteBuilder MapFileManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/file").WithTags("文件管理");

            //路由定义
            //[IgnoreAntiforgeryToken]// 文件上传 API
            api.MapPost("/uploadWithOutInfo", async (IFileManageService service, [FromForm] FileUploadRequest? request) =>
            {
                try
                {
                    if (request == null)
                    {
                        Console.WriteLine("request is null");
                        return Results.BadRequest();
                    }
                    Console.WriteLine(request.FileStream?.Length.ToString());
                    Console.WriteLine(request.UploadPath);
                    Console.WriteLine(request.FileName);
                    var result = await service.UploadFileStreamToServerPath(request.FileStream?.OpenReadStream(), request.UploadPath, request.FileName);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    // 记录异常日志
                    Console.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            }).DisableAntiforgery();
            api.MapPost("/uploadFileToWebServer", async (IFileManageService service, [FromBody] PlugVariableData? request) => await service.UploadFileToWebServer(request));

            api.MapPost("/upload", async (IFileManageService service, [FromForm] FileUploadRequest? request) =>
            {
                try
                {
                    if (request == null)
                    {
                        Console.WriteLine("request is null");
                        return Results.BadRequest();
                    }
                    Log.Information("API开始处理文件上传");
                    var result = await service.UploadFileRequestToServer(request);
                    //还需要在数据库添加一条文件信息数据，便于后续下载

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    // 记录异常日志
                    Console.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            }).DisableAntiforgery();

            api.MapGet("/GetFolderFiles/{*rootPath}", async (IFileManageService service, string rootPath) => await service.GetFolderFiles(rootPath));

            api.MapGet("/DownloadFileByFileId/{fileId}", async (IFileManageService service, string fileId) => await service.DownloadFileByFileId(fileId));

            api.MapGet("/GetFileInformations", async (IFileManageService service) => await service.GetAllFileInformations());

            api.MapPost("/getContent", async (IFileManageService service, string filePath) => await service.GetFileContent(filePath));
            api.MapGet("/getContentByFileId/{fileId}", async (IFileManageService service, string fileId) => await service.GetFileContentByFileId(fileId));
            api.MapGet("/getStreamByFileId/{fileId}", async (IFileManageService service, string fileId) => await service.GetFileStreamByFileId(fileId));
            api.MapGet("/download/{**filePath}", async (IFileManageService service, string filePath) => await service.DownloadFileByPath(filePath));


            api.MapGet("/GetPlugWorkpathFiles/{plugDefinitionId}", async (IFileManageService service, string plugDefinitionId) => await service.GetPlugWorkpathFiles(plugDefinitionId));


            api.MapPost("/uploadchunk", async (IFileManageService service, [FromForm] FileUploadRequest? request) => await service.UploadChunk(request)).DisableAntiforgery();
            api.MapPost("/completeupload", async (IFileManageService service, [FromForm] FileUploadRequest? request) => await service.CompleteUpload(request)).DisableAntiforgery();

            api.MapPost("/delete", async (IFileManageService service, [FromBody] FileDeleteRequest? request) => await service.DeleteFile(request));

            // 以 zip 方式打包下载工具
            api.MapGet("/downloadTool", async (string name, string version, string? path) =>
            {
                if (string.IsNullOrEmpty(name))
                    return Results.BadRequest("工具名称不能为空");

                using var scope = app.ServiceProvider.CreateScope();
                var fileManageService = scope.ServiceProvider.GetRequiredService<IFileManageService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

                // 如果 path 为空，从工具表中查询 ToolBasePath（工具完整包目录）
                string toolPath = path ?? "";
                if (string.IsNullOrEmpty(toolPath))
                {
                    var tool = await dbContext.Set<Tool>().FirstOrDefaultAsync(t => t.ToolName == name && t.ToolVersion == version);
                    if (tool == null)
                        return Results.BadRequest($"未找到工具: {name} (版本: {version})");
                    
                    // 优先使用 ToolBasePath（工具包根目录），否则用 ToolPath 的父目录
                    toolPath = !string.IsNullOrEmpty(tool.ToolBasePath) 
                        ? tool.ToolBasePath 
                        : (tool.ToolPath != null ? Path.GetDirectoryName(tool.ToolPath)?.Replace('\\', '/') : null) ?? "";
                    
                    if (string.IsNullOrEmpty(toolPath))
                        return Results.BadRequest($"工具 {name} 的路径为空，无法下载");
                }

                var (fileStream, fileName, errorMessage) = await fileManageService.DownloadToolAsync(name, version ?? "latest", toolPath);

                if (fileStream == null)
                    return Results.BadRequest(errorMessage ?? "下载失败");

                return Results.File(fileStream, "application/zip", fileName);
            });

            return app;
        }

    }
}
