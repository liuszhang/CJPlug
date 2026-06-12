using CJ.Plug.Models.MCPTools;

namespace RESTPlug.Capabilities;

/// <summary>
/// HTTP 请求插件的能力描述 — 发送 HTTP 请求并获取响应
/// </summary>
public class HttpPluginCapability : IPluginCapability
{
    public string PluginTypeKey => "RESTPlug";
    public string Name => "HTTP请求";

    public string Description =>
        "发送 HTTP 请求并获取响应内容。支持 GET、POST、PUT、DELETE 等方法，" +
        "可自定义请求头、请求体和超时时间。适用于调用外部 REST API 获取数据。";

    public List<CapabilityParameter> Inputs => new()
    {
        new()
        {
            Name = "url", Type = "String",
            Description = "请求的目标 URL，包含协议和路径",
            IsRequired = true
        },
        new()
        {
            Name = "method", Type = "String",
            Description = "HTTP 方法: GET, POST, PUT, DELETE, PATCH 等",
            IsRequired = true,
            Value = "GET"
        },
        new()
        {
            Name = "headers", Type = "String", IsArray = true,
            Description = "请求头，JSON 键值对格式，如 [{\"key\":\"Content-Type\",\"value\":\"application/json\"}]"
        },
        new()
        {
            Name = "body", Type = "String",
            Description = "请求体内容（POST/PUT 时使用），可以是 JSON 字符串或纯文本"
        },
        new()
        {
            Name = "timeout", Type = "Int",
            Description = "请求超时时间（秒）",
            Value = "30"
        },
    };

    public List<CapabilityParameter> Outputs => new()
    {
        new()
        {
            Name = "statusCode", Type = "Int",
            Description = "HTTP 响应状态码（200 表示成功）"
        },
        new()
        {
            Name = "responseBody", Type = "String",
            Description = "响应正文内容"
        },
        new()
        {
            Name = "responseHeaders", Type = "String",
            Description = "响应头信息"
        },
    };

    public string[] Tags => new[] { "网络", "API", "HTTP", "请求", "数据获取" };
}
