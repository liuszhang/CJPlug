using CJ.Plug.FileManageApi.Contracts;

using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Mvc;
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

            return app;
        }

    }
}
