using Microsoft.AspNetCore.Mvc;
using System.IO;
using ToolMng.Models;

namespace ToolMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        //[HttpPost("UploadFile"), Consumes("application/json")]
        //public async Task<IActionResult> UploadFile([FromBody] UploadModel model)
        //{
        //    if (model == null || string.IsNullOrEmpty(model.FileContent) || string.IsNullOrEmpty(model.FileName))
        //    {
        //        return BadRequest("缺少必要的文件内容或文件名");
        //    }
        //    try
        //    {
        //        // 解码Base64字符串为字节数组
        //        byte[] fileBytes = Convert.FromBase64String(model.FileContent);

        //        // 确保目录存在
        //        var uploadsFolder = Path.Combine(@"F:\OneDrive\桌面\tmp2", "toolAgentFiles", "uploads");
        //        if (!Directory.Exists(uploadsFolder))
        //            Directory.CreateDirectory(uploadsFolder);
        //        //Directory.CreateDirectory(_uploadFolderPath);

        //        // 构建完整的文件路径并保存
        //        string filePath = Path.Combine(uploadsFolder, model.FileName);
        //        Console.WriteLine(filePath);
        //        System.IO.File.WriteAllBytes(filePath, fileBytes);
        //        //using (var stream = new FileStream(filePath, FileMode.Create))
        //        //{
        //        //    File.WriteAllBytes(filePath, fileBytes);
        //        //}
        //        //File.WriteAllBytes(filePath, fileBytes);

        //        return Ok($"文件 '{model.FileName}' 已成功保存。");
        //    }
        //    catch (FormatException fe)
        //    {
        //        return BadRequest($"Base64解码错误: {fe.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, $"文件保存失败: {ex.Message}");
        //    }
        //    //var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "toolAgentFiles", "uploads");
            

            
            

        //    //return Ok($"File '{file.FileName}' uploaded successfully.");
        //}



        [HttpPost("UploadFile")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                return BadRequest("缺少必要的文件内容或文件名");
            }
            try
            {
                // 解码Base64字符串为字节数组
                //byte[] fileBytes = Convert.FromBase64String(model.FileContent);

                // 确保目录存在
                //var uploadsFolder = Path.Combine(@"F:\OneDrive\桌面\tmp2", "toolAgentFiles", "uploads");
                var currentDirectory = Directory.GetCurrentDirectory();
                //var uploadsFolder = Path.Combine(currentDirectory, "toolAgentFiles", "uploads");
                var uploadsFolder = Path.Combine(StaticData.ToolAgentServer, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                //Directory.CreateDirectory(_uploadFolderPath);

                // 构建完整的文件路径并保存
                var filePathInfo =new FileInfo(Path.Combine(uploadsFolder, file.FileName));
                string filePath=filePathInfo.FullName;
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

                return Ok($"文件 '{file.FileName}' 已成功保存。");
            }
            catch (FormatException fe)
            {
                return BadRequest($"Base64解码错误: {fe.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"文件保存失败: {ex.Message}");
            }
            //var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "toolAgentFiles", "uploads");





            //return Ok($"File '{file.FileName}' uploaded successfully.");
        }



        public class UploadModel
        {
            public string FileContent { get; set; }
            public string FileName { get; set; }
        }
    }
}
