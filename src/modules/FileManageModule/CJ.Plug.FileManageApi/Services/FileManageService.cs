using CJ.Plug.FileManageApi.Contracts;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;

namespace CJ.Plug.FileManageApi.Services
{
    public class FileManageService : IFileManageService
    {
        private readonly MainDbContext _dbContext;

        public FileManageService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbContext.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// 上传文件数据至服务器指定路径,不创建文件信息，用于临时使用，如可视化文件等
        /// </summary>
        /// <param name="FileStream"></param>
        /// <param name="plug"></param>
        /// <returns></returns>
        public async Task<string?> UploadFileStreamToServerPath(Stream? FileStream, string? uploadPath, string? fileName)
        {
            try
            {
                //Log.Information(uploadPath);
                //Log.Information(fileName);
                //Log.Information(FileStream.Length.ToString());
                // 构建完整的文件路径
                var filePathInfo = new FileInfo(Path.Combine(GlobalData.MainWebFileServer, uploadPath, fileName));
                string filePath = filePathInfo.FullName;
                // 确保文件的上级目录存在，如果不存在则创建
                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    //Console.WriteLine("1");
                    await FileStream.CopyToAsync(stream);
                    //Console.WriteLine("2");
                }
                return "/webFiles/" + uploadPath + "/" + filePathInfo.Name;
            }
            catch (HttpRequestException ex)
            {
                Log.Information($"An error1(UploadFileStreamToServerPath) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error1(UploadFileStreamToServerPath) occurred while fetching data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                Log.Information($"An error2(UploadFileStreamToServerPath) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error2(UploadFileStreamToServerPath) occurred while fetching data: {ex.Message}");
                return null;
            }
        }


        //通过文件ID获取文件并复制到WEB服务器路径中
        public async Task<string?> UploadFileToWebServer(PlugVariableData? fileData)
        {
            try
            {
                var fileId = fileData?.Value?.GetFileIdFromFileVariable();
                var fileName = fileData?.Value?.GetFileNameFromFileVariable();
                if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(fileName))
                {
                    CLog.Error("FileId or FileName is null or empty.");
                    return null;
                }
                // 构建完整的文件路径
                var filePathInfo = new FileInfo(Path.Combine(GlobalData.MainWebFileServer, fileId + "-" + fileName));
                string filePath = filePathInfo.FullName;
                // 确保文件的上级目录存在，如果不存在则创建
                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }
                var file = await _dbContext.Set<FileInformation>().FirstOrDefaultAsync(f => f.FileId == fileId);
                if (file == null)
                {
                    Log.Information($"File with ID {fileId} not found.");
                    return null;
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    //Console.WriteLine("1");
                    await System.IO.File.OpenRead(file.FilePath).CopyToAsync(stream);
                    //Console.WriteLine("2");
                }
                return fileId + "-" + fileName;
                //return "/webFiles/" + fileId + "-" + fileName;
            }
            catch (HttpRequestException ex)
            {
                CLog.Information($"An error1(UploadFileToWebServer) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error1(UploadFileToWebServer) occurred while fetching data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                CLog.Information($"An error2(UploadFileToWebServer) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error2(UploadFileToWebServer) occurred while fetching data: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 上传文件数据至服务器
        /// </summary>
        /// <param name="FileStream"></param>
        /// <param name="plug"></param>
        /// <returns></returns>
        public async Task<string?> UploadFileRequestToServer(FileUploadRequest? fur)
        {
            try
            {
                // 构建完整的文件路径
                var filePathInfo = new FileInfo(Path.Combine(GlobalData.MainFileServerPathRoot, fur.UploadPath, fur.FileName));
                string filePath = filePathInfo.FullName;
                //Log.Information("API2---the full path:" + filePath);
                //System.IO.File.Delete(filePath);
                // 确保文件的上级目录存在，如果不存在则创建
                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fur.FileStream?.OpenReadStream()?.CopyToAsync(stream);
                }

                var fileInfo = MapToEntity(fur, filePath);
                await CreateFileInformation(fileInfo);

                return filePath;
            }
            catch (HttpRequestException ex)
            {
                Log.Information($"An error1(UploadFileRequestToServer) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error1(UploadFileRequestToServer) occurred while fetching data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                Log.Information($"An error2(UploadFileRequestToServer) occurred while fetching data: {ex.Message}");
                Console.WriteLine($"An error2(UploadFileRequestToServer) occurred while fetching data: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 获取服务器上指定路径的文件列表
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public async Task<FileSystemNode?> GetFolderFiles(string? rootPath)
        {
            if (rootPath == null)
            {
                rootPath = GlobalData.PDZsRootPath;
            }
            if (rootPath == "all")
            {
                //Console.WriteLine(rootPath);
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                //Console.WriteLine("驱动器列表:");
                // 获取本地主机名
                string hostName = Dns.GetHostName();
                // 获取主机名对应的IP地址列表
                IPAddress[] addresses = Dns.GetHostEntry(hostName).AddressList;

                // 遍历IP地址列表，找到第一个IPv4地址
                IPAddress ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var root = new FileSystemNode("all", "服务端地址：" + ipv4.ToString(), true);
                //var subItems = Directory.GetFileSystemEntries(rootPath);
                //foreach (var subItem in subItems)
                //{
                //    var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                //    root.Children.Add(item);
                //}

                foreach (DriveInfo d in allDrives)
                {
                    //Console.WriteLine($"名称: {d.Name}");
                    //Console.WriteLine($"卷标: {d.VolumeLabel}");
                    //Console.WriteLine($"类型: {d.DriveType}");
                    //Console.WriteLine($"总大小: {d.TotalSize / 1024 / 1024} MB");
                    //Console.WriteLine($"可用空间: {d.AvailableFreeSpace / 1024 / 1024} MB");
                    //Console.WriteLine("-----------------------------");
                    var driverItem = new FileSystemNode(d.Name, d.Name, Directory.Exists(d.Name));
                    var subItems = Directory.GetFileSystemEntries(d.Name);
                    //Console.WriteLine(subItems);
                    foreach (var subItem in subItems)
                    {
                        var item = new FileSystemNode(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                        PopulateFileMetadata(item, subItem);
                        //Console.WriteLine(subItem);
                        driverItem.Children.Add(item);
                    }
                    //var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                    root.Children.Add(driverItem);

                }
                return root;
            }

            //请求体是相对路径，需要拼接上服务器的根路径
            rootPath = Path.Combine(GlobalData.PDZsRootPath, rootPath);
            try
            {
                if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
                var rootNode = new FileSystemNode(rootPath, rootPath, true);
                PopulateFolderRecursive(rootNode, rootPath, GlobalData.PDZsRootPath);
                return rootNode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 创建上传文件的信息数据
        /// <summary>
        /// 递归填充文件夹内容
        /// </summary>
        private static void PopulateFolderRecursive(FileSystemNode parentNode, string physicalPath, string rootPath)
        {
            if (!Directory.Exists(physicalPath)) return;
            
            var subItems = Directory.GetFileSystemEntries(physicalPath);
            foreach (var subItem in subItems)
            {
                var isDirectory = Directory.Exists(subItem);
                var relativePath = Path.GetRelativePath(rootPath, subItem);
                var item = new FileSystemNode(relativePath, Path.GetFileName(subItem), isDirectory)
                {
                    RelativePath = relativePath,
                    FolderDisplayName = Path.GetFileName(subItem)
                };
                PopulateFileMetadata(item, subItem);
                
                if (isDirectory)
                {
                    PopulateFolderRecursive(item, subItem, rootPath);
                }
                
                parentNode.Children ??= new List<FileSystemNode>();
                parentNode.Children.Add(item);
            }
        }

        /// </summary>
        /// <param name="fileInformation"></param>
        /// <returns></returns>
        public async Task CreateFileInformation(FileInformation fileInformation)
        {
            try
            {
                _dbContext.Set<FileInformation>().Add(fileInformation);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Information($"An error occurred while fetching data: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过文件ID下载文件
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<IResult> DownloadFileByFileId(string fileId)
        {
            try
            {
                var fileInformation = await _dbContext.Set<FileInformation>().FirstOrDefaultAsync(f => f.FileId == fileId);
                if (fileInformation == null)
                {
                    Log.Information("File not found");
                    return Results.NotFound();
                }
                var filePath = fileInformation.FilePath;
                var fileName = fileInformation.FileName;
                //Log.Information(filePath);
                //Log.Information(fileName);
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                if (fileStream == null)
                {
                    return Results.NotFound();
                }
                return Results.File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                Log.Information($"An error occurred while fetching downlaod file data: {ex.Message}");
                return Results.InternalServerError();
            }
        }

        /// <summary>
        /// 获取所有的文件信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<FileInformation>?> GetAllFileInformations()
        {
            try
            {
                var fileInformations = await _dbContext.Set<FileInformation>().ToListAsync();
                foreach (var fileInformation in fileInformations)
                {
                    Console.WriteLine(fileInformation.FileName);
                }
                return fileInformations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定文件ID的文件信息
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<FileInformation?> GetFileInformationById(string fileId)
        {
            try
            {
                var fileInformation = await _dbContext.Set<FileInformation>().FirstOrDefaultAsync(f => f.FileId == fileId);
                return fileInformation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取文本文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<IResult> GetFileContent(string filePath)
        {
            //Log.Information($"{filePath}");
            var fileInfo = new FileInfo(filePath.Trim('"'));
            //Log.Information($"{fileInfo.FullName}");
            if (!System.IO.File.Exists(fileInfo.FullName))
            {
                return Results.NotFound("文件未找到");
            }
            var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
            return Results.Stream(fileStream, "text/plain");
        }

        /// <summary>
        /// 通过文件ID获取文本文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<IResult> GetFileContentByFileId(string fileId)
        {
            var file = await _dbContext.Set<FileInformation>().FirstOrDefaultAsync(f => f.FileId == fileId);
            if (file == null)
            {
                return Results.NotFound();
            }
            var fileInfo = new FileInfo(file?.FilePath?.Trim('"'));
            //Log.Information($"{fileInfo.FullName}");
            if (!System.IO.File.Exists(fileInfo.FullName))
            {
                return Results.NotFound("文件未找到");
            }
            var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
            return Results.Stream(fileStream, "text/plain");
        }


        /// <summary>
        /// 通过文件ID获取文件流
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<IResult> GetFileStreamByFileId(string fileId)
        {
            // 输入验证
            if (string.IsNullOrEmpty(fileId))
            {
                return Results.BadRequest("文件ID不能为空");
            }

            try
            {
                // 查询文件信息
                var file = await _dbContext.Set<FileInformation>()
                    .FirstOrDefaultAsync(f => f.FileId == fileId);

                if (file == null)
                {
                    return Results.NotFound($"未找到ID为{fileId}的文件记录");
                }

                // 处理文件路径（移除可能的引号）
                string filePath = file.FilePath?.Trim('"');
                if (string.IsNullOrEmpty(filePath))
                {
                    return Results.BadRequest("文件路径为空");
                }

                var fileInfo = new FileInfo(filePath);

                // 检查文件是否存在
                if (!fileInfo.Exists)
                {
                    return Results.NotFound($"文件 {filePath} 不存在");
                }

                // 创建文件流（使用using确保资源释放）
                using var fileStream = new FileStream(
                    fileInfo.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read); // 允许其他进程读取文件

                // 设置响应头
                var contentType = GetContentType(fileInfo.Extension);
                return Results.Stream(
                    fileStream,
                    contentType,
                    fileInfo.Name); // 包含文件名以便客户端使用
            }
            catch (Exception ex)
            {
                // 记录详细错误日志
                CLog.Error("获取文件流时发生错误");
                return Results.StatusCode(500);
            }
        }

        /// <summary>
        /// 获取文件扩展名对应的MIME类型
        /// </summary>
        private string GetContentType(string fileExtension)
        {
            return fileExtension switch
            {
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }




        /// <summary>
        /// 通过相对路径下载文件（供文件浏览器下载使用）
        /// </summary>
        public async Task<IResult> DownloadFileByPath(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(GlobalData.MainFileServerPathRoot, filePath.TrimStart('/', '\\'));
                if (!File.Exists(fullPath))
                {
                    return Results.NotFound($"文件不存在: {fullPath}");
                }

                var fileInfo = new FileInfo(fullPath);
                var contentType = GetContentType(fileInfo.Extension);
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Results.File(fileStream, contentType, fileInfo.Name);
            }
            catch (Exception ex)
            {
                CLog.Error($"下载文件失败: {ex.Message}");
                return Results.StatusCode(500);
            }
        }


        public async Task<FileSystemNode?> GetPlugWorkpathFiles(string? plugDefinitionId)
        {
            if (plugDefinitionId == "all")
            {
                //Console.WriteLine(rootPath);
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                //Console.WriteLine("驱动器列表:");
                // 获取本地主机名
                string hostName = Dns.GetHostName();
                // 获取主机名对应的IP地址列表
                IPAddress[] addresses = Dns.GetHostEntry(hostName).AddressList;

                // 遍历IP地址列表，找到第一个IPv4地址
                IPAddress ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var root = new FileSystemNode("all", "服务端地址：" + ipv4.ToString(), true);
                //var subItems = Directory.GetFileSystemEntries(rootPath);
                //foreach (var subItem in subItems)
                //{
                //    var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                //    root.Children.Add(item);
                //}

                foreach (DriveInfo d in allDrives)
                {
                    //Console.WriteLine($"名称: {d.Name}");
                    //Console.WriteLine($"卷标: {d.VolumeLabel}");
                    //Console.WriteLine($"类型: {d.DriveType}");
                    //Console.WriteLine($"总大小: {d.TotalSize / 1024 / 1024} MB");
                    //Console.WriteLine($"可用空间: {d.AvailableFreeSpace / 1024 / 1024} MB");
                    //Console.WriteLine("-----------------------------");
                    var driverItem = new FileSystemNode(d.Name, d.Name, Directory.Exists(d.Name));
                    var subItems = Directory.GetFileSystemEntries(d.Name);
                    //Console.WriteLine(subItems);
                    foreach (var subItem in subItems)
                    {
                        var item = new FileSystemNode(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                        //Console.WriteLine(subItem);
                        driverItem.Children.Add(item);
                    }
                    //var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                    root.Children.Add(driverItem);

                }
                return root;
            }

            var plug = await _dbContext.Set<Plug.Models.Plug.Plug>().FirstOrDefaultAsync(p => p.DefinitionId == plugDefinitionId);
            if (plug == null)
            {
                Log.Information($"Plug with DefinitionId {plugDefinitionId} not found.");
                return null;
            }
            //请求体是相对路径，需要拼接上服务器的根路径
            var rootFolder = Path.Combine(GlobalData.MainFileServerPathRoot, plug.WorkPath);
            try
            {
                if (!Directory.Exists(rootFolder)) Directory.CreateDirectory(rootFolder);
                var root = new FileSystemNode(rootFolder, rootFolder, true);
                var subItems = Directory.GetFileSystemEntries(rootFolder);
                foreach (var subItem in subItems)
                {
                    var item = new FileSystemNode(
                        Path.GetRelativePath(GlobalData.MainFileServerPathRoot, subItem),   //返回相对路径
                        Path.GetFileName(subItem),
                        Directory.Exists(subItem));
                    item.FolderDisplayName = Path.GetFileName(subItem);
                    root.Children.Add(item);
                }
                return root;
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                Log.Information(ex.StackTrace);
                return null;
            }
        }



        public async Task<IActionResult> UploadChunk(FileUploadRequest? request)
        {
            Console.WriteLine("-------1010101------");
            if (request == null)
                return new BadRequestResult();

            try
            {
                // 解析请求参数
                var fileName = request.FileName;
                var offset = request.Offset;
                var chunkSize = request.ChunkSize;
                var fileId = request.FileId;

                if (string.IsNullOrEmpty(fileName))
                    return new BadRequestResult();

                // 获取文件流
                var fileContent = request.FileStream;
                if (fileContent == null)
                    return new BadRequestResult();
                // 构建临时文件路径（将路径分隔符替换为_，避免子目录不存在导致创建失败）
                var safeFileName = fileName.Replace('/', '_').Replace('\\', '_');
                var tempFilePath = Path.Combine(GlobalData.MainWebFileServer, $"{fileId}_{safeFileName}.temp");

                // 以追加模式打开文件流，写入当前块
                using (var fileStream = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 8192, true))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    await using var contentStream = fileContent.OpenReadStream();
                    await contentStream.CopyToAsync(fileStream);
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                CLog.Error(ex.Message);
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CompleteUpload(FileUploadRequest? request)
        {
            //Log.Information("接收到文件上传完成提醒");
            //Console.WriteLine("接收到文件上传完成提醒");
            if (request == null)
            {
                CLog.Error("请求内容为空，无法完成上传");
                return new BadRequestResult();
            }

            try
            {
                // 解析请求参数
                var fileName = request.FileName;
                var fileId = request.FileId;
                Log.Information($"收到文件上传完成信息: {fileName}, FileId: {fileId}");
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileId))
                    return new BadRequestResult();

                // 临时文件路径和最终文件路径（将路径分隔符替换为_，避免子目录不存在导致创建失败）
                var safeFileName = fileName.Replace('/', '_').Replace('\\', '_');
                var tempFilePath = Path.Combine(GlobalData.MainWebFileServer, $"{fileId}_{safeFileName}.temp");
                var finalFilePath = Path.Combine(GlobalData.PDZsRootPath, request.UploadPath, request.FileName);

                // 检查临时文件是否存在
                if (!File.Exists(tempFilePath))
                    return new NotFoundResult();

                var directory = Path.GetDirectoryName(finalFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    // 确保最终文件的目录存在
                    Directory.CreateDirectory(directory);
                }

                // 重命名临时文件为最终文件名
                File.Move(tempFilePath, finalFilePath, true);

                // 这里可以添加文件合并后的处理逻辑，如记录文件信息到数据库
                Log.Information($"文件 {finalFilePath} 上传完成");

                var fileInfo = MapToEntity(request, finalFilePath);
                await CreateFileInformation(fileInfo);

                //return new OkObjectResult(finalFilePath);            
                // 手动创建ContentResult
                return new ContentResult
                {
                    Content = finalFilePath,
                    ContentType = "text/plain",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                CLog.Error(ex.Message + "完成文件上传时发生错误");
                return new StatusCodeResult(500);
            }
        }

        // 辅助方法：从表单中获取值
        private async Task<string> GetFormValue(MultipartFormDataContent form, string key)
        {
            var content = form.FirstOrDefault(x => x.Headers.ContentDisposition?.Name?.Trim('"') == key);
            if (content == null)
                return string.Empty;

            return await content.ReadAsStringAsync();
        }

        /// <summary>
        /// 根据PlugDataZone和PlugDefinitionId删除指定文件，主要用于处理临时文件，如可视化文件等
        /// </summary>
        /// <param name="plugDataZone"></param>
        /// <param name="PlugDefinitionId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<FileInformation?> DeleteFile(FileDeleteRequest fileDeleteRequest)
        {
            string FilePath = fileDeleteRequest.FilePath;
            string fileName = fileDeleteRequest.FileName;
            
            // 支持两种调用方式：
            // 1. FilePath 含目录，FileName 含文件名（传统方式）
            // 2. FilePath 含完整路径（含文件名），FileName 为空（简化方式）
            string filePath;
            if (string.IsNullOrEmpty(fileName))
            {
                filePath = Path.Combine(GlobalData.PDZsRootPath, FilePath?.TrimStart('/', '\\') ?? string.Empty);
            }
            else
            {
                filePath = Path.Combine(GlobalData.PDZsRootPath, FilePath ?? string.Empty, fileName);
            }
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    var fileInformation = await _dbContext.Set<FileInformation>().FirstOrDefaultAsync(f => f.FilePath == filePath);
                    if (fileInformation != null)
                    {
                        _dbContext.Set<FileInformation>().Remove(fileInformation);
                        await _dbContext.SaveChangesAsync();
                        return fileInformation;
                    }
                    // 文件已删除，数据库无记录时返回占位对象表示成功
                    return new FileInformation { FilePath = filePath, FileName = fileName };
                }
                else
                {
                    CLog.Warning($"File {filePath} not found for deletion.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLog.Error($"An error occurred while deleting file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Map FileUploadRequest to FileInformation entity
        /// </summary>
        public static FileInformation MapToEntity(FileUploadRequest request, string filePath)
        {
            return new FileInformation
            {
                FileId = request.FileId,
                FilePath = filePath,
                FileName = request.FileName,
                FileUploadPath = request.UploadPath,
                FileUploadType = request.FileUploadType,
                FileUploader = request.FileCreator,
                FileUploadDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public async Task<bool> MoveDirectory(string sourcePath, string destPath)
        {
            try
            {
                var sourceFull = Path.Combine(GlobalData.MainFileServerPathRoot, sourcePath.TrimStart('/', '\\'));
                var destFull = Path.Combine(GlobalData.MainFileServerPathRoot, destPath.TrimStart('/', '\\'));

                if (!Directory.Exists(sourceFull))
                {
                    Log.Warning($"MoveDirectory: 源目录不存在: {sourceFull}");
                    return false;
                }

                // 确保目标上级目录存在
                var destParent = Path.GetDirectoryName(destFull);
                if (destParent != null && !Directory.Exists(destParent))
                    Directory.CreateDirectory(destParent);

                // 如果目标已存在，先删除
                if (Directory.Exists(destFull))
                    Directory.Delete(destFull, true);

                Directory.Move(sourceFull, destFull);
                Log.Information($"MoveDirectory: {sourceFull} → {destFull}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"MoveDirectory 失败: {sourcePath} → {destPath}");
                return false;
            }
        }

        public async Task<(Stream? fileStream, string? fileName, string? errorMessage)> DownloadToolAsync(string toolName, string toolVersion, string toolPath)
        {
            try
            {
                if (string.IsNullOrEmpty(toolPath))
                    return (null, null, "工具路径不能为空");

                // 拼接完整路径
                var fullPath = Path.Combine(GlobalData.MainFileServerPathRoot, toolPath);

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return (null, null, $"工具路径不存在: {fullPath}");

                var tempDir = Path.GetTempPath();
                var zipFileName = $"{toolName}_{toolVersion}.zip";
                var tempZipPath = Path.Combine(tempDir, $"tool_download_{Guid.NewGuid()}.zip");

                if (Directory.Exists(fullPath))
                {
                    ZipFile.CreateFromDirectory(fullPath, tempZipPath);
                }
                else
                {
                    using var zipStream = new FileStream(tempZipPath, FileMode.Create);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
                    archive.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                }

                var fileStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return (fileStream, zipFileName, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"打包下载工具失败: {toolName}({toolVersion}), 路径: {toolPath}");
                return (null, null, $"打包下载工具失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为 FileSystemNode 填充文件元数据（大小、上传人、最后修改时间）
        /// </summary>
        private static void PopulateFileMetadata(FileSystemNode node, string physicalPath)
        {
            if (node.IsDirectory) return;
            try
            {
                var fileInfo = new FileInfo(physicalPath);
                if (fileInfo.Exists)
                {
                    node.Size = fileInfo.Length;
                    node.LastWriteTime = fileInfo.LastWriteTime;
                    node.FileType = Path.GetExtension(physicalPath).TrimStart('.');
                    // 尝试获取文件所有者（Windows 特有，跨平台时忽略）
                    try
                    {
                        var acl = fileInfo.GetAccessControl();
                        node.Creator = acl.GetOwner(typeof(System.Security.Principal.NTAccount)).Value;
                    }
                    catch
                    {
                        // 非 Windows 或无权限时忽略
                    }
                }
            }
            catch
            {
                // 忽略读取元数据失败
            }
        }
    }
}
