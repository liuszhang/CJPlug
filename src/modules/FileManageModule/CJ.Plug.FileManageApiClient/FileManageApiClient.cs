using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace CJ.Plug.FileManageApiClient
{
    public class FileManageApiClient : BaseApiClient, IFileManageApiClient
    {
        private readonly IServiceProvider _serviceProvider;
        private IPDZApiClient? PDZApiClient;

        public FileManageApiClient(HttpClient dispatcherClient, IServiceProvider serviceProvider) : base(dispatcherClient)
        {
            _serviceProvider = serviceProvider;
        }

        
            /// <summary>
            /// 获取图站上指定文件夹内文件内容
            /// </summary>
            /// <param name="parentNode"></param>
            /// <returns></returns>
            public async Task<List<FileSystemNode>?> GetFolderFiles(FileSystemNode? parentNode, string? userName, string? toolAgentHostIp)
            {
                Console.WriteLine("begin to get files");
                //string toolAgentTogGet = "";
                //using var Http = new HttpClient();
                //Http.BaseAddress = new Uri(toolAgentTogGet);
                var response = new HttpResponseMessage();
                if (parentNode == null)
                {
                    response = await httpClient.GetAsync("/api/file/GetFolderFiles/" + (userName + "/Tools").Replace('\\', '/'));
                }

                if (parentNode != null)
                {
                    //Log.Information($"GetFolderFiles: {parentNode.FullPath}");
                    response = await httpClient.GetAsync("/api/file/GetFolderFiles/" + parentNode.FullPath.Replace('\\', '/'));
                }
                //response = await Http.GetAsync("api/ToolAgentFile/GetCurrentFiles/?rootPath=" + parentNode.FullPath);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    //MyCon1sole.WriteLine(json);
                    var options = new JsonSerializerOptions
                    {
                        IgnoreNullValues = true, // 可选，忽略JSON中的null值
                        PropertyNameCaseInsensitive = true, // 可选，忽略属性名称大小写
                                                            //ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                    };
                    try
                    {
                        var tmpFileTreeNodes = System.Text.Json.JsonSerializer.Deserialize<FileSystemNode>(json, options);
                        if (tmpFileTreeNodes == null)
                        {
                            return null;
                        }
                        //MyCon1sole.WriteLine(tmpFileTreeNodes.Name);
                        if (parentNode == null)
                        {
                            var root = new List<FileSystemNode>();
                            root.Add(tmpFileTreeNodes);
                            return root;
                        }
                        //var DynamicChildTreeItems = new HashSet<FileSystemNode>(tmpFileTreeNodes.Children);
                        return tmpFileTreeNodes.Children;
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Failed to deserialize: {ex.Message}");
                    }

                }
                else
                {
                    Console.WriteLine($"Failed to load file tree: {response.ReasonPhrase}");
                    return null;
                }
                return null;
            }

            /// <summary>
            /// 获取流程工作目录下的文件
            /// </summary>
            /// <returns></returns>
            public async Task<List<FileSystemNode>?> GetWorkPathFiles(Plug.Models.Plug.Plug Plug)
            {
                var parentNode = new FileSystemNode(Plug.WorkPath, "WorkPath", true);
                return await GetFolderFiles(parentNode, Plug.Creater, null);
            }

            /// <summary>
            /// 上传文件至工作目录,供前端使用
            /// </summary>
            /// <param name="browserFile"></param>
            /// <param name="PlugData"></param>
            /// <returns></returns>
            public async Task<(string?, string?)> UploadFileToWorkPath(IBrowserFile browserFile, PlugData PlugData)
            {
                var fur = new FileUploadRequest
                {
                    FileName = browserFile.Name,
                    FileId = RandomLongIdentityGenerator.GenerateId(),
                    FileCreator = PlugData.Creater,
                    FileUploadType = FileFolderType.Design.ToString(),
                    UploadPath = PlugData.WorkPath,
                    ProcessDefinitionId = PlugData.ParentPlugDefinitionId,
                    PlugDefinitionId = PlugData.PlugDefinitionId
                };
                return (await UploadFileByRequest(browserFile, fur), fur.FileId);
            }

            //上传文件至文件参数
            public async Task<(string?, string?)> UploadFileToVariable(Stream stream, string fileName, PlugVariableData PlugVariableData)
            {
                PDZApiClient= PDZApiClient??_serviceProvider.GetRequiredService<IPDZApiClient>();
                var PlugDataZone = await PDZApiClient.GetPDZByIdAsync(PlugVariableData.PlugDataZoneId);
                if (PlugDataZone == null)
                {
                    CLog.Error("上传文件至参数失败：PDZ为空。");
                    return (null, null);
                }
                var fur = new FileUploadRequest
                {
                    FileName = fileName,
                    FileId = RandomLongIdentityGenerator.GenerateId(),
                    FileCreator = PlugDataZone.UserName,
                    UploadPath = Path.Combine(PlugDataZone?.PDZWorkPath, PlugVariableData?.PlugDefinitionId),
                    ProcessDefinitionId = PlugDataZone.PlugDefinitionId,
                    PlugDefinitionId = PlugVariableData.PlugDefinitionId
                };
                var filePath = await UploadFileInChunks(stream, fur);

                CLog.Information($"上传文件至变量成功，文件名：{fileName}，文件路径：{filePath}");
                var OldFileId = PlugVariableData.Value.GetFileIdFromFileVariable();
                PlugVariableData = await PDZApiClient.GetPlugVariableDataById(PlugVariableData.Id);
                //只更新参数的文件内容，不更新其他值
                PlugVariableData.Value = $"{fileName}:{fur.FileId}";
                await PDZApiClient.UpdatePlugVariableData(PlugVariableData);
                CLog.Information($"更新变量成功：{PlugVariableData.Name}({PlugVariableData.Id})");
                StatusReporter.PDZUpdated(PlugDataZone.PDZId);
                //删除之前的文件,暂时先不删除
                //await DeleteFile(OldFileId);

                return (filePath, fur.FileId);
            }

            //上传文件至文件参数
            //public async Task<string?> UploadFileToVariable(Stream stream, string fileName, PlugVariableData PlugVariableData)
            //{
            //    var fileId = RandomLongIdentityGenerator.GenerateId();
            //    var fur = new FileUploadRequest
            //    {
            //        FileName = fileName,
            //        FileId = fileId,
            //        //UploadPath = $"{PlugVariableData.PlugDefinitionId}/{PlugVariableData.Name}",
            //        UploadPath = $"{fileId}",
            //    };
            //    var response = await UploadFileStreamByRequest(stream, fur);
            //    if (response == null)
            //    {
            //        Log.Information($"上传文件至变量失败，文件名：{fileName}");
            //        return null;
            //    }
            //    Log.Information($"上传文件至变量成功，文件名：{fileName}，文件路径：{response}");
            //    var OldFileId = PlugVariableData.Value.GetFileIdFromFileVariable();
            //    PlugVariableData = await GetPlugVariableDataById(PlugVariableData.Id);
            //    //只更新参数的文件内容，不更新其他值
            //    PlugVariableData.Value = $"{fileName}:{fur.FileId}";
            //    await UpdatePlugVariableData(PlugVariableData);
            //    Log.Information($"上传文件至变量成功。{PlugVariableData.Name}({PlugVariableData.Id})");
            //    //删除之前的文件,暂时先不删除
            //    //await DeleteFile(OldFileId);

            //    return response;
            //}


            /// <summary>
            /// 上传文件至工作目录(Stream流方式)
            /// </summary>
            /// <param name="browserFile"></param>
            /// <param name="Plug"></param>
            /// <returns></returns>
            public async Task<(string?, string?)> UploadFileStreamToWorkPath(Stream stream, string fileName, Plug.Models.Plug.Plug Plug)
            {
                var fur = new FileUploadRequest
                {
                    FileName = fileName,
                    FileId = RandomLongIdentityGenerator.GenerateId(),
                    FileCreator = Plug.Creater,
                    FileUploadType = FileFolderType.Design.ToString(),
                    UploadPath = Plug.WorkPath,
                    ProcessDefinitionId = Plug.ParentPlugDefinitionId,
                    PlugDefinitionId = Plug.DefinitionId
                };
                return (await UploadFileStreamByRequest(stream, fur), fur.FileId);
                //return (await UploadFileInChunks(stream, fur), fur.FileId);
            }





            /// <summary>
            /// 以分块的方式上传文件至工作目录,用于大文件上传，避免内存溢出问题
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="fileName"></param>
            /// <param name="PlugData"></param>
            /// <returns></returns>
            public async Task<(string?, string?)> UploadFileStreamToWorkPathInChunks(Stream stream, string fileName, PlugData PlugData)
            {
                var fur = new FileUploadRequest
                {
                    FileName = fileName,
                    FileId = RandomLongIdentityGenerator.GenerateId(),
                    FileCreator = PlugData.Creater,
                    FileUploadType = FileFolderType.Design.ToString(),
                    UploadPath = PlugData.WorkPath,
                    ProcessDefinitionId = PlugData.ParentPlugDefinitionId,
                    PlugDefinitionId = PlugData.PlugDefinitionId,
                };
                //return (await UploadFileStreamByRequest(stream, fur), fur.FileId);
                return (await UploadFileInChunks(stream, fur), fur.FileId);
            }


            /// <summary>
            /// 边上传文件边写入流，一种内存优化手段
            /// </summary>
            /// <param name="streamWriter"></param>
            /// <param name="fileName"></param>
            /// <param name="plug"></param>
            /// <returns></returns>
            public async Task<string?> UploadFileStreamToWorkPath(Func<Stream, Task> streamWriter, string fileName, Plug.Models.Plug.Plug plug)
            {
                var fur = new FileUploadRequest
                {
                    FileName = fileName,
                    FileId = RandomLongIdentityGenerator.GenerateId(),
                    FileCreator = plug.Creater,
                    FileUploadType = FileFolderType.Design.ToString(),
                    UploadPath = plug.WorkPath,
                    ProcessDefinitionId = plug.ParentPlugDefinitionId,
                    PlugDefinitionId = plug.DefinitionId
                };

                using var form = new MultipartFormDataContent();

                // 创建一个自定义的内存流，用于捕获写入的数据
                using var memoryStream = new MemoryStream();

                // 使用PushStreamContent将处理后的数据写入内存流
                using var content = new CustomPushStreamContent(async (outputStream, _, _) =>
                {
                    try
                    {
                        // 调用委托处理并写入内容
                        await streamWriter(outputStream);
                        await outputStream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"写入流时发生错误: {ex.Message}");
                        throw;
                    }
                });

                // 将内容添加到表单
                form.Add(content, "FileStream", fur.FileName);

                // 添加其他字段
                form.Add(new StringContent(fur.UploadPath ?? ""), "UploadPath");
                form.Add(new StringContent(fur.FileName ?? ""), "FileName");
                form.Add(new StringContent(fur.FileId.ToString()), "FileId");
                form.Add(new StringContent(fur.FileCreator ?? ""), "FileCreator");
                form.Add(new StringContent(fur.FileUploadType ?? ""), "FileUploadType");
                form.Add(new StringContent(fur.ProcessDefinitionId ?? ""), "ProcessDefinitionId");
                form.Add(new StringContent(fur.PlugDefinitionId ?? ""), "PlugDefinitionId");

                // 发送请求
                var response = await httpClient.PostAsync("api/file/upload", form);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("------------File uploaded successfully.");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"-----------Failed to upload file. Status code: {response.StatusCode}");
                    return null;
                }
            }


            // 自定义 PushStreamContent 实现
            public class CustomPushStreamContent : HttpContent
            {
                private readonly Func<Stream, HttpContent, TransportContext?, Task> _onStreamAvailable;
                private readonly CancellationToken _cancellationToken;

                public CustomPushStreamContent(
                    Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable,
                    CancellationToken cancellationToken = default)
                {
                    _onStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
                    _cancellationToken = cancellationToken;
                }

                protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
                {
                    try
                    {
                        await _onStreamAvailable(stream, this, context);
                    }
                    catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
                    {
                        // 处理取消请求
                        throw;
                    }
                }

                protected override bool TryComputeLength(out long length)
                {
                    length = -1;
                    return false;
                }
            }


            /// <summary>
            /// 封装Request上传文件,供前端使用
            /// </summary>
            /// <param name="browserFile"></param>
            /// <param name="fur"></param>
            /// <returns></returns>
            private async Task<string?> UploadFileByRequest(IBrowserFile browserFile, FileUploadRequest fur)
            {
                var stream = browserFile.OpenReadStream(maxAllowedSize: 512000000);
                //return await UploadFileStreamByRequest(stream, fur);
                //分块上传方法
                return await UploadFileInChunks(stream, fur);
            }

            /// <summary>
            /// 普通的文件上传方法,用于Stream流方式上传文件
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="fur"></param>
            /// <returns>文件路径</returns>
            private async Task<string?> UploadFileStreamByRequest(Stream stream, FileUploadRequest fur)
            {
                // 创建 multipart/form-data 内容
                var form = new MultipartFormDataContent();
                form.Add(new StreamContent(stream, 8192), "FileStream", fur.FileName);
                //上传服务器后的相对路径由这里决定
                //fur.UploadPath= fur.FileCreator + "//" + fur.FileUploadType + "//" + fur.ProcessDefinitionId+"//"+ fur.PlugDefinitionId;
                // 添加其他字段
                form.Add(new StringContent(fur.UploadPath ?? ""), "UploadPath");
                form.Add(new StringContent(fur.FileName ?? ""), "FileName");
                form.Add(new StringContent(fur.FileId ?? ""), "FileId");
                form.Add(new StringContent(fur.FileCreator ?? ""), "FileCreator");
                form.Add(new StringContent(fur.FileUploadType ?? ""), "FileUploadType");
                form.Add(new StringContent(fur.ProcessDefinitionId ?? ""), "ProcessDefinitionId");
                form.Add(new StringContent(fur.PlugDefinitionId ?? ""), "PlugDefinitionId");

                // 发送请求
                var response = await httpClient.PostAsync("api/file/upload", form);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("------------File uploaded successfully.");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"-----------Failed to upload file. Status code: {response.StatusCode}");
                    return null;
                }
            }


            /// <summary>
            /// 大文件分块上传
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="fur"></param>
            /// <param name="chunkSize"></param>
            /// <returns></returns>
            private async Task<string?> UploadFileInChunks(Stream stream, FileUploadRequest fur, int chunkSize = 5 * 1024 * 1024, CancellationToken cancellationToken = default)
            {
                var buffer = new byte[chunkSize];
                int bytesRead;
                long offset = 0;
                var fileName = fur.FileName;
                try
                {
                    //Log.Information($"准备上传{fileName}");
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
                    {
                        using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
                        using var form = new MultipartFormDataContent();

                        // 添加文件内容
                        var fileContent = new StreamContent(chunkStream);
                        form.Add(fileContent, "FileStream", fileName);

                        // 添加元数据
                        form.Add(new StringContent(offset.ToString()), "Offset");
                        form.Add(new StringContent(bytesRead.ToString()), "ChunkSize");

                        // 添加其他字段
                        form.Add(new StringContent(fur.UploadPath ?? ""), "UploadPath");
                        form.Add(new StringContent(fur.FileName ?? ""), "FileName");
                        form.Add(new StringContent(fur.FileId ?? ""), "FileId");
                        form.Add(new StringContent(fur.FileCreator ?? ""), "FileCreator");
                        form.Add(new StringContent(fur.FileUploadType ?? ""), "FileUploadType");
                        form.Add(new StringContent(fur.ProcessDefinitionId ?? ""), "ProcessDefinitionId");
                        form.Add(new StringContent(fur.PlugDefinitionId ?? ""), "PlugDefinitionId");


                        var response = await httpClient.PostAsync("api/file/uploadchunk", form, cancellationToken);

                        //using var response = await httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        offset += bytesRead;
                    }
                    Log.Information($"上传{fileName}分块完成，已上传{offset}字节。准备通知服务器。");


                    // 通知服务器所有块已上传完成
                    var completeForm = new MultipartFormDataContent();
                    completeForm.Add(new StringContent(fur.FileName ?? ""), "FileName");
                    completeForm.Add(new StringContent(fur.FileId ?? ""), "FileId");
                    completeForm.Add(new StringContent(fur.UploadPath ?? ""), "UploadPath");
                    completeForm.Add(new StringContent(fur.FileCreator ?? ""), "FileCreator");
                    completeForm.Add(new StringContent(fur.FileUploadType ?? ""), "FileUploadType");
                    completeForm.Add(new StringContent(fur.ProcessDefinitionId ?? ""), "ProcessDefinitionId");
                    completeForm.Add(new StringContent(fur.PlugDefinitionId ?? ""), "PlugDefinitionId");

                    var response2 = await httpClient.PostAsync("api/file/completeupload", completeForm);
                    response2.EnsureSuccessStatusCode();
                    var filePath = await response2.Content.ReadAsStringAsync();
                    return filePath;
                }
                catch (Exception ex)
                {
                    CLog.Error($"上传{fileName}分块完成通知失败: {ex.Message}");
                    return null;
                }
            }





            public async Task<string?> UploadFileWithOutFileInfo(string filePath)
            {
                try
                {
                    // 创建 multipart/form-data 内容
                    var form = new MultipartFormDataContent();
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var streamContent = new StreamContent(fileStream);
                    //var fileContent = new ByteArrayContent(await streamContent.ReadAsByteArrayAsync());

                    form.Add(streamContent, "FileStream", Path.GetFileName(filePath));
                    // 添加其他字段
                    form.Add(new StringContent("stlFiles"), "UploadPath");
                    form.Add(new StringContent(Path.GetFileName(filePath)), "FileName");
                    form.Add(new StringContent(""), "FileId");
                    form.Add(new StringContent(""), "FileCreator");
                    form.Add(new StringContent(""), "FileUploadType");
                    form.Add(new StringContent(""), "ProcessDefinitionId");
                    form.Add(new StringContent(""), "PlugDefinitionId");

                    // 发送请求
                    var response = await httpClient.PostAsync("api/file/uploadWithOutInfo", form);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("------------File uploaded successfully.");
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Console.WriteLine($"-----------Failed to upload file. Status code: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Information($"-----------Failed to upload file. Exception: {ex.Message}");
                    return null;
                }


            }

            public async Task<Stream?> DownloadFileByFileId(string fileId)
            {
                var response = await httpClient.GetAsync("api/file/DownloadFileByFileId/" + fileId);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    Log.Information($"Failed to download file. Status code: {response.StatusCode}");
                    return null;
                }
            }

            public async Task<Stream?> DownloadFileByPath(string filePath)
            {
                var response = await httpClient.GetAsync("api/file/download/" + Uri.EscapeDataString(filePath));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    Log.Information($"Failed to download file by path. Status code: {response.StatusCode}");
                    return null;
                }
            }

            public async Task<string?> GetFileContent(string filePath)
            {
                // 构建查询字符串
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["filePath"] = filePath;
                //var fullUrl = $"{apiUrl}?{query}";
                // 发送请求
                var response = await httpClient.PostAsync($"api/file/getContent?{query}", null);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("------------GET File content successfully.");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"-----------Failed to GET file CONTENT. Status code: {response.StatusCode}");
                    return null;
                }
            }

            public async Task<string?> GetFileContentByFileId(string fileId)
            {
                //Log.Information(fileId);
                // 发送请求
                var response = await httpClient.GetAsync($"api/file/getContentByFileId/{fileId}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("------------GET File content successfully.");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"-----------Failed to GET file CONTENT. Status code: {response.StatusCode}");
                    return null;
                }
            }


            public async Task<Stream?> GetFileStreamByFileId(string fileId)
            {
                //Log.Information(fileId);
                // 发送请求
                var response = await httpClient.GetAsync($"api/file/getContentByFileId/{fileId}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("------------GET File content successfully.");
                    return await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    Console.WriteLine($"-----------Failed to GET file CONTENT. Status code: {response.StatusCode}");
                    return null;
                }
            }

            public async Task<string?> UploadFileToWebServer(PlugVariableData stlFileData)
            {
                try
                {
                    // 发送请求
                    var response = await httpClient.PostAsJsonAsync<PlugVariableData>($"api/file/uploadFileToWebServer", stlFileData);
                    if (response.IsSuccessStatusCode)
                    {
                        CLog.Information("------------File uploaded successfully.");
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        CLog.Warning($"-----------Failed to upload file to web server. Status code: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    CLog.Error($"-----------Failed to upload file to web server. Exception: {ex.Message}");
                    return null;
                }


            }

        public async Task<FileInformation?> DeleteFileWithRequest(FileDeleteRequest fileDeleteRequest)
        {
            try
            {
                // 发送请求
                var response = await httpClient.PostAsJsonAsync<FileDeleteRequest>($"api/file/delete", fileDeleteRequest);
                if (response.IsSuccessStatusCode)
                {
                    //CLog.Information("------------File Deleted successfully.");
                    var result = await response.Content.ReadAsStringAsync();
                    var fileInfo = JsonSerializer.Deserialize<FileInformation>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return fileInfo;
                }
                else
                {
                    CLog.Warning($"-----------Failed to Delete file. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLog.Error($"-----------Failed to Delete file. Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> MoveDirectory(string sourcePath, string destPath)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/file/moveDirectory",
                    new { SourcePath = sourcePath, DestPath = destPath });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                CLog.Error($"MoveDirectory 失败: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> UploadFileFromBase64(string fileContent, string fileName)
        {
            try
            {
                var payload = new { fileContent, fileName };
                var response = await httpClient.PostAsJsonAsync("api/file/uploadBase64", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    CLog.Warning($"UploadFileFromBase64 失败: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLog.Error($"UploadFileFromBase64 异常: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> UploadFileFromUrl(string url, string? fileName)
        {
            try
            {
                var payload = new { url, fileName };
                var response = await httpClient.PostAsJsonAsync("api/file/uploadFromUrl", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    CLog.Warning($"UploadFileFromUrl 失败: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLog.Error($"UploadFileFromUrl 异常: {ex.Message}");
                return null;
            }
        }

        public async Task<List<FileInformation>?> SearchFiles(string? keyword)
        {
            try
            {
                var query = string.IsNullOrWhiteSpace(keyword) ? "" : $"?keyword={Uri.EscapeDataString(keyword)}";
                var response = await httpClient.GetAsync($"api/file/searchFiles{query}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<FileInformation>>();
                }
                else
                {
                    CLog.Warning($"SearchFiles 失败: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLog.Error($"SearchFiles 异常: {ex.Message}");
                return null;
            }
        }
    }
}
