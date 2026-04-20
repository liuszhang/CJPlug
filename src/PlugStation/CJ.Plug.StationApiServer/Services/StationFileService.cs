
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Shared;
using CJ.Plug.StationApiService.Contracts;
using Serilog;

namespace CJ.Plug.StationApiService.Services
{
    public class StationFileService : IStationFileService
    {
        private IFileManageApiClient FileManageApiClient { get; set; }

        public StationFileService(IFileManageApiClient mainApiClient)
        {
            FileManageApiClient = mainApiClient;
        }

        public async Task<string> DownloadFileByFileId(string fileId, string fileName)
        {
            // 定义文件保存路径
            string directoryPath = Path.Combine(GlobalData.StationFileRootPath, fileId);
            string filePath = Path.Combine(directoryPath, fileName);

            try
            {
                // 获取下载流
                using var downloadStream = await FileManageApiClient.DownloadFileByFileId(fileId);
                if (downloadStream == null)
                {
                    Log.Information($"下载或保存文件时发生错误: {fileName}({fileId})");
                    return null;
                }

                // 确保目录存在，如果不存在则创建
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 创建文件流并将下载内容写入文件
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await downloadStream.CopyToAsync(fileStream);

                // 返回保存的文件路径
                return filePath;
            }
            catch (Exception ex)
            {
                // 处理可能的异常（如网络错误、权限问题等）
                Log.Information($"下载或保存文件时发生错误: {ex.Message}");
                throw; // 根据需要决定是否抛出异常或返回错误信息
            }
        }

        public async Task<(string?,string?)> UploadFileToVariable(PlugVariableData variableData)
        {
            var fileInfo= new FileInfo(variableData.Value);
            var fileName= fileInfo.Name;
            using var fileStream = new FileStream(variableData.Value, FileMode.Open, FileAccess.Read);
            return await FileManageApiClient.UploadFileToVariable(fileStream, fileName, variableData);
        }
    }
}
