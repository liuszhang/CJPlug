using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.DeekSeekIn
{
    public interface IDeepSeekService
    {
        IAsyncEnumerable<string> Ask(string Question);
        IAsyncEnumerable<string> AskWithTool(string Question);
        IAsyncEnumerable<string> StreamReasoningFromContentAsync(string content);

        /// <summary>
        /// 非流式 Chat Completion — 发送 system + user prompt，返回完整回复文本。
        /// 用于工作流生成等需要一次性拿到完整响应的场景。
        /// </summary>
        Task<string> ChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用指定模型端点进行问答（Ollama 类型）
        /// </summary>
        IAsyncEnumerable<string> Ask(string Question, Uri modelEndpoint, string modelName);

        /// <summary>
        /// 使用指定模型端点进行带工具的问答（Ollama 类型）
        /// </summary>
        IAsyncEnumerable<string> AskWithTool(string Question, Uri modelEndpoint, string modelName);

        /// <summary>
        /// 使用指定模型配置进行 OpenRouter 流式问答
        /// </summary>
        IAsyncEnumerable<string> StreamReasoningFromContentAsync(string content, string apiKey, string model);

        /// <summary>
        /// 使用指定模型配置进行非流式 Chat Completion
        /// </summary>
        Task<string> ChatCompletionAsync(string systemPrompt, string userPrompt, string apiKey, string model, CancellationToken cancellationToken = default);

        /// <summary>
        /// 通用 OpenAI 兼容流式问答 — 使用自定义 ApiBaseUrl + ApiKey + Model。
        /// 适用于非 Ollama 的自定义供应商（LM Studio / vLLM / 自部署兼容端点等）。
        /// </summary>
        IAsyncEnumerable<string> StreamChatCompletionAsync(string content, string apiBaseUrl, string apiKey, string model);

        /// <summary>
        /// 带 MCP Server 连接的 Ollama 工具问答
        /// </summary>
        IAsyncEnumerable<string> AskWithTool(string Question, string? mcpConnectionString);

        /// <summary>
        /// 带 MCP Server 连接的 Ollama 工具问答（指定端点+模型）
        /// </summary>
        IAsyncEnumerable<string> AskWithTool(string Question, Uri modelEndpoint, string modelName, string? mcpConnectionString);

        /// <summary>
        /// 带 MCP Server 配置的通用 OpenAI 兼容流式问答
        /// </summary>
        IAsyncEnumerable<string> StreamChatCompletionAsync(string content, string apiBaseUrl, string apiKey, string model, string? mcpConnectionString);
    }
}
