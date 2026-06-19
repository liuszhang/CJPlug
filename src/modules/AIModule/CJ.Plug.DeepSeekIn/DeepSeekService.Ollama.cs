using OllamaSharp;
using OllamaSharp.ModelContextProtocol.Server;
using System.Threading.Channels;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
namespace CJ.Plug.DeekSeekIn
{
    public partial class DeepSeekService : IDeepSeekService
    {

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

        public async IAsyncEnumerable<string> AskWithTool(string Question, string? mcpConnectionString)
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
            chat.OnToolCall += (s, e) =>
            {
                Console.WriteLine($"[DeepSeekService] AskWithTool OnToolCall: {e.Function?.Name}");
            };

            var toolList = await LoadMcpToolsAsync(mcpConnectionString);

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

        public async IAsyncEnumerable<string> AskWithTool(string Question, Uri modelEndpoint, string modelName, string? mcpConnectionString)
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

            var toolList = await LoadMcpToolsAsync(mcpConnectionString);

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
    }
}
