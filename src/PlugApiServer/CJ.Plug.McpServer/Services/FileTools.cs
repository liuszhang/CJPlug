using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace CJ.Plug.McpServer.Services;

/// <summary>
/// MCP 文件工具 —— 为 AI Agent 提供文件上传和查询能力。
/// AI Agent 可通过这些工具将文件上传到平台，或查找已有文件，
/// 获取 "fileName:fileId" 格式的文件引用，用于其他 MCP Tool 的 File 类型参数。
/// </summary>
[McpServerToolType]
public sealed class FileTools
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };
    private static string? _apiServerUrl;

    /// <summary>
    /// 配置 API 服务器地址（由 Program.cs 在启动时调用）
    /// </summary>
    public static void Configure(string apiServerUrl)
    {
        _apiServerUrl = apiServerUrl;
        _httpClient.BaseAddress = new Uri(apiServerUrl);
    }

    [McpServerTool, Description(
        "上传文件到插件平台。参数 fileContent 为 base64 编码的文件内容，fileName 为文件名（含扩展名，如 report.docx）。" +
        "返回文件引用字符串（格式: \"fileName:fileId\"），请将此返回值直接作为其他 MCP Tool 的文件参数值使用。")]
    public static async Task<string> UploadFileForMcpTool(
        [Description("base64 编码的文件内容")] string fileContent,
        [Description("文件名（含扩展名），如 report.docx")] string fileName)
    {
        try
        {
            if (_apiServerUrl == null)
                return "错误: FileTools 未配置 API 服务器地址";

            if (string.IsNullOrWhiteSpace(fileContent))
                return "错误: fileContent 不能为空";

            if (string.IsNullOrWhiteSpace(fileName))
                return "错误: fileName 不能为空";

            var payload = new { fileContent, fileName };
            var response = await _httpClient.PostAsJsonAsync("/api/file/uploadBase64", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"文件上传失败 (HTTP {(int)response.StatusCode}): {error}";
            }

            var fileReference = await response.Content.ReadAsStringAsync();
            return $"文件上传成功！请在调用其他工具时使用此值作为文件参数: {fileReference}";
        }
        catch (Exception ex)
        {
            return $"文件上传异常: {ex.Message}";
        }
    }

    [McpServerTool, Description(
        "列出插件平台中已上传的文件及其 fileId。" +
        "使用返回的文件引用（格式: \"fileName:fileId\"）作为其他 MCP Tool 的文件参数值。" +
        "可通过 searchKeyword 按文件名搜索，不传则返回最近上传的文件列表。")]
    public static async Task<string> ListAvailableFiles(
        [Description("按文件名搜索的关键词（可选，不传则列出最近的文件）")] string? searchKeyword = null)
    {
        try
        {
            if (_apiServerUrl == null)
                return "错误: FileTools 未配置 API 服务器地址";

            var query = string.IsNullOrWhiteSpace(searchKeyword)
                ? ""
                : $"?keyword={Uri.EscapeDataString(searchKeyword)}";

            var response = await _httpClient.GetAsync($"/api/file/searchFiles{query}");

            if (!response.IsSuccessStatusCode)
                return $"查询文件列表失败 (HTTP {(int)response.StatusCode})";

            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (files == null || files.Count == 0)
                return string.IsNullOrWhiteSpace(searchKeyword)
                    ? "没有找到任何文件。请先使用 UploadFileForMcpTool 上传文件。"
                    : $"没有找到匹配 '{searchKeyword}' 的文件。";

            var sb = new StringBuilder();
            sb.AppendLine(string.IsNullOrWhiteSpace(searchKeyword)
                ? $"找到 {files.Count} 个文件（按时间倒序）:"
                : $"搜索 '{searchKeyword}' 找到 {files.Count} 个文件:");
            sb.AppendLine();

            foreach (var f in files)
            {
                var fName = f.TryGetProperty("fileName", out var nameProp) ? nameProp.GetString() : "?";
                var fId = f.TryGetProperty("fileId", out var idProp) ? idProp.GetString() : "?";
                var uploadDate = f.TryGetProperty("fileUploadDate", out var dateProp) ? dateProp.GetString() : "";
                var uploader = f.TryGetProperty("fileUploader", out var uploaderProp) ? uploaderProp.GetString() : "";

                // 用于其他 MCP Tool 的文件引用格式
                sb.AppendLine($"- 引用值: \"{fName}:{fId}\"  |  文件名: {fName}  |  上传者: {uploader}  |  日期: {uploadDate}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("使用方法: 复制上面的「引用值」（如 \"report.docx:xxxx\"），直接作为其他 MCP Tool 的文件参数值传入。");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"查询文件列表异常: {ex.Message}";
        }
    }
}
