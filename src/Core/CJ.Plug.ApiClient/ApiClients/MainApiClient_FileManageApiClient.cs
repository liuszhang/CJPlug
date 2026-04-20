//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.ApiClient.Contracts;
using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.Models.Relation;
using Microsoft.Extensions.DependencyInjection;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.TASApiClient;
using CJ.Plug.StationAndToolApiClient;
using CJ.Plug.ProcessManageApiClient;
using Microsoft.AspNetCore.Components.Forms;

public partial class MainApiClient : IFileManageApiClient
{
    public async Task<Stream?> DownloadFileByFileId(string fileId) => await FileManageApiClient.Value.DownloadFileByFileId(fileId);
    public async Task<string?> GetFileContent(string filePath) => await FileManageApiClient.Value.GetFileContent(filePath);
    public async Task<string?> GetFileContentByFileId(string fileId) => await FileManageApiClient.Value.GetFileContentByFileId(fileId);
    public async Task<Stream?> GetFileStreamByFileId(string fileId) => await FileManageApiClient.Value.GetFileStreamByFileId(fileId);
    public async Task<List<FileSystemNode>?> GetFolderFiles(FileSystemNode? parentNode, string? userName, string? toolAgentHostIp) => await FileManageApiClient.Value.GetFolderFiles(parentNode, userName, toolAgentHostIp);
    public async Task<List<FileSystemNode>?> GetWorkPathFiles(Plug Plug) => await FileManageApiClient.Value.GetWorkPathFiles(Plug);
    public async Task<string?> UploadFileStreamToWorkPath(Func<Stream, Task> streamWriter, string fileName, Plug plug) => await FileManageApiClient.Value.UploadFileStreamToWorkPath(streamWriter, fileName, plug);
    public async Task<(string?, string?)> UploadFileStreamToWorkPath(Stream stream, string fileName, Plug Plug) => await FileManageApiClient.Value.UploadFileStreamToWorkPath(stream,fileName, Plug);
    public async Task<(string?, string?)> UploadFileStreamToWorkPathInChunks(Stream stream, string fileName, PlugData PlugData) => await FileManageApiClient.Value.UploadFileStreamToWorkPathInChunks(stream,fileName, PlugData);
    public async Task<(string?, string?)> UploadFileToVariable(Stream stream, string fileName, PlugVariableData PlugVariableData) => await FileManageApiClient.Value.UploadFileToVariable(stream,fileName, PlugVariableData);
    public async Task<string?> UploadFileToWebServer(PlugVariableData stlFileData) => await FileManageApiClient.Value.UploadFileToWebServer(stlFileData);
    public async Task<(string?, string?)> UploadFileToWorkPath(IBrowserFile browserFile, PlugData PlugData) => await FileManageApiClient.Value.UploadFileToWorkPath(browserFile, PlugData);
    public async Task<string?> UploadFileWithOutFileInfo(string filePath) => await FileManageApiClient.Value.UploadFileWithOutFileInfo(filePath);
    public async Task<FileInformation?> DeleteFileWithRequest(FileDeleteRequest fileDeleteRequest) => await FileManageApiClient.Value.DeleteFileWithRequest(fileDeleteRequest);
}




