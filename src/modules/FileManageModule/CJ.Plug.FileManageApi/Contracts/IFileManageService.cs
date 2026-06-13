
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

        Task<bool> MoveDirectory(string sourcePath, string destPath);

        Task<(Stream? fileStream, string? fileName, string? errorMessage)> DownloadToolAsync(string toolName, string toolVersion, string toolPath);

        /// <summary>
        /// 从 base64 编码内容上传文件并创建 FileInformation 记录，返回 "fileName:fileId" 格式引用字符串
        /// </summary>
        Task<string?> UploadFileFromBase64(string fileContent, string fileName);

        /// <summary>
        /// 从远程 URL 下载文件并创建 FileInformation 记录，返回 "fileName:fileId" 格式引用字符串
        /// </summary>
        Task<string?> UploadFileFromUrl(string url, string? fileName);

        /// <summary>
        /// 按文件名搜索已有文件，最多返回 100 条
        /// </summary>
        Task<List<FileInformation>?> SearchFiles(string? keyword);
    }
}
