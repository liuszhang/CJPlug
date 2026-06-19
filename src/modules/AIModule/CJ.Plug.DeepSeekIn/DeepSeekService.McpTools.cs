using OllamaSharp;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace CJ.Plug.DeekSeekIn
{
    public partial class DeepSeekService : IDeepSeekService
    {

        // ---- 带 MCP Server 配置的重载方法 ----

        /// <summary>
        /// 从 MCP Server 连接字符串加载工具列表。返回 null 表示未配置或加载失败。
        /// 自行实现 MCP SSE 客户端，绕过 OllamaSharp GetFromMcpServers 的 JSON Schema type 数组反序列化问题。
        /// </summary>
        private static async Task<IList<OllamaSharp.Models.Chat.Tool>?> LoadMcpToolsAsync(string? mcpConnectionString)
        {
            Console.WriteLine($"[DeepSeek] LoadMcpToolsAsync called with mcpConnectionString='{(string.IsNullOrEmpty(mcpConnectionString) ? "(null/empty)" : mcpConnectionString)}'");
            if (string.IsNullOrEmpty(mcpConnectionString))
                return null;

            try
            {
                var baseUrl = mcpConnectionString.TrimEnd('/');
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // Step 1: Connect to SSE endpoint to get the message endpoint
                Console.WriteLine($"[DeepSeek] MCP SSE: connecting to {baseUrl}/sse");
                using var sseResponse = await httpClient.GetAsync($"{baseUrl}/sse", HttpCompletionOption.ResponseHeadersRead, cts.Token);
                sseResponse.EnsureSuccessStatusCode();

                string? messageEndpoint = null;
                using var sseStream = await sseResponse.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new System.IO.StreamReader(sseStream);

                // Read SSE events until we get the endpoint
                string? currentEvent = null;
                string? dataBuffer = null;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line == null) break; // EOF

                    if (line.StartsWith("event: "))
                        currentEvent = line[7..];
                    else if (line.StartsWith("data: "))
                        dataBuffer = line[6..];
                    else if (line == "" && currentEvent != null)
                    {
                        if (currentEvent == "endpoint" && dataBuffer != null)
                        {
                            messageEndpoint = dataBuffer;
                            Console.WriteLine($"[DeepSeek] MCP SSE: got endpoint = {messageEndpoint}");
                            break;
                        }
                        currentEvent = null;
                        dataBuffer = null;
                    }
                }

                if (messageEndpoint == null)
                {
                    Console.WriteLine($"[DeepSeek] MCP SSE: no endpoint event received");
                    return null;
                }

                // Resolve message endpoint URL (could be relative or absolute)
                var endpointUrl = messageEndpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? messageEndpoint
                    : $"{baseUrl}{messageEndpoint}";

                // Step 2: Send tools/list JSON-RPC request
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "tools/list"
                };
                var rpcJson = JsonSerializer.Serialize(rpcRequest);
                Console.WriteLine($"[DeepSeek] MCP: sending tools/list to {endpointUrl}");
                using var postContent = new StringContent(rpcJson, Encoding.UTF8, "application/json");
                var rpcResponse = await httpClient.PostAsync(endpointUrl, postContent, cts.Token);
                rpcResponse.EnsureSuccessStatusCode();

                var responseJson = await rpcResponse.Content.ReadAsStringAsync(cts.Token);
                Console.WriteLine($"[DeepSeek] MCP: received tools/list response ({responseJson.Length} chars)");

                // Step 3: Preprocess JSON to fix type arrays in JSON Schema,
                // then deserialize into OllamaSharp Tool objects
                var responseNode = JsonNode.Parse(responseJson);
                var resultNode = responseNode?["result"];
                var toolsArray = resultNode?["tools"]?.AsArray();
                if (toolsArray == null || toolsArray.Count == 0)
                {
                    Console.WriteLine($"[DeepSeek] MCP: no tools in response");
                    return Array.Empty<OllamaSharp.Models.Chat.Tool>();
                }

                var toolList = new List<OllamaSharp.Models.Chat.Tool>();
                foreach (var toolNode in toolsArray)
                {
                    var name = toolNode?["name"]?.GetValue<string>() ?? "unknown";
                    var description = toolNode?["description"]?.GetValue<string>() ?? "";
                    var inputSchema = toolNode?["inputSchema"];

                    // Fix JSON Schema type arrays recursively before serialization
                    var fixedSchema = McpFixJsonSchemaTypes(inputSchema);

                    // Build Parameters: the fixed JSON Schema as JsonElement
                    string fixedSchemaJson;
                    if (fixedSchema != null)
                        fixedSchemaJson = fixedSchema.ToJsonString();
                    else
                        fixedSchemaJson = "{}";

                    Console.WriteLine($"[DeepSeek] MCP tool[{toolList.Count}]: Name={name}, Schema={fixedSchemaJson[..Math.Min(200, fixedSchemaJson.Length)]}");

                    var parametersObj = JsonSerializer.Deserialize<OllamaSharp.Models.Chat.Parameters>(fixedSchemaJson);
                    var tool = new OllamaSharp.Models.Chat.Tool
                    {
                        Type = "function",
                        Function = new OllamaSharp.Models.Chat.Function
                        {
                            Name = name,
                            Description = description,
                            Parameters = parametersObj
                        }
                    };
                    toolList.Add(tool);
                }

                Console.WriteLine($"[DeepSeek] Loaded {toolList.Count} tools from MCP Server: {mcpConnectionString}");
                for (int i = 0; i < toolList.Count; i++)
                {
                    var tool = toolList[i];
                    var func = tool.Function;
                    Console.WriteLine($"[DeepSeek]   tool[{i}]: Name={func?.Name}, Description={(func?.Description == null ? "(null)" : func.Description[..Math.Min(60, func.Description.Length)])}, ParametersNull={func?.Parameters == null}, ToolType={tool.Type}");
                }
                return toolList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeepSeek] Failed to load MCP tools from {mcpConnectionString}: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[DeepSeek]   InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                return null;
            }
        }

        /// <summary>
        /// Recursively walk a JSON Schema node and convert any array-typed "type" field
        /// (e.g. ["string"], ["string","null"]) into a single string (taking the first non-null type).
        /// Also handles "items", "additionalProperties", "prefixItems" etc. that may contain nested schemas.
        /// </summary>
        private static JsonNode? McpFixJsonSchemaTypes(JsonNode? node)
        {
            if (node == null) return null;

            if (node is JsonObject obj)
            {
                // Fix "type" field if it's an array
                if (obj.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonArray typeArr)
                {
                    // Take the first non-"null" type, or the first type if all are "null"
                    string? chosen = null;
                    foreach (var t in typeArr)
                    {
                        var s = t?.GetValue<string>();
                        if (s != null && !s.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            chosen = s;
                            break;
                        }
                    }
                    chosen ??= typeArr[0]?.GetValue<string>() ?? "string";
                    obj["type"] = chosen;
                }

                // Recursively fix nested schema containers
                foreach (var key in new[] { "properties", "items", "additionalProperties", "prefixItems", "contains", "propertyNames", "if", "then", "else", "not" })
                {
                    if (obj.TryGetPropertyValue(key, out var child))
                        obj[key] = McpFixJsonSchemaTypes(child);
                }

                // Fix array-valued schema nodes: "anyOf", "allOf", "oneOf"
                foreach (var key in new[] { "anyOf", "allOf", "oneOf" })
                {
                    if (obj.TryGetPropertyValue(key, out var child) && child is JsonArray arr)
                    {
                        var fixedArr = new JsonArray();
                        foreach (var item in arr)
                            fixedArr.Add(McpFixJsonSchemaTypes(item));
                        obj[key] = fixedArr;
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                var fixedArr = new JsonArray();
                foreach (var item in arr)
                    fixedArr.Add(McpFixJsonSchemaTypes(item));
                return fixedArr;
            }

            return node;
        }
    }
}
