using CJ.Plug.LlmConfigApiClient;
using CJCore.LLM.Abstractions;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.Agent;

/// <summary>
/// 把 CJPlug 既有的 <b>LLM 配置（平台默认模型）</b>翻译成 CJCore Agent 链路要的
/// <see cref="ILLMConfigReader"/>（P8.3.1）。
///
/// <para><b>为什么不是自己再建一套模型配置</b>：CJPlug 已经有 LLM 配置模块（供应商 + 模型 + 默认模型，
/// <c>AskAI</c> 页面就在用），再建一套就是第二个真理源 —— 用户在平台里改了默认模型，
/// Agent 却读另一份，属于最难查的一类 bug。本类只做<b>协议翻译</b>，不做配置存储。</para>
///
/// <para><b>为什么只实现这一个方法</b>：<see cref="ILLMConfigReader"/> 契约本来就只有一个方法
/// （<c>GetDefaultModelConfigAsync</c>）⇒ 适配面极窄，这也是 C 分支成本远低于预估的原因之一。</para>
///
/// <para><b>失败口径</b>：取不到配置时返回空串而不是抛异常 —— 让上层（页面 / Agent）把
/// 「未配置默认模型」翻译成一句人能看懂的提示；异常留给真正的网络故障。</para>
/// </summary>
public sealed class PlugLlmConfigReader : ILLMConfigReader
{
    private readonly ILlmConfigApiClient _client;
    private readonly ILogger<PlugLlmConfigReader> _logger;

    public PlugLlmConfigReader(ILlmConfigApiClient client, ILogger<PlugLlmConfigReader> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 返回的是 <b>BaseUrl 原样</b>（如 <c>https://api.deepseek.com</c> 或 <c>http://localhost:11434/v1</c>）：
    /// 拼 <c>/chat/completions</c> 的工作由 <see cref="LlmEndpointNormalizer"/> 在 <c>LLMClient</c> 内统一做
    /// （已含 <c>/v1</c> 补全的口径）⇒ 这里<b>不要</b>再拼一次，否则会拼出 <c>.../v1/chat/completions/chat/completions</c>。
    /// </remarks>
    public async Task<(string? Endpoint, string? ApiKey, string? ModelName)> GetDefaultModelConfigAsync(
        CancellationToken ct = default)
    {
        try
        {
            var info = await _client.GetDefaultModelInfoAsync(ct);
            if (info is null)
            {
                _logger.LogWarning("[P8-Agent] 未取到平台默认模型（LLM 配置为空）");
                return (null, null, null);
            }

            _logger.LogInformation(
                "[P8-Agent] 默认模型: Provider={Provider}, Model={Model}, Endpoint={Endpoint}",
                info.ProviderName, info.ModelName, info.ApiBaseUrl);

            return (info.ApiBaseUrl, info.ApiKey, info.ModelName);
        }
        catch (Exception ex)
        {
            // 上层（LLMChatService）会把它要的"未配置默认模型"单独判空；这里是真故障，记下来但不吞。
            _logger.LogWarning(ex, "[P8-Agent] 读取平台默认模型失败");
            return (null, null, null);
        }
    }
}
