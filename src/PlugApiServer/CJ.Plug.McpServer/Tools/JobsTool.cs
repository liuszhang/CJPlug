using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace CJ.Plug.McpServer.Tools
{
    [McpServerToolType]
    public sealed class JobsTool
    {

        /// <summary>
        /// 获取系统中所有的作业信息
        /// </summary>
        /// <returns></returns>
        //[McpServerTool, Description("获取系统中所有的作业信息")]
        public static async Task<string> ListJob()
        {
            var httpClient = new HttpClient();
            var json = await httpClient.ReadJsonDocumentAsync("http://localhost:6661/api/Job/getJobs");

            return $"获取到的所有作业数据：{JsonSerializer.Serialize(json)}";
        }
    }
}
