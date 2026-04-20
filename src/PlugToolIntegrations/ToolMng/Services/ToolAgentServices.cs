using System.Diagnostics;
using ToolMng.Contracts;
using ToolMng.Models;

namespace ToolMng.Services
{
    public class ToolAgentServices : IToolAgentServices
    {
        
        /// <summary>
        /// 上传文件含（索引号）到图站
        /// </summary>
        /// <param name="file"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<string> UploadFileToToolAgentFileServerAsync(IFormFile file, string index)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 上传文件到图站
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<string?> UploadFileToToolAgentFileServerAsync(IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                return null;
            }
            try
            {
                // 确保目录存在
                //var uploadsFolder = Path.Combine(@"C:\tmp", "metedatas", "TmpActivityFiles");
                var uploadsFolder = StaticData.ToolAgentServer;
                //var currentDirectory = Directory.GetCurrentDirectory();
                //var uploadsFolder = Path.Combine(currentDirectory, "toolAgentFiles", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // 构建完整的文件路径并保存
                var filePathInfo = new FileInfo(Path.Combine(uploadsFolder, Guid.NewGuid().ToString()+"_"+file.FileName));
                string filePath = filePathInfo.FullName;
                //Console.WriteLine(filePath);
                // 确保文件的上级目录存在，如果不存在则创建
                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }

                //System.IO.File.WriteAllBytes(filePath, fileBytes);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                //File.WriteAllBytes(filePath, fileBytes);

                //return Ok($"文件 '{file.FileName}' 已成功创建。");
                Console.WriteLine($"文件 '{filePath}' 已成功创建。");
                return filePath;

            }
            catch (FormatException fe)
            {
                Console.WriteLine($"Base64解码错误: {fe.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(StatusCodes.Status500InternalServerError.ToString()+ $"文件保存失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stream方式上传文件至图站
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string?> UploadFileStreamToToolAgentFileServerAsync(Stream file, string fileName)
        {
            if (file == null)
            {
                return null;
            }
            try
            {
                // 确保目录存在
                //var uploadsFolder = Path.Combine(@"C:\tmp", "metedatas", "TmpActivityFiles");
                var uploadsFolder = Path.Combine(StaticData.ToolAgentServer);
                //var currentDirectory = Directory.GetCurrentDirectory();
                //var uploadsFolder = Path.Combine(currentDirectory, "toolAgentFiles", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // 构建完整的文件路径并保存
                var filePathInfo = new FileInfo(Path.Combine(uploadsFolder, fileName));
                string filePath = filePathInfo.FullName;
                Console.WriteLine(filePath);
                // 确保文件的上级目录存在，如果不存在则创建
                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }

                //System.IO.File.WriteAllBytes(filePath, fileBytes);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                //File.WriteAllBytes(filePath, fileBytes);

                //return Ok($"文件 '{file.FileName}' 已成功创建。");
                Console.WriteLine($"文件 '{fileName}' 已成功创建。");
                return filePath;

            }
            catch (FormatException fe)
            {
                Console.WriteLine($"Base64解码错误: {fe.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(StatusCodes.Status500InternalServerError.ToString() + $"文件保存失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                //Console.WriteLine($"文件{filePath}已成功删除！");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"删除文件{filePath}时出错: {ex.Message}");
            }
        }
    }
}
