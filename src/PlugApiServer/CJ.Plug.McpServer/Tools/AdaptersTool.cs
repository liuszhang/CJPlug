using CJ.Plug.Models.Shared;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace CJ.Plug.McpServer.Tools
{
    [McpServerToolType]
    public sealed class AdaptersTool
    {
        //[McpServerTool, Description("获取系统中所有的工具适配器列表及其具体信息，信息中包含了工具适配器ID，可用于工具适配器执行")]
        public static async Task<string> ListAdapter()
        {
            var httpClient = new HttpClient();
            var json = await httpClient.ReadJsonDocumentAsync($"{GlobalData.MainApiServer}/api/mcp/getActiveTools");

            return $"获取到的工具适配器数据：{JsonSerializer.Serialize(json)}";
        }


        /// <summary>
        /// 根据工具适配器的sourcePlugId进行工具适配器执行
        /// </summary>
        /// <param name="sourcePlugId">工具适配器的sourcePlugId</param>
        /// <returns></returns>
        //[McpServerTool, Description("根据工具适配器的sourcePlugId进行工具适配器执行")]
        public static async Task<string> ExecuteAdapter(string sourcePlugId)
        {
            var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync($"{GlobalData.MainApiServer}/api/plug/executePlugByDefinitionId/{sourcePlugId}");

            return $"启动工具适配器的结果：{json}";
        }

        //[McpServerTool, Description("接收文件输入的工具执行")]
        public static async Task<string> ExecuteFileAdapter(string fileStream)
        {
            //var httpClient = new HttpClient();
            //var json = await httpClient.GetStringAsync($"http://localhost:6661/api/plug/executePlugByDefinitionId/{adapterId}");

            return $"获得的文件长度：{fileStream.Length}";
        }
    }
}
