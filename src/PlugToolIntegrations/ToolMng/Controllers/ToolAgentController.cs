using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ToolMng.Contracts;
using ToolMng.Models;

namespace ToolMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolAgentController : ControllerBase
    {
        private IToolAgentServices toolAgentServices;
        private IToolAgentNXServices toolAgentNXServices;

        public ToolAgentController(IToolAgentServices toolAgentServices, IToolAgentNXServices toolAgentNXServices)
        {
            this.toolAgentServices = toolAgentServices;
            this.toolAgentNXServices = toolAgentNXServices; 
        }

        /// <summary>
        /// 工具执行主服务，通过命令字符串方式传递
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("execute")]
        public async Task<IActionResult> ToolExecute([FromBody] string command = "start notepad")
        {            
            try
            {
                Console.WriteLine($"\n--------------------\n工具调用请求命令体为：{command}\n--------------------\n");
                //这里需要处理路径中的斜杠以免传递过程中的转义问题
                command = command.Replace("\\", "\\\\");
                ToolAgentCommandModel toolAgentCommandModel = new ToolAgentCommandModel();
                JObject jsonObject = JObject.Parse(command);
                //Console.WriteLine(jsonObject["RequestId"]);

                toolAgentCommandModel.RequestId = jsonObject["RequestId"] != null ? jsonObject["RequestId"].ToString() : "";
                toolAgentCommandModel.ExecuteCommand = jsonObject["ExecuteCommand"] != null ? jsonObject["ExecuteCommand"].ToString() : "";
                toolAgentCommandModel.ToolName = jsonObject["ToolName"] != null ? jsonObject["ToolName"].ToString() : "";
                //Console.WriteLine(toolAgentCommandModel.ToolName);            
                toolAgentCommandModel.ExecuteParameters = ((JArray)jsonObject["ExecuteParameters"]).ToObject<string[]>();
                //Console.WriteLine();

                string result = await RunCmdAsync(toolAgentCommandModel);
                //Console.WriteLine(result);               

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem($"执行命令时发生错误: {ex.Message}");
            }
        }

        [HttpPost("ExecuteFromRequest")]
        public async Task<IActionResult> ToolExecuteFromFormData([FromBody] ToolAgentCommandModel toolAgentCommandModel)
        {
            //Console.WriteLine(toolAgentCommandModel.RequestId);
            try
            {
                string result = await RunCmdAsync(toolAgentCommandModel);
                //Console.WriteLine(result);
                return Ok(result);
                //var currentDirectory = Directory.GetCurrentDirectory();
                //var batsFolder = Path.Combine(currentDirectory, "toolAgentFiles", "bats");
                //if (!Directory.Exists(batsFolder))
                //    Directory.CreateDirectory(batsFolder);
                //// 指定生成的BAT文件路径
                //string batFilePath = Path.Combine(batsFolder, RequestId + ".bat");
                //// 检查文件是否存在
                //if (!System.IO.File.Exists(batFilePath))
                //{
                //    //构建接受多个参数的BAT脚本
                //    var content = WriteContentToBat(ToolName, ExecuteParameters);
                //    System.IO.File.WriteAllText(batFilePath, content);
                //    //System.IO.File.WriteAllText(batFilePath, ToolName);
                //    Console.WriteLine($"BAT文件'{batFilePath}'已创建。");
                //}
                //else
                //{
                //    var content = WriteContentToBat(ToolName, ExecuteParameters);
                //    System.IO.File.WriteAllText(batFilePath, content);
                //    //System.IO.File.WriteAllText(batFilePath, ToolName);
                //    Console.WriteLine($"BAT文件'{batFilePath}'已更新。");
                //}

                //// 执行命令并获取输出
                //string result = await RunBatAsync(batFilePath);
                //Console.WriteLine(result);
                //return Ok(result);
                //return Ok();
            }
            catch (Exception ex)
            {
                return Problem($"执行命令时发生错误: {ex.Message}");
            }
        }

        private string WriteContentToBat(string exePath, string[] args)
        {
            // 定义脚本内容的各个部分
            //string commandPath = @"C:\MyPro\MyElsa\ToolMng\toolAgentFiles\bats\a b\ToolExecuteTest.exe";
            //string[] parameters = new string[]
            //{
            //"a",
            //"b",
            //"c",
            //"--a",
            //"1111",
            //"--b",
            //"2222",
            //"--c",
            //"3333"
            //};
            string commandPath = exePath;
            string[] parameters = args;



            // 构建完整的bat命令字符串
            StringBuilder batContentBuilder = new StringBuilder();
            batContentBuilder.AppendLine("@echo off");
            //添加支持中文
            batContentBuilder.AppendLine("chcp 65001");          
            batContentBuilder.AppendLine("setlocal enabledelayedexpansion");
            batContentBuilder.AppendFormat("set \"command={0}\"\n", commandPath);

            // 添加参数到bat命令
            batContentBuilder.Append("set \"fullCommand=start \"\" ").Append("\"!command!\"");
            foreach (var param in parameters)
            {
                batContentBuilder.Append(" ").Append("\"" + param + "\"");
            }
            batContentBuilder.AppendLine("\"");
            batContentBuilder.AppendLine("echo !fullCommand!");
            batContentBuilder.AppendLine("call !fullCommand!");
            batContentBuilder.AppendLine("endlocal");

            return batContentBuilder.ToString();
        }

        /// <summary>
        /// 执行CMD命令
        /// </summary>
        /// <param name="toolAgentCommandModel"></param>
        /// <returns></returns>
        private async Task<string> RunCmdAsync(ToolAgentCommandModel toolAgentCommandModel)
        {

            using (var process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = toolAgentCommandModel.ToolName;
                    process.StartInfo.Arguments = toolAgentCommandModel.ExecuteCommand;
                    process.StartInfo.Arguments += String.Join(' ', toolAgentCommandModel.ExecuteParameters);
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    Console.WriteLine(process.StartInfo.Arguments);
                    process.Start();
                    Console.WriteLine($"子进程 ID: {process.Id}");
                    Console.WriteLine($"子进程启动设备: {process.MachineName}");
                    Console.WriteLine($"子进程启动时间: {process.StartTime}");
                    Console.WriteLine($"子进程主模块: {(process.MainModule != null ? process.MainModule.ModuleName : "尚未加载主模块")}");


                    string output = await process.StandardOutput.ReadToEndAsync();
                    //Console.WriteLine(output);
                    //string output = "";
                    //// 逐行读取输出
                    //while (!process.StandardOutput.EndOfStream)
                    //{
                    //    var line = await process.StandardOutput.ReadLineAsync();
                    //    Console.WriteLine(line);
                    //    output += line;
                    //}
                    //string output = await process.StandardOutput.ReadToEndAsync();

                    //await process.WaitForExitAsync();                    
                    Console.WriteLine($"子进程结束时间: {process.ExitTime}");
                    Console.WriteLine($"子进程退出码: {process.ExitCode}");

                    Console.WriteLine($"子进程执行结果: {output}");
                    return output;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法启动子进程: {ex.Message}");
                    return ex.ToString();
                }
            }
        }


        private async Task<string> RunBatAsync(string bat)
        {
            try
            {
                // 创建一个新的进程启动信息
                ProcessStartInfo startInfo = new ProcessStartInfo();

                // 设置要执行的文件路径
                startInfo.FileName = bat;

                // 设置是否使用操作系统shell启动进程
                startInfo.UseShellExecute = false; // 不通过shell，以便重定向输出
                startInfo.RedirectStandardOutput = true; // 重定向标准输出
                startInfo.CreateNoWindow = true; // 不显示命令行窗口

                // 创建进程
                Process process = new Process();

                // 关联启动信息
                process.StartInfo = startInfo;

                // 启动进程
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                Console.WriteLine(output);
                // 等待进程自然结束，这句是可选的，取决于你是否需要等待脚本执行完毕
                process.WaitForExit();

                Console.WriteLine("BAT脚本已执行完毕。");
                //System.IO.File.Delete(bat);
                return "结果：" + output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行BAT脚本时出错: {ex.Message}");
                return ex.Message;
            }
        }


        [HttpPost("executePython")]
        public async Task<IActionResult> PythonFileExecute([FromBody] string filePath)
        {
            try
            {                
                ToolAgentCommandModel toolAgentCommandModel = new ToolAgentCommandModel();

                //toolAgentCommandModel.RequestId = jsonObject["RequestId"] != null ? jsonObject["RequestId"].ToString() : "";
                toolAgentCommandModel.ExecuteCommand = "";
                toolAgentCommandModel.ToolName = "python";
                //Console.WriteLine(toolAgentCommandModel.ToolName);            
                toolAgentCommandModel.ExecuteParameters = [filePath];

                string result = await RunCmdAsync(toolAgentCommandModel);
                Console.WriteLine(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem($"执行命令时发生错误: {ex.Message}");
            }
        }

        [HttpPost("executePythonCode")]
        public async Task<IActionResult> PythonCodeExecute([FromBody] string? pythonCode)
        {
            if (pythonCode == null)
            {
                return Problem($"请传递python代码");
            }
            //Console.WriteLine(pythonCode);
            try
            {
                ToolAgentCommandModel toolAgentCommandModel = new ToolAgentCommandModel();
                toolAgentCommandModel.ExecuteCommand = "-c ";
                toolAgentCommandModel.ToolName = "python";
                //Console.WriteLine(toolAgentCommandModel.ToolName);            
                toolAgentCommandModel.ExecuteParameters = ["\"\n"+pythonCode+"\n\""];

                string result = await RunCmdAsync(toolAgentCommandModel);
                //Console.WriteLine(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem($"执行命令时发生错误: {ex.Message}");
            }
        }

        [HttpPost("executePythonCodeAsFile")]
        public async Task<IActionResult> PythonCodeExecuteAsFile([FromBody] string? pythonCode)
        {
            if (pythonCode == null)
            {
                return Problem($"请传递python代码");
            }
            //Console.WriteLine(pythonCode);
            string fileName=Guid.NewGuid().ToString()+".py";
            byte[] byteData = Encoding.UTF8.GetBytes(pythonCode);
            using MemoryStream stream = new MemoryStream(byteData);


            var filePath = await toolAgentServices.UploadFileStreamToToolAgentFileServerAsync(stream, fileName);
            try
            {
                ToolAgentCommandModel toolAgentCommandModel = new ToolAgentCommandModel();
                toolAgentCommandModel.ExecuteCommand = "";
                toolAgentCommandModel.ToolName = "python";
                //Console.WriteLine(toolAgentCommandModel.ToolName);            
                toolAgentCommandModel.ExecuteParameters = [filePath];

                string result = await RunCmdAsync(toolAgentCommandModel);
                //Console.WriteLine(result);
                await toolAgentServices.DeleteFile(filePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem($"执行命令时发生错误: {ex.Message}");
            }
        }







        /// <summary>
        /// 获取NX模型的参数值
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost("GetNXParameters")]
        public async Task<IActionResult> GetNXParametersFromNXFile([FromForm] IFormFile file)
        {
            try
            {
                var filePath = await toolAgentServices.UploadFileToToolAgentFileServerAsync(file);
                if (filePath == null)
                {
                    return BadRequest("上传文件失败");
                }
                Console.WriteLine($"【获取参数】上传至图站的临时文件名为：{filePath}");
                //接下来调用NX工具读取模型参数
                var output = await toolAgentNXServices.GetNXParametersFromNXFileAsync(filePath);
                if (output == null)
                {
                    return NotFound();
                }
                //Console.WriteLine(output);
                return Ok(output);
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

        /// <summary>
        /// 设置NX模型参数值
        /// </summary>
        /// <param name="file">NX文件</param>
        /// <param name="newParameters">更新的参数表，标准格式为：a=1,b=2,c=3</param>
        /// <returns>更新后的NX模型</returns>
        [HttpPost("SetNXParameters")]
        public async Task<IActionResult> SetNXParametersToNXFile([FromForm] IFormFile file, [FromForm] string newParameters)
        {
            try
            {
                var filePath = await toolAgentServices.UploadFileToToolAgentFileServerAsync(file);
                if (filePath == null)
                {
                    return BadRequest("上传文件失败");
                }
                Console.WriteLine($"【设置参数】上传至图站的临时文件名为：{filePath}");
                //接下来调用NX工具设置模型参数
                var output = await toolAgentNXServices.SetNXParametersToNXFileAsync(filePath, newParameters);
                if (output == false)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                var fileStream = await System.IO.File.ReadAllBytesAsync(filePath);
                await toolAgentServices.DeleteFile(filePath);
                //读取文件内容并以数据流返回
                //return File(fileStream, "application/stream", file.Name);
                return File(fileStream, "application/stream", file.Name);
                //return Ok(fileStream);
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

        /// <summary>
        /// 导出NX模型至STL格式
        /// </summary>
        /// <param name="NXFile"></param>
        /// <returns></returns>
        [HttpPost("ExportNXToStl")]
        public async Task<IActionResult> ExportNXFileToStlFile([FromForm] IFormFile NXFile)
        {
            try
            {
                var nxFilePath = await toolAgentServices.UploadFileToToolAgentFileServerAsync(NXFile);
                if (nxFilePath == null)
                {
                    return BadRequest("上传文件失败");
                }

                string StlPath= Path.ChangeExtension(nxFilePath, "stl");
                //Console.WriteLine(StlPath);
                //接下来调用NX工具导出STL
                var outputStlPath = await toolAgentNXServices.ExportNXFileToStlFileAsync(nxFilePath, StlPath);
                if (outputStlPath == null)
                {
                    Console.WriteLine("导出失败");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                var filePathInfo = new FileInfo(outputStlPath);
                string fullFilePath = filePathInfo.FullName;
                var fileStream =await System.IO.File.ReadAllBytesAsync(fullFilePath);
                await toolAgentServices.DeleteFile(fullFilePath);
                //读取文件内容并以数据流返回
                //string newStlPath= Path.ChangeExtension(NXFile.FileName, "stl");
                //return File(fileStream, "application/stream", newStlPath);
                return File(fileStream, "application/stream", filePathInfo.Name);
                //如果是返回物理文件，使用下面的内容
                //return PhysicalFile(fullFilePath, "text/plain", filePathInfo.Name);


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
