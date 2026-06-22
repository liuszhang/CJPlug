using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LLMPlug;

public class LLMPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
{
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        CLog.Information("execute LLMPlug");
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
        {
            return await ReportErrorResult(erd);
        }

        var PlugDefinitionId = plugExecutionRequest?.PlugDefinitionId;

        try
        {
            // 读取 Question 参数值
            var question = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.Question.ToString());
            if (string.IsNullOrEmpty(question))
            {
                CLog.Error("Question参数为空", PlugDataZone?.PDZId);
                return await ReportErrorResult(erd);
            }

            // 从 PDZ 变量读取配置（由配置页面保存）
            var systemPrompt = PlugDataZone?.GetVariableValue(PlugDefinitionId, "LLMSystemPrompt") ?? "";
            var llmUrl = PlugDataZone?.GetVariableValue(PlugDefinitionId, "LLMUrl") ?? "";
            var llmApiKey = PlugDataZone?.GetVariableValue(PlugDefinitionId, "LLMApiKey") ?? "";
            var llmModel = PlugDataZone?.GetVariableValue(PlugDefinitionId, "LLMModel") ?? "";

            if (string.IsNullOrEmpty(llmUrl) || string.IsNullOrEmpty(llmModel))
            {
                CLog.Error("LLM配置不完整：url或model为空", PlugDataZone?.PDZId);
                return await ReportErrorResult(erd);
            }

            // 构建 system + user messages
            var messages = new List<(string role, string content)>
            {
                ("system", systemPrompt),
                ("user", question)
            };

            // 调用大模型 API
            var responseJson = await CallChatApiAsync(llmUrl, llmApiKey, llmModel, messages);

            // 解析返回结果
            var (thinking, answer) = ParseResponse(responseJson);

            // 写入 Thinking 参数
            PlugDataZone?.SetVariableValue(PlugDefinitionId, InitVariableNames.Thinking.ToString(), thinking ?? "");
            var thinkingVar = PlugDataZone?.PlugVariableDatas?
                .Find(p => p.PlugDefinitionId == PlugDefinitionId && p.Name == InitVariableNames.Thinking.ToString());
            if (thinkingVar != null)
            {
                await PDZApiClient.UpdatePlugVariableData(thinkingVar);
            }

            // 写入 Answer 参数
            PlugDataZone?.SetVariableValue(PlugDefinitionId, InitVariableNames.Answer.ToString(), answer ?? "");
            var answerVar = PlugDataZone?.PlugVariableDatas?
                .Find(p => p.PlugDefinitionId == PlugDefinitionId && p.Name == InitVariableNames.Answer.ToString());
            if (answerVar != null)
            {
                await PDZApiClient.UpdatePlugVariableData(answerVar);
            }

            StatusReporter.PDZUpdated(PlugDataZone?.PDZId);

            CLog.Information("LLM调用完成", PlugDataZone?.PDZId);
        }
        catch (Exception e)
        {
            CLog.Information("LLM调用出错：" + e.Message);
            return await ReportErrorResult(erd);
        }

        return await ReportCompletedResult(erd);
    }

    /// <summary>
    /// 调用 OpenAI 兼容的 Chat Completions API
    /// </summary>
    private async Task<string> CallChatApiAsync(string apiBaseUrl, string apiKey, string model, List<(string role, string content)> messages)
    {
        // 标准化 API 地址：自动补全 /v1 前缀（OpenAI 兼容 API 标准路径）
        var baseUrl = apiBaseUrl.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl += "/v1";
        }
        var endpoint = baseUrl + "/chat/completions";

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        if (!string.IsNullOrEmpty(apiKey))
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = model,
            messages = messages.Select(m => new { role = m.role, content = m.content }),
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, httpContent);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API returned {(int)response.StatusCode}: {responseText[..Math.Min(200, responseText.Length)]}");

        return responseText;
    }

    /// <summary>
    /// 解析 OpenAI 兼容响应，提取 thinking（reasoning_content）和 answer（content）
    /// </summary>
    private (string? thinking, string? answer) ParseResponse(string responseJson)
    {
        string? thinking = null;
        string? answer = null;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var message))
                    {
                        // reasoning_content（DeepSeek 等模型的思考过程）
                        if (message.TryGetProperty("reasoning_content", out var reasoningEl))
                            thinking = reasoningEl.GetString();
                        else if (message.TryGetProperty("reasoning", out var reasoningEl2))
                            thinking = reasoningEl2.GetString();

                        // content（回答正文）
                        if (message.TryGetProperty("content", out var contentEl))
                            answer = contentEl.GetString();
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            CLog.Information("解析LLM响应JSON出错：" + ex.Message);
        }

        // 兜底：无 reasoning 但 message 有 content 时拿 content 当 answer
        if (string.IsNullOrEmpty(answer) && !string.IsNullOrEmpty(thinking))
        {
            answer = thinking;
            thinking = null;
        }

        return (thinking, answer);
    }
}
