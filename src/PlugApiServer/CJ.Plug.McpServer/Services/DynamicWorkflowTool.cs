using System.Text.Json;
using System.Text.Json.Nodes;
using CJ.Plug.Models.MCPTools;
using ModelContextProtocol.Protocol;

namespace CJ.Plug.McpServer.Services;

/// <summary>
/// 动态工作流工具 —— 将 PublishedWorkflowDto 转换为 MCP Protocol.Tool 并执行调用
/// </summary>
public class DynamicWorkflowTool
{
    private readonly PublishedWorkflowDto _dto;
    private readonly HttpClient _dispatchClient;
    private readonly string _mcpToolName;
    private readonly Tool _protocolTool;

    public PublishedWorkflowDto Dto => _dto;
    public string McpToolName => _mcpToolName;
    public Tool ProtocolTool => _protocolTool;

    public DynamicWorkflowTool(
        string mcpToolName,
        PublishedWorkflowDto dto,
        HttpClient dispatchClient)
    {
        _mcpToolName = mcpToolName;
        _dto = dto;
        _dispatchClient = dispatchClient;

        // 使用 McpSchemaGenerator 生成结构化参数 schema（每个参数独立一个 property）
        var schema = McpSchemaGenerator.GenerateInputSchema(dto.EntryVariables);
        Console.WriteLine($"[DynamicWorkflowTool] Tool={mcpToolName}, EntryVariables count={dto.EntryVariables.Count}");

        _protocolTool = new Tool
        {
            Name = mcpToolName,
            Description = BuildDescription(dto),
            InputSchema = JsonDocument.Parse(schema.ToJsonString()).RootElement,
        };
    }

    private static string BuildDescription(PublishedWorkflowDto dto)
    {
        var desc = dto.Description ?? $"Execute workflow: {dto.Name}";
        return desc;
    }

    /// <summary>
    /// 执行工具调用 —— 将 MCP 参数映射为 PlugVariableData 并调用 DispatchServer
    /// </summary>
    public async Task<CallToolResult> InvokeAsync(
        JsonObject arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var inputVariables = new List<PlugVariableData>();

            // 将 arguments 中的每个属性作为输入参数（扁平键值对格式）
            // 例如: {"param1": "value1", "param2": "value2"}
            foreach (var (key, value) in arguments)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                // 跳过 JSON Schema 协议字段（如 $schema）
                if (key.StartsWith("$"))
                    continue;

                var strValue = value?.ToString();
                if (value is JsonObject || value is JsonArray)
                    strValue = value.ToJsonString();

                inputVariables.Add(new PlugVariableData
                {
                    Name = key,
                    Value = strValue ?? "",
                    IsInput = true,
                });
            }

            var request = new McpToolExecutionRequest
            {
                PlugDefinitionId = _dto.WorkflowDefinitionId,
                ToolType = _dto.ToolType ?? "Workflow",
                InputVariables = inputVariables,
            };

            var response = await _dispatchClient.PostAsJsonAsync(
                "/api/plug/executeMcpTool", request, cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync(cancellationToken);

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result }
                }
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Execution failed: {ex.Message}" }
                },
                IsError = true
            };
        }
    }
}
