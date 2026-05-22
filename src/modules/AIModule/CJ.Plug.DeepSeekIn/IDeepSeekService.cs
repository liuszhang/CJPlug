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
    }
}
