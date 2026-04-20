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
    }
}
