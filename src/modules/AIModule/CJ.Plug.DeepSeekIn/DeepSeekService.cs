using OllamaSharp;
using OllamaSharp.ModelContextProtocol.Server;
using System.Threading.Channels;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace CJ.Plug.DeekSeekIn
{
    public class DeepSeekService : IDeepSeekService
    {
        //public readonly MainApiClient MainApiclient;
        private readonly Uri _modelEndpoint = new Uri("http://localhost:11434");
        //private readonly string _modelName = "deepseek-r1:1.5b";
        //private readonly string _modelName = "qwen3:1.7b";
        private readonly string _modelName = "qwen3:4b";

        public async IAsyncEnumerable<string> Ask(string Question)
        {
            if (string.IsNullOrEmpty(Question))
            {
                yield return "请输入您的问题？";
                yield break;
            }

        
            var chatClient = new OllamaApiClient(_modelEndpoint, _modelName);
            var chat = new Chat(chatClient);
            chat.AllowRecursiveToolCalls = true;
            chat.Think = true;

            // Channel to merge think events and answer tokens
            var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            chat.OnThink += (s, e) =>
            {
                // push think content to the output channel so the caller can receive it
                output.Writer.TryWrite($"{e}");
            };

            Console.WriteLine("start===>问答开始");

            // Start a task to read answer tokens and write them to the channel
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var answerToken in chat.SendAsync(Question))
                    {
                        await output.Writer.WriteAsync(answerToken.ToString());
                    }
                }
                catch (Exception ex)
                {
                    // propagate exceptions to the reader
                    output.Writer.TryWrite($"[Error] {ex.Message}");
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            // Stream merged results to the caller
            await foreach (var item in output.Reader.ReadAllAsync())
            {
                yield return item;
            }

            Console.WriteLine("end===>问答结束");
        }

        public async IAsyncEnumerable<string> AskWithTool(string Question)
        {
            if (string.IsNullOrEmpty(Question))
            {
                yield return "请输入您的问题？";
                yield break;
            }

            var chatClient = new OllamaApiClient(_modelEndpoint, _modelName);
            var chat = new Chat(chatClient);
            chat.AllowRecursiveToolCalls = true;
            chat.Think = true;
            chat.OnThink += (s, e) =>
            {
                //Console.WriteLine($"[DeepSeekService] Ask OnThinking: {e}");
            };
            chat.OnToolCall += (s, e) =>
            {
                Console.WriteLine($"[DeepSeekService] Ask OnToolCall: {e.Function?.Name}");
            };
            Console.WriteLine("start===>问答开始");

            var config = new McpServerConfiguration
            {
                Name = "test",
                Command = "http://localhost:3001",
                //等待ollama支持 Streamable HTTP ^ ^
                TransportType = McpServerTransportType.Sse
            };

            var toolList = await OllamaSharp.ModelContextProtocol.Tools.GetFromMcpServers(config);

            foreach (var tool in toolList)
            {
                Console.WriteLine($"get tool:{tool.Function?.Name}");
            }

            // Channel to merge think events and answer tokens
            var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            chat.OnThink += (s, e) =>
            {
                // push think content to the output channel so the caller can receive it
                output.Writer.TryWrite($"{e}");
            };

            Console.WriteLine("start===>问答开始");

            // Start a task to read answer tokens and write them to the channel
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var answerToken in chat.SendAsync(Question,toolList))
                    {
                        await output.Writer.WriteAsync(answerToken.ToString());
                    }
                }
                catch (Exception ex)
                {
                    // propagate exceptions to the reader
                    output.Writer.TryWrite($"[Error] {ex.Message}");
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            // Stream merged results to the caller
            await foreach (var item in output.Reader.ReadAllAsync())
            {
                yield return item;
            }

            Console.WriteLine("end===>问答结束");

            
        }
        /// <summary>
        /// Call OpenRouter chat completions API and return raw JSON response.
        /// </summary>
        /// <param name="messages">Sequence of (role, content) tuples to send.</param>
        /// <param name="apiKey">OpenRouter API key (Bearer).</param>
        /// <param name="model">Model name, defaults to "openrouter/free".</param>
        /// <param name="maxTokens">Maximum tokens to generate.</param>
        /// <param name="temperature">Sampling temperature.</param>
        public async Task<string> CallOpenRouterChatAsync(IEnumerable<(string role, string content)> messages, string apiKey, string model = "openrouter/free", int maxTokens = 150, double temperature = 0.7)
        {
            apiKey = "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";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("apiKey is required", nameof(apiKey));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                messages = messages.Select(m => new { role = m.role, content = m.content }),
                model = model,
                //max_tokens = maxTokens,
                temperature = temperature
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(responseText);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OpenRouter API returned {(int)response.StatusCode}: {responseText}");
            }

            return responseText;
        }

        /// <summary>
        /// Parse a OpenRouter chat completion JSON response and stream the assistant message content in chunks.
        /// </summary>
        /// <param name="json">Raw JSON response from OpenRouter.</param>
        /// <param name="chunkSize">Maximum characters per streamed chunk.</param>
        public async IAsyncEnumerable<string> ParseAndStreamOpenRouterResponseAsync(string json, int chunkSize = 64)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                yield break;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var message))
                    {
                        if (message.TryGetProperty("content", out var contentEl))
                        {
                            var content = contentEl.GetString() ?? string.Empty;
                            // stream content in chunks
                            for (int i = 0; i < content.Length; i += chunkSize)
                            {
                                var len = Math.Min(chunkSize, content.Length - i);
                                yield return content.Substring(i, len);
                                await Task.Yield();
                            }
                        }

                        // also stream optional reasoning (if present)
                        if (message.TryGetProperty("reasoning", out var reasoningEl))
                        {
                            var reasoning = reasoningEl.GetString();
                            if (!string.IsNullOrEmpty(reasoning))
                            {
                                yield return "[reasoning_start]";
                                await Task.Yield();
                                for (int i = 0; i < reasoning.Length; i += chunkSize)
                                {
                                    var len = Math.Min(chunkSize, reasoning.Length - i);
                                    yield return reasoning.Substring(i, len);
                                    await Task.Yield();
                                }
                                yield return "[reasoning_end]";
                                await Task.Yield();
                            }
                        }
                    }
                }
            }
            else
            {
                // fallback: stream raw json
                for (int i = 0; i < json.Length; i += chunkSize)
                {
                    var len = Math.Min(chunkSize, json.Length - i);
                    yield return json.Substring(i, len);
                    await Task.Yield();
                }
            }
        }


        /// <summary>
        /// Build messages from a single content string (system empty + user content), call OpenRouter chat completions
        /// and stream back only the `reasoning` parts from the response in chunks.
        /// </summary>
        /// <param name="content">The user content to send (the question or prompt).</param>
        /// <param name="apiKey">OpenRouter API key (Bearer).</param>
        /// <param name="model">Model name to request.</param>
        /// <param name="chunkSize">Maximum characters per streamed chunk.</param>
        public async IAsyncEnumerable<string> StreamReasoningFromContentAsync(string content)
        {
            string apiKey = "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";
            string model = "openrouter/free";
            int chunkSize = 64;
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }

            // build messages as in the curl example: empty system + user content
            var messages = new List<(string role, string content)>
            {
                ("system", ""),
                ("user", content)
            };

            string jsonResponse;
            try
            {
                jsonResponse = await CallOpenRouterChatAsync(messages, apiKey, model);
            }
            catch (Exception ex)
            {
                //yield return $"[Error] {ex.Message}";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                yield break;
            }

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var foundAny = false;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    // try message.reasoning first
                    if (choice.TryGetProperty("message", out var messageEl))
                    {
                        if (messageEl.TryGetProperty("content", out var reasoningEl) && reasoningEl.ValueKind == JsonValueKind.String)
                        {
                            Console.WriteLine($"[Debug] Found reasoning in message.content: {reasoningEl.GetString()}");
                            var reasoning = reasoningEl.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(reasoning))
                            {
                                foundAny = true;
                                for (int i = 0; i < reasoning.Length; i += chunkSize)
                                {
                                    var len = Math.Min(chunkSize, reasoning.Length - i);
                                    yield return reasoning.Substring(i, len);
                                    await Task.Yield();
                                }
                            }
                        }
                        else if(messageEl.TryGetProperty("reasoning", out var reasoningEl2) && reasoningEl2.ValueKind == JsonValueKind.String)
                        {
                            Console.WriteLine($"[Debug] Found reasoning in message.reasoning: {reasoningEl2.GetString()}");
                            var reasoning = reasoningEl2.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(reasoning))
                            {
                                foundAny = true;
                                for (int i = 0; i < reasoning.Length; i += chunkSize)
                                {
                                    var len = Math.Min(chunkSize, reasoning.Length - i);
                                    yield return reasoning.Substring(i, len);
                                    await Task.Yield();
                                }
                            }
                        }
                    }
                }
            }

            if (!foundAny)
            {
                // indicate no reasoning was present
                yield return "oops!";
            }
        }

        /// <summary>
        /// Non-streaming chat completion for workflow generation etc.
        /// Sends system + user prompts to OpenRouter and returns the full response text.
        /// </summary>
        public async Task<string> ChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            var messages = new List<(string role, string content)>
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            };

            var responseJson = await CallOpenRouterChatAsync(
                messages,
                apiKey: "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",
                model: "openrouter/free",
                maxTokens: 4096,
                temperature: 0.3);

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentEl))
                    {
                        return contentEl.GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        // ---- 带参数的重载方法，支持从 LLM 配置动态指定模型 ----

        public async IAsyncEnumerable<string> Ask(string Question, Uri modelEndpoint, string modelName)
        {
            if (string.IsNullOrEmpty(Question))
            {
                yield return "请输入您的问题？";
                yield break;
            }

            var chatClient = new OllamaApiClient(modelEndpoint, modelName);
            var chat = new Chat(chatClient);
            chat.AllowRecursiveToolCalls = true;
            chat.Think = true;

            var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            chat.OnThink += (s, e) =>
            {
                output.Writer.TryWrite($"{e}");
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var answerToken in chat.SendAsync(Question))
                    {
                        await output.Writer.WriteAsync(answerToken.ToString());
                    }
                }
                catch (Exception ex)
                {
                    output.Writer.TryWrite($"[Error] {ex.Message}");
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            await foreach (var item in output.Reader.ReadAllAsync())
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<string> AskWithTool(string Question, Uri modelEndpoint, string modelName)
        {
            if (string.IsNullOrEmpty(Question))
            {
                yield return "请输入您的问题？";
                yield break;
            }

            var chatClient = new OllamaApiClient(modelEndpoint, modelName);
            var chat = new Chat(chatClient);
            chat.AllowRecursiveToolCalls = true;
            chat.Think = true;
            chat.OnToolCall += (s, e) =>
            {
                Console.WriteLine($"[DeepSeekService] AskWithTool OnToolCall: {e.Function?.Name}");
            };

            var config = new McpServerConfiguration
            {
                Name = "test",
                Command = "http://localhost:3001",
                TransportType = McpServerTransportType.Sse
            };

            var toolList = await OllamaSharp.ModelContextProtocol.Tools.GetFromMcpServers(config);

            var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            chat.OnThink += (s, e) =>
            {
                output.Writer.TryWrite($"{e}");
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var answerToken in chat.SendAsync(Question, toolList))
                    {
                        await output.Writer.WriteAsync(answerToken.ToString());
                    }
                }
                catch (Exception ex)
                {
                    output.Writer.TryWrite($"[Error] {ex.Message}");
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            await foreach (var item in output.Reader.ReadAllAsync())
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<string> StreamReasoningFromContentAsync(string content, string apiKey, string model)
        {
            int chunkSize = 64;
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }

            var messages = new List<(string role, string content)>
            {
                ("system", ""),
                ("user", content)
            };

            string jsonResponse;
            try
            {
                jsonResponse = await CallOpenRouterChatAsync(messages, apiKey, model);
            }
            catch (Exception)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                yield break;
            }

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var foundAny = false;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var messageEl))
                    {
                        if (messageEl.TryGetProperty("content", out var reasoningEl) && reasoningEl.ValueKind == JsonValueKind.String)
                        {
                            var reasoning = reasoningEl.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(reasoning))
                            {
                                foundAny = true;
                                for (int i = 0; i < reasoning.Length; i += chunkSize)
                                {
                                    var len = Math.Min(chunkSize, reasoning.Length - i);
                                    yield return reasoning.Substring(i, len);
                                    await Task.Yield();
                                }
                            }
                        }
                        else if (messageEl.TryGetProperty("reasoning", out var reasoningEl2) && reasoningEl2.ValueKind == JsonValueKind.String)
                        {
                            var reasoning = reasoningEl2.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(reasoning))
                            {
                                foundAny = true;
                for (int i = 0; i < reasoning.Length; i += chunkSize)
                                {
                                    var len = Math.Min(chunkSize, reasoning.Length - i);
                                    yield return reasoning.Substring(i, len);
                                    await Task.Yield();
                                }
                            }
                        }
                    }
                }
            }

            if (!foundAny)
            {
                yield return "oops!";
            }
        }

        public async Task<string> ChatCompletionAsync(string systemPrompt, string userPrompt, string apiKey, string model, CancellationToken cancellationToken = default)
        {
            var messages = new List<(string role, string content)>
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            };

            var responseJson = await CallOpenRouterChatAsync(messages, apiKey, model, maxTokens: 4096, temperature: 0.3);

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentEl))
                    {
                        return contentEl.GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        // ---- 通用 OpenAI 兼容 API 调用（自定义 ApiBaseUrl） ----

        /// <summary>
        /// 通用 OpenAI 兼容流式问答 — 使用 SSE 实时流式传输，每收到一个 token 立即 yield return。
        /// 向 <c>{apiBaseUrl}/chat/completions</c> 发送 stream=true 请求，逐行解析 SSE 事件。
        /// </summary>
        public async IAsyncEnumerable<string> StreamChatCompletionAsync(
            string content,
            string apiBaseUrl,
            string apiKey,
            string model)
        {
            Console.WriteLine($"[DeepSeek] StreamChatCompletionAsync: url={apiBaseUrl}, model={model}, apiKey={(string.IsNullOrEmpty(apiKey) ? "(empty)" : apiKey[..Math.Min(8, apiKey.Length)] + "...")}");

            if (string.IsNullOrWhiteSpace(content))
                yield break;

            var endpoint = apiBaseUrl.TrimEnd('/') + "/chat/completions";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            if (!string.IsNullOrEmpty(apiKey))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "" },
                    new { role = "user", content = content }
                },
                stream = true
            };

            var json = JsonSerializer.Serialize(payload);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = httpContent };
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                Console.WriteLine($"[DeepSeek] SSE stream started, status={(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeepSeek] SSE request FAILED: {ex.GetType().Name} - {ex.Message}");
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DeepSeek] SSE error {(int)response.StatusCode}: {errorBody[..Math.Min(200, errorBody.Length)]}");
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            // 委托给不含 try-catch 的内层方法，满足 CS1626 约束
            await foreach (var token in ReadSseTokensAsync(reader))
                yield return token;
        }

        /// <summary>从 SSE 流中逐行读取并 yield return 每个 content token。不含 try-catch（CS1626 要求）。</summary>
        private static async IAsyncEnumerable<string> ReadSseTokensAsync(StreamReader reader)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line.Substring(6);
                if (data == "[DONE]") break;

                var token = TryParseSseContentToken(data);
                if (token != null)
                    yield return token;
            }
        }

        /// <summary>安全解析 SSE data 行中的 choices[0].delta.content，失败返回 null。</summary>
        private static string? TryParseSseContentToken(string data)
        {
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                {
                    foreach (var choice in choices.EnumerateArray())
                    {
                        if (choice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var contentEl) &&
                            contentEl.ValueKind == JsonValueKind.String)
                        {
                            var token = contentEl.GetString();
                            if (!string.IsNullOrEmpty(token))
                                return token;
                        }
                    }
                }
            }
            catch (JsonException) { }
            return null;
        }

        /// <summary>
        /// 向给定端点发送 OpenAI 兼容的 chat/completions 请求，返回原始 JSON。
        /// </summary>
        private async Task<string> CallChatCompletionApiAsync(
            string userContent,
            string apiBaseUrl,
            string apiKey,
            string model)
        {
            var endpoint = apiBaseUrl.TrimEnd('/') + "/chat/completions";
            Console.WriteLine($"[DeepSeek] POST {endpoint} | model={model} | hasAuth={!string.IsNullOrEmpty(apiKey)}");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            if (!string.IsNullOrEmpty(apiKey))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "" },
                    new { role = "user", content = userContent }
                },
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpoint, httpContent);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Chat API returned {(int)response.StatusCode}: {responseText[..Math.Min(200, responseText.Length)]}");

            return responseText;
        }
    }
}
