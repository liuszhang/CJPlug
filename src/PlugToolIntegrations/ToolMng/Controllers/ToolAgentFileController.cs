using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Xml.Linq;
using ToolMng.Models;

namespace ToolMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolAgentFileController : ControllerBase
    {
        [HttpGet("GetFilesStructure")]
        public IActionResult GetFilesStructure(string rootPath = "C:\\tmp")
        {
            try
            {
                var root = new FileSystemItem(rootPath,rootPath, true);
                BuildFileSystemTree(rootPath, root);
                return Ok(root);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("GetCurrentFiles")]
        public IActionResult GetCurrentFiles(string rootPath = "C:")
        {
            if (rootPath == null)
            {
                rootPath = "C:\\tmp";
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
                var root = new FileSystemItem("all","图站地址："+ ipv4.ToString(), true);
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
                    var driverItem= new FileSystemItem(d.Name, d.Name, Directory.Exists(d.Name));
                    var subItems = Directory.GetFileSystemEntries(d.Name);
                    //Console.WriteLine(subItems);
                    foreach (var subItem in subItems)
                    {
                        var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                        //Console.WriteLine(subItem);
                        driverItem.Children.Add(item);
                    }
                    //var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                    root.Children.Add(driverItem);

                }
                return Ok(root);
            }
            try
            {
                var root = new FileSystemItem(rootPath,rootPath, true);
                var subItems = Directory.GetFileSystemEntries(rootPath);
                foreach (var subItem in subItems)
                {
                    var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                    root.Children.Add(item);                    
                }
                return Ok(root);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private void BuildFileSystemTree(string path, FileSystemItem node)
        {
            try
            {
                var subItems = Directory.GetFileSystemEntries(path);
                foreach (var subItem in subItems)
                {
                    var item = new FileSystemItem(Path.GetFullPath(subItem), Path.GetFileName(subItem), Directory.Exists(subItem));
                    node.Children.Add(item);
                    if (item.IsDirectory)
                    {
                        BuildFileSystemTree(subItem, item);
                    }
                }
            }
    
            catch
            {
                // 处理异常，如权限问题等
            }
        }



        [HttpPost("UploadFile")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                return BadRequest("缺少必要的文件内容或文件名");
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
                var filePathInfo = new FileInfo(Path.Combine(uploadsFolder, file.FileName));
                string filePath = filePathInfo.FullName;
                //System.IO.File.Delete(filePath);
                //Console.WriteLine("fullpath:"+filePath);
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
                return Ok(filePath);
            }
            catch (FormatException fe)
            {
                return BadRequest($"Base64解码错误: {fe.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"文件保存失败: {ex.Message}");
            }
        }
    }
}
