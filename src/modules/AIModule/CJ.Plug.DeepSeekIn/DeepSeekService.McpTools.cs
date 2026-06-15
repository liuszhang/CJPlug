using OllamaSharp;
using OllamaSharp.ModelContextProtocol.Server;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
namespace CJ.Plug.DeekSeekIn
{
    public partial class DeepSeekService : IDeepSeekService
    {

        // ---- 带 MCP Server 配置的重载方法 ----

        /// <summary>
        /// 从 MCP Server 连接字符串加载工具列表。返回 null 表示未配置或加载失败。
        /// </summary>
        private static async Task<IList<OllamaSharp.Models.Chat.Tool>?> LoadMcpToolsAsync(string? mcpConnectionString)
        {
            if (string.IsNullOrEmpty(mcpConnectionString))
                return null;

            try
            {
                var config = new McpServerConfiguration
                {
                    Name = "mcp-server",
                    Command = mcpConnectionString,
                    TransportType = McpServerTransportType.Sse
                };
                var toolList = await OllamaSharp.ModelContextProtocol.Tools.GetFromMcpServers(config);
                Console.WriteLine($"[DeepSeek] Loaded {toolList.Length} tools from MCP Server: {mcpConnectionString}");
                foreach (var tool in toolList)
                    Console.WriteLine($"[DeepSeek]   tool: {tool.Function?.Name}");
                return toolList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeepSeek] Failed to load MCP tools from {mcpConnectionString}: {ex.Message}");
                return null;
            }
        }
    }
}
