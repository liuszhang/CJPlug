using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace CJ.Plug.FileManageApiClient
{
    public interface IFileManageApiClient
    {
        Task<Stream?> DownloadFileByFileId(string fileId);
        Task<string?> GetFileContent(string filePath);
        Task<string?> GetFileContentByFileId(string fileId);
        Task<Stream?> GetFileStreamByFileId(string fileId);
        Task<Stream?> DownloadFileByPath(string filePath);
        Task<List<FileSystemNode>?> GetFolderFiles(FileSystemNode? parentNode, string? userName, string? toolAgentHostIp);
        Task<List<FileSystemNode>?> GetWorkPathFiles(Models.Plug.Plug Plug);
        Task<string?> UploadFileStreamToWorkPath(Func<Stream, Task> streamWriter, string fileName, Models.Plug.Plug plug);
        Task<(string?, string?)> UploadFileStreamToWorkPath(Stream stream, string fileName, Models.Plug.Plug Plug);
        Task<(string?, string?)> UploadFileStreamToWorkPathInChunks(Stream stream, string fileName, PlugData PlugData);
        Task<(string?, string?)> UploadFileToVariable(Stream stream, string fileName, PlugVariableData PlugVariableData);
        Task<string?> UploadFileToWebServer(PlugVariableData stlFileData);
        Task<(string?, string?)> UploadFileToWorkPath(IBrowserFile browserFile, PlugData PlugData);
        Task<string?> UploadFileWithOutFileInfo(string filePath);

        Task<FileInformation?> DeleteFileWithRequest(FileDeleteRequest fileDeleteRequest);

        Task<bool> MoveDirectory(string sourcePath, string destPath);

        /// <summary>
        /// 从 base64 编码内容上传文件，返回 "fileName:fileId" 格式引用字符串
        /// </summary>
        Task<string?> UploadFileFromBase64(string fileContent, string fileName);

        /// <summary>
        /// 从远程 URL 下载文件，返回 "fileName:fileId" 格式引用字符串
        /// </summary>
        Task<string?> UploadFileFromUrl(string url, string? fileName);

        /// <summary>
        /// 按文件名搜索已有文件
        /// </summary>
        Task<List<FileInformation>?> SearchFiles(string? keyword);
    }
}
