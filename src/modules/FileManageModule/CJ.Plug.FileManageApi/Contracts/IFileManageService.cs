
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace CJ.Plug.FileManageApi.Contracts
{
    public interface IFileManageService
    {
        Task<string?> UploadFileStreamToServerPath(Stream? FileStream, string? UploadPath, string? FileName);
        Task<string?> UploadFileToWebServer(PlugVariableData? FileId);
        Task<FileSystemNode?> GetFolderFiles(string? RootPath);
        Task<string?> UploadFileRequestToServer(FileUploadRequest? fur);
        Task<IResult> DownloadFileByFileId(string fileId);
        Task<List<FileInformation>?> GetAllFileInformations();
        Task<FileInformation?> GetFileInformationById(string fileId);

        Task<IResult> GetFileContent(string filePath);
        Task<IResult> GetFileContentByFileId(string fileId);
        Task<IResult> GetFileStreamByFileId(string fileId);
        Task<IResult> DownloadFileByPath(string filePath);

        Task<FileSystemNode?> GetPlugWorkpathFiles(string? RootPath);


        Task<IActionResult> UploadChunk(FileUploadRequest? request);
        Task<IActionResult> CompleteUpload(FileUploadRequest? request);

        Task<FileInformation?> DeleteFile(FileDeleteRequest fileDeleteRequest);

        Task<(Stream? fileStream, string? fileName, string? errorMessage)> DownloadToolAsync(string toolName, string toolVersion, string toolPath);
    }
}
