using System.Text;
using System.Text.Json;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.MCPTools;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.ProcessManageApi.Services;

/// <summary>
/// AI 工作流生成器 — 接收自然语言描述，利用 LLM 自动编排工作流。
/// 将 CapabilityRegistry 注入 LLM Prompt，让 LLM 理解可用插件并生成结构化工作流 JSON。
/// </summary>
public class AiWorkflowBuilderService
{
    private readonly CapabilityRegistry _registry;
    private readonly IDeepSeekService _deepSeekService;
    private readonly ILogger<AiWorkflowBuilderService> _logger;

    public AiWorkflowBuilderService(
        CapabilityRegistry registry,
        IDeepSeekService deepSeekService,
        ILogger<AiWorkflowBuilderService> logger)
    {
        _registry = registry;
        _deepSeekService = deepSeekService;
        _logger = logger;
    }

    /// <summary>
    /// 根据用户自然语言需求生成工作流定义
    /// </summary>
    public async Task<WorkflowGenerationResult> GenerateAsync(
        string userPrompt, CancellationToken ct = default)
    {
        // 1. 获取插件能力上下文
        var capabilitiesContext = _registry.ToPromptContext();

        // 2. 构造 System Prompt
        var systemPrompt = BuildSystemPrompt(capabilitiesContext);

        // 3. 调用 LLM（通过 DeepSeekService → OpenRouter）
        //var llmResponse = await _deepSeekService.ChatCompletionAsync(systemPrompt, userPrompt, ct);
        var llmResponse = new StringBuilder();
        await foreach (var chunk in _deepSeekService.Ask(systemPrompt + userPrompt))
        {
            llmResponse.Append(chunk);
            // 处理每个 chunk（这里简单拼接，可以根据需要处理流式输出）
            //CLog.Information($"Received chunk: {chunk}");
        }

        // 4. 解析 LLM 输出
        //CLog.Information(llmResponse.ToString());
        return ParseWorkflowResponse(llmResponse.ToString());
    }

    /// <summary>
    /// 验证生成的工作流是否合法（插件名和参数名是否匹配）
    /// </summary>
    public List<string> Validate(WorkflowGenerationResult result)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(result.Name))
            errors.Add("工作流名称不能为空");

        foreach (var activity in result.Activities)
        {
            var capability = _registry.Get(activity.Plugin);
            if (capability == null)
            {
                errors.Add($"未找到插件: {activity.Plugin}");
                continue;
            }

            // 检查必填参数
            foreach (var required in capability.Inputs.Where(i => i.IsRequired))
            {
                if (!activity.Params.ContainsKey(required.Name))
                    errors.Add($"插件 {activity.Plugin} 缺少必填参数: {required.Name}");
            }
        }

        return errors;
    }

    #region Private Methods

    private string BuildSystemPrompt(string capabilitiesContext)
    {
        return $$"""
你是一个工作流生成器。你的任务是根据用户需求，使用给定的插件编排工作流。

{{capabilitiesContext}}

## 规则

1. **输出格式**: 严格输出以下 JSON 格式（不要包含其他文字）:
```json
{
  "name": "工作流名称（英文 snake_case，如 city_weather_query）",
  "description": "工作流的中文描述",
  "inputs": [
    { "name": "参数名", "type": "String/Int/Float/Bool", "description": "参数说明", "isRequired": true/false }
  ],
  "activities": [
    {
      "plugin": "插件名称（必须与上面列表中的完全一致）",
      "description": "此步骤的作用",
      "params": {
        "参数名": "参数值"
      }
    }
  ]
}
```

2. **参数引用语法**:
   - 引用工作流入口参数: `$输入参数名`
   - 引用上一步输出: `$prev.输出参数名`

3. **插件选择**: 每个 activity 的 plugin 字段必须是上面插件列表中的 Name（不是 PluginTypeKey）
4. **参数映射**: params 中的值使用 $参数名 引用时，参数名必须来自该插件定义的 Inputs 或上一步插件的 Outputs
5. **最后一步**: 最后一个 activity 的输出将作为整个工作流的返回结果
6. **保持简洁**: 只生成必要的步骤
""";
    }

    private WorkflowGenerationResult ParseWorkflowResponse(string llmResponse)
    {
        try
        {
            // 尝试提取 JSON（LLM 可能包在 ```json ``` 中）
            var json = ExtractJson(llmResponse);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new WorkflowGenerationResult
            {
                Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "untitled" : "untitled",
                Description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                RawResponse = llmResponse,
            };

            // 解析入口参数
            if (root.TryGetProperty("inputs", out var inputs) && inputs.ValueKind == JsonValueKind.Array)
            {
                result.Inputs = inputs.EnumerateArray().Select(i => new WorkflowInputDef
                {
                    Name = i.GetProperty("name").GetString() ?? "",
                    Type = i.TryGetProperty("type", out var t) ? t.GetString() ?? "String" : "String",
                    Description = i.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    IsRequired = i.TryGetProperty("isRequired", out var req) && req.GetBoolean(),
                }).ToList();
            }

            // 解析活动步骤
            if (root.TryGetProperty("activities", out var activities) && activities.ValueKind == JsonValueKind.Array)
            {
                result.Activities = activities.EnumerateArray().Select(a => new WorkflowActivityDef
                {
                    Plugin = a.GetProperty("plugin").GetString() ?? "",
                    Description = a.TryGetProperty("description", out var ad) ? ad.GetString() ?? "" : "",
                    Params = a.TryGetProperty("params", out var ap) && ap.ValueKind == JsonValueKind.Object
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(ap.GetRawText()) ?? new()
                        : new(),
                }).ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM workflow response");
            return new WorkflowGenerationResult
            {
                Name = "parse_error",
                Description = $"解析失败: {ex.Message}",
                RawResponse = llmResponse,
            };
        }
    }

    private static string ExtractJson(string text)
    {
        // 移除 ```json ... ``` 包裹
        var start = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            start += 7;
            var end = text.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return text[start..end].Trim();
        }

        // 直接找 { 开始
        var braceStart = text.IndexOf('{');
        if (braceStart >= 0)
        {
            var braceEnd = text.LastIndexOf('}');
            if (braceEnd > braceStart)
                return text[braceStart..(braceEnd + 1)].Trim();
        }

        return text;
    }

    #endregion
}

/// <summary>
/// AI 生成的工作流结果
/// </summary>
public class WorkflowGenerationResult
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<WorkflowInputDef> Inputs { get; set; } = new();
    public List<WorkflowActivityDef> Activities { get; set; } = new();
    public string RawResponse { get; set; } = "";
    public List<string>? ValidationErrors { get; set; }
}

public class WorkflowInputDef
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "String";
    public string Description { get; set; } = "";
    public bool IsRequired { get; set; }
}

public class WorkflowActivityDef
{
    public string Plugin { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Params { get; set; } = new();
}
