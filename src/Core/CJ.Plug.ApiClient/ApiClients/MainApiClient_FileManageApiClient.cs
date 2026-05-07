using CJ.Plug.AuditModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.FileManageApiClient;
using Microsoft.AspNetCore.Components.Forms;

public partial class MainApiClient : IFileManageApiClient
{
    public async Task<Stream?> DownloadFileByFileId(string fileId)
    {
        var result = await FileManageApiClient.Value.DownloadFileByFileId(fileId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"下载文件ID: {fileId}");
        return result;
    }

    public async Task<string?> GetFileContent(string filePath)
    {
        var result = await FileManageApiClient.Value.GetFileContent(filePath);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取文件内容: {filePath}");
        return result;
    }

    public async Task<string?> GetFileContentByFileId(string fileId)
    {
        var result = await FileManageApiClient.Value.GetFileContentByFileId(fileId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取文件内容ID: {fileId}");
        return result;
    }

    public async Task<Stream?> GetFileStreamByFileId(string fileId)
    {
        var result = await FileManageApiClient.Value.GetFileStreamByFileId(fileId);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取文件流ID: {fileId}");
        return result;
    }

    public async Task<List<FileSystemNode>?> GetFolderFiles(FileSystemNode? parentNode, string? userName, string? toolAgentHostIp)
    {
        var result = await FileManageApiClient.Value.GetFolderFiles(parentNode, userName, toolAgentHostIp);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "获取文件夹文件列表");
        return result;
    }

    public async Task<List<FileSystemNode>?> GetWorkPathFiles(Plug Plug)
    {
        var result = await FileManageApiClient.Value.GetWorkPathFiles(Plug);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"获取工作路径文件: {Plug.DefinitionId}");
        return result;
    }

    public async Task<string?> UploadFileStreamToWorkPath(Func<Stream, Task> streamWriter, string fileName, Plug plug)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileStreamToWorkPath(streamWriter, fileName, plug);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件: {fileName}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件异常: {fileName}", ex.Message);
            throw;
        }
    }

    public async Task<(string?, string?)> UploadFileStreamToWorkPath(Stream stream, string fileName, Plug Plug)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileStreamToWorkPath(stream, fileName, Plug);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件: {fileName}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件异常: {fileName}", ex.Message);
            throw;
        }
    }

    public async Task<(string?, string?)> UploadFileStreamToWorkPathInChunks(Stream stream, string fileName, PlugData PlugData)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileStreamToWorkPathInChunks(stream, fileName, PlugData);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"分块上传文件: {fileName}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"分块上传文件异常: {fileName}", ex.Message);
            throw;
        }
    }

    public async Task<(string?, string?)> UploadFileToVariable(Stream stream, string fileName, PlugVariableData PlugVariableData)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileToVariable(stream, fileName, PlugVariableData);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件到变量: {fileName}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件到变量异常: {fileName}", ex.Message);
            throw;
        }
    }

    public async Task<string?> UploadFileToWebServer(PlugVariableData stlFileData)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileToWebServer(stlFileData);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, "上传文件到Web服务器");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, "上传文件到Web服务器异常", ex.Message);
            throw;
        }
    }

    public async Task<(string?, string?)> UploadFileToWorkPath(IBrowserFile browserFile, PlugData PlugData)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileToWorkPath(browserFile, PlugData);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件: {browserFile.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件异常: {browserFile.Name}", ex.Message);
            throw;
        }
    }

    public async Task<string?> UploadFileWithOutFileInfo(string filePath)
    {
        try
        {
            var result = await FileManageApiClient.Value.UploadFileWithOutFileInfo(filePath);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件: {filePath}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Create, $"上传文件异常: {filePath}", ex.Message);
            throw;
        }
    }

    public async Task<FileInformation?> DeleteFileWithRequest(FileDeleteRequest fileDeleteRequest)
    {
        try
        {
            var result = await FileManageApiClient.Value.DeleteFileWithRequest(fileDeleteRequest);
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, "删除文件");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Other, AuditOperationType.Delete, "删除文件异常", ex.Message);
            throw;
        }
    }
}
