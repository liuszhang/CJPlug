using System.Text.Json.Nodes;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;

namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 从 PDZ 入口变量（BaseVariable 列表）自动生成 MCP JSON Schema
/// </summary>
public static class McpSchemaGenerator
{
    /// <summary>
    /// 将入口变量列表转换为 MCP Tool 的 inputSchema（JSON Schema 格式）
    /// </summary>
    /// <param name="entryVariables">工作流的入口变量（IsInput == true 的变量）</param>
    /// <returns>MCP inputSchema JSON</returns>
    public static JsonObject GenerateInputSchema(IEnumerable<BaseVariable> entryVariables)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var v in entryVariables)
        {
            // 跳过没有名称的变量
            if (string.IsNullOrWhiteSpace(v.Name))
                continue;

            // 尝试解析类型
            if (!Enum.TryParse<VariableTypeEnum>(v.Type, out var varType))
                continue;

            var prop = BuildPropertySchema(v, varType);
            properties[v.Name] = prop;

            if (v.IsRequired == true)
                required.Add(v.Name);
        }

        schema["properties"] = properties;
        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    /// <summary>
    /// 构建单个参数的 JSON Schema property 节点
    /// </summary>
    private static JsonObject BuildPropertySchema(BaseVariable v, VariableTypeEnum varType)
    {
        var isArray = v.IsArray;
        var jsonType = McpTypeMapper.ToJsonSchemaType(varType, isArray);

        var prop = new JsonObject
        {
            ["type"] = jsonType,
            ["description"] = BuildDescription(v)
        };

        // 数组类型需要声明 items
        if (isArray)
        {
            var itemType = McpTypeMapper.ToJsonSchemaItemType(varType);
            prop["items"] = new JsonObject { ["type"] = itemType };
        }

        // 默认值
        if (!string.IsNullOrWhiteSpace(v.Value))
        {
            prop["default"] = v.Value;
        }

        // DisplayName 作为 title
        if (!string.IsNullOrWhiteSpace(v.DisplayName) && v.DisplayName != v.Name)
        {
            prop["title"] = v.DisplayName;
        }

        return prop;
    }

    /// <summary>
    /// 构建参数描述文本：优先用 Description，其次 DisplayName，最后 Name
    /// </summary>
    private static string BuildDescription(BaseVariable v)
    {
        if (!string.IsNullOrWhiteSpace(v.Description))
            return v.Description;
        if (!string.IsNullOrWhiteSpace(v.DisplayName))
            return v.DisplayName;
        return v.Name ?? "";
    }

    /// <summary>
    /// 将入口变量列表（EntryVariableDto）转换为 MCP Tool 的 inputSchema（JSON Schema 格式）
    /// </summary>
    /// <param name="entryVariables">工作流的入口变量（已经过 IsInput 过滤的变量）</param>
    /// <returns>MCP inputSchema JSON</returns>
    public static JsonObject GenerateInputSchema(IEnumerable<EntryVariableDto> entryVariables)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var v in entryVariables)
        {
            if (string.IsNullOrWhiteSpace(v.Name))
                continue;

            if (!Enum.TryParse<VariableTypeEnum>(v.Type, out var varType))
                continue;

            var prop = BuildPropertySchema(v.Name, v.DisplayName, v.Description,
                v.Value, v.IsRequired, v.IsArray, varType);
            properties[v.Name] = prop;

            if (v.IsRequired == true)
                required.Add(v.Name);
        }

        schema["properties"] = properties;
        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    /// <summary>
    /// 构建单个参数的 JSON Schema property 节点（参数独立版本）
    /// </summary>
    private static JsonObject BuildPropertySchema(
        string name, string? displayName, string? description,
        string? defaultValue, bool isRequired, bool isArray, VariableTypeEnum varType)
    {
        var jsonType = McpTypeMapper.ToJsonSchemaType(varType, isArray);

        var prop = new JsonObject
        {
            ["type"] = jsonType,
            ["description"] = description ?? displayName ?? name
        };

        if (isArray)
        {
            var itemType = McpTypeMapper.ToJsonSchemaItemType(varType);
            prop["items"] = new JsonObject { ["type"] = itemType };
        }

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            prop["default"] = defaultValue;
        }

        if (!string.IsNullOrWhiteSpace(displayName) && displayName != name)
        {
            prop["title"] = displayName;
        }

        return prop;
    }

    /// <summary>
    /// 将 MCP tools/call 传入的 arguments 映射为工作流执行参数
    /// </summary>
    /// <param name="arguments">MCP 客户端传来的参数（JSON）</param>
    /// <returns>key-value 参数字典，可直接传给工作流执行</returns>
    public static Dictionary<string, object?> MapArguments(JsonObject arguments)
    {
        var result = new Dictionary<string, object?>();
        foreach (var prop in arguments)
        {
            result[prop.Key] = prop.Value?.ToString();
        }
        return result;
    }
}
