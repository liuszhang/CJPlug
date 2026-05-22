using CJ.Plug.Models.VariableType;

namespace CJ.Plug.Models.MCPTools;

/// <summary>
/// 将 VariableTypeEnum 映射为 MCP JSON Schema 的标准类型
/// </summary>
public static class McpTypeMapper
{
    /// <summary>
    /// 获取单个变量类型的 JSON Schema type 字符串
    /// </summary>
    public static string ToJsonSchemaType(VariableTypeEnum type, bool isArray)
    {
        if (isArray)
            return "array";

        return type switch
        {
            VariableTypeEnum.String or
            VariableTypeEnum.TextMapping or
            VariableTypeEnum.DefaultOutputMapping or
            VariableTypeEnum.DefaultInputMapping or
            VariableTypeEnum.RequestHeader or
            VariableTypeEnum.WordTextMapping or
            VariableTypeEnum.ConditionExpression or
            VariableTypeEnum.File => "string",

            VariableTypeEnum.Int => "integer",
            VariableTypeEnum.Float => "number",
            VariableTypeEnum.Bool => "boolean",

            // 未明确映射的类型默认按 string 处理
            _ => "string"
        };
    }

    /// <summary>
    /// 获取数组元素的 JSON Schema type 字符串
    /// </summary>
    public static string ToJsonSchemaItemType(VariableTypeEnum type)
    {
        return type switch
        {
            VariableTypeEnum.Int => "integer",
            VariableTypeEnum.Float => "number",
            VariableTypeEnum.Bool => "boolean",
            _ => "string"
        };
    }
}
