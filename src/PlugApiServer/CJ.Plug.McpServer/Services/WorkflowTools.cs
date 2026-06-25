using System.ComponentModel;
using System.Text.Json;
using System.Net.Http.Json;
using CJ.Plug.Models.MCPTools;
using ModelContextProtocol.Server;

namespace CJ.Plug.McpServer.Services;

/// <summary>
/// 提供对已发布工作流的 MCP Tool 访问。
/// 本阶段通过 ListPublishedWorkflows + ExecutePublishedWorkflow 模式暴露，
/// 后续可升级为每个工作流独立注册为 MCP Tool。
/// </summary>
[McpServerToolType]
public sealed class WorkflowTools
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static string? _dispatchServerUrl;

    /// <summary>
    /// 配置 DispatchServer 地址（由 Program.cs 在启动时调用）
    /// </summary>
    public static void Configure(string dispatchServerUrl)
    {
        _dispatchServerUrl = dispatchServerUrl;
        _httpClient.BaseAddress = new Uri(dispatchServerUrl);
    }

    /// <summary>
    /// 列出所有已发布为 MCP Tool 的工作流及其输入参数 Schema
    /// </summary>
    [McpServerTool, Description("列出所有已发布为 MCP Tool 的工作流，包含每个工作流的名称、描述和输入参数定义。调用此工具了解有哪些可用的工作流及其参数要求。")]
    public static async Task<string> ListPublishedWorkflows()
    {
        try
        {
            var workflows = await _httpClient
                .GetFromJsonAsync<List<PublishedWorkflowDto>>(
                    "/api/mcp/getPublishedWorkflows");

            if (workflows == null || workflows.Count == 0)
                return "当前没有已发布的工作流。请在 ProcessManage 中将工作流标记为发布。";

            var result = new System.Text.StringBuilder();
            result.AppendLine($"共有 {workflows.Count} 个已发布的工作流：\n");

            foreach (var wf in workflows)
            {
                result.AppendLine($"## {wf.Name}");
                result.AppendLine($"ID: {wf.WorkflowDefinitionId}");
                result.AppendLine($"描述: {wf.Description}");
                result.AppendLine($"参数数量: {wf.EntryVariables.Count}");

                if (wf.EntryVariables.Count > 0)
                {
                    result.AppendLine("参数列表:");
                    foreach (var v in wf.EntryVariables)
                    {
                        var required = v.IsRequired ? "(必填)" : "(可选)";
                        var array = v.IsArray ? "[]" : "";
                        result.AppendLine($"  - {v.Name}: {v.Type}{array} {required} — {v.Description}");
                        if (!string.IsNullOrEmpty(v.Value))
                            result.AppendLine($"    默认值: {v.Value}");
                    }
                }
                result.AppendLine();
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"获取已发布工作流失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 执行指定的已发布工作流
    /// </summary>
    [McpServerTool, Description("执行一个已发布的工作流。先调用 ListPublishedWorkflows 查看可用的工作流及其参数定义。")]
    public static async Task<string> ExecutePublishedWorkflow(
        [Description("工作流定义ID（从 ListPublishedWorkflows 获取）")] string workflowDefinitionId,
        [Description("工作流参数，JSON 格式，例如 {\"city\": \"北京\", \"limit\": 10}。也支持纯字符串或纯数字作为单个参数传入。")] string parameters)
    {
        try
        {
            var inputVariables = ParseParameters(parameters);

            var request = new McpToolExecutionRequest
            {
                PlugDefinitionId = workflowDefinitionId,
                ToolType = "Workflow",
                InputVariables = inputVariables,
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/plug/executeMcpTool", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return $"工作流执行结果: {result}";
        }
        catch (Exception ex)
        {
            return $"工作流执行失败: {ex.Message}";
        }
    }

    private static List<PlugVariableData> ParseParameters(string parameters)
    {
        var inputVariables = new List<PlugVariableData>();

        if (string.IsNullOrWhiteSpace(parameters))
            return inputVariables;

        // 策略 1: 尝试 JSON 对象 → 键值对展开
        try
        {
            var doc = JsonDocument.Parse(parameters);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var kvp in root.EnumerateObject())
                {
                    inputVariables.Add(new PlugVariableData
                    {
                        Name = kvp.Name,
                        Value = kvp.Value.ValueKind == JsonValueKind.String
                            ? kvp.Value.GetString()
                            : kvp.Value.GetRawText(),
                        Type = MapJsonKindToType(kvp.Value.ValueKind),
                        IsInput = true,
                    });
                }
                return inputVariables;
            }
        }
        catch (JsonException) { /* 不是 JSON 对象，继续下一种策略 */ }

        // 策略 2: 尝试解析为 JSON 值类型（字符串、数字、布尔）
        try
        {
            // 用 JsonSerializer 安全包裹：将任意字符串转为 JSON 值后再包进对象
            var wrapped = $"{{\"value\": {JsonSerializer.Serialize(parameters)}}}";
            var doc = JsonDocument.Parse(wrapped);
            var root = doc.RootElement;
            if (root.TryGetProperty("value", out var val))
            {
                inputVariables.Add(new PlugVariableData
                {
                    Name = "input",
                    Value = val.ValueKind == JsonValueKind.String
                        ? val.GetString()
                        : val.GetRawText(),
                    Type = MapJsonKindToType(val.ValueKind),
                    IsInput = true,
                });
                return inputVariables;
            }
        }
        catch (JsonException) { }

        // 策略 3: 纯文本 — 原样作为单个参数
        inputVariables.Add(new PlugVariableData
        {
            Name = "input",
            Value = parameters,
            Type = "String",
            IsInput = true,
        });

        return inputVariables;
    }

    /// <summary>
    /// 将 JSON ValueKind 映射为变量类型字符串
    /// </summary>
    private static string MapJsonKindToType(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.Number => "Double",
            JsonValueKind.True or JsonValueKind.False => "Bool",
            _ => "String",
        };
    }

    /// <summary>
    /// 获取指定工作流的详细 JSON Schema（供 LLM 理解参数结构）
    /// </summary>
    [McpServerTool, Description("获取指定工作流的完整 JSON Schema 定义，用于精确理解每个参数的类型、约束和默认值。")]
    public static async Task<string> GetWorkflowSchema(
        [Description("工作流定义ID")] string workflowDefinitionId)
    {
        try
        {
            var workflows = await _httpClient
                .GetFromJsonAsync<List<PublishedWorkflowDto>>(
                    "/api/mcp/getPublishedWorkflows");

            var wf = workflows?.FirstOrDefault(
                w => w.WorkflowDefinitionId == workflowDefinitionId);

            if (wf == null)
                return $"未找到工作流: {workflowDefinitionId}";

            var schema = new
            {
                name = wf.Name,
                description = wf.Description,
                workflowDefinitionId = wf.WorkflowDefinitionId,
                inputSchema = McpSchemaGenerator.GenerateInputSchema(
                    wf.EntryVariables.Select(v => new CJ.Plug.Models.Shared.BaseVariable
                    {
                        Name = v.Name,
                        DisplayName = v.DisplayName,
                        Description = v.Description,
                        Type = v.Type,
                        IsRequired = v.IsRequired,
                        IsArray = v.IsArray,
                        Value = v.Value,
                        IsInput = true,
                    }))
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return $"获取工作流 Schema 失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 查询工作流执行状态
    /// </summary>
    [McpServerTool, Description("查询工作流执行状态。传入执行后返回的 workflowInstanceId，获取当前执行进度和结果。")]
    public static async Task<string> GetExecutionStatus(
        [Description("工作流实例 ID（从 ExecutePublishedWorkflow 返回的结果中获取）")] string workflowInstanceId)
    {
        try
        {
            var status = await _httpClient.GetFromJsonAsync<ExecutionStatusDto>(
                $"/api/plug/executionStatus/{workflowInstanceId}");

            if (status == null)
                return "未找到执行记录";

            var result = new System.Text.StringBuilder();
            result.AppendLine($"执行状态: {status.Status}");
            if (!string.IsNullOrEmpty(status.SubStatus))
                result.AppendLine($"子状态: {status.SubStatus}");
            if (status.CreatedAt.HasValue)
                result.AppendLine($"创建时间: {status.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            if (status.FinishedAt.HasValue)
                result.AppendLine($"完成时间: {status.FinishedAt:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(status.ResultMessage))
                result.AppendLine($"结果消息: {status.ResultMessage}");
            if (!string.IsNullOrEmpty(status.ResultString))
                result.AppendLine($"结果内容: {status.ResultString}");

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"查询执行状态失败: {ex.Message}";
        }
    }
}
