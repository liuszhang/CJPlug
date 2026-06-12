using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>
/// 接口类插头回退处理器。
/// 继承 <see cref="ApiPlugExecuteService"/>，复用 HTTP 请求发送、OutputMapping 等通用逻辑。
/// 当 PlugTypeKey 无法匹配到具体的接口类执行服务时，通过 Category="接口类" 作为回退。
/// </summary>
public class ApiPlugCategoryFallbackHandler : ApiPlugExecuteService, IPlugCategoryFallbackHandler
{
    public string Category => "接口类";

    /// <summary>
    /// 哨兵值，不会匹配任何真实 PlugTypeKey；此 handler 仅通过 Category 匹配。
    /// </summary>
    protected override string PlugTypeKey => "__ApiPlugCategoryFallback__";

    /// <summary>
    /// 通用 HTTP 变量名，DataPrepare 阶段从 PDZ 或 InputVariables 读取。
    /// </summary>
    protected override string[]? DataPrepareVariableNames =>
        ["Url", "Method", "Headers", "Body", "ContentType"];

    public ApiPlugCategoryFallbackHandler(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// 仅通过 Category 匹配，不匹配任何 PlugTypeKey。
    /// </summary>
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => false;

    /// <summary>
    /// 从 PDZ 读取 Url / Method / Headers / Body，构建通用 HTTP 请求。
    /// 使用大小写不敏感查找，兼容 MCP Tool Schema 中可能的小写参数名（url/method/headers/body）。
    /// </summary>
    protected override Task<HttpRequestMessage?> BuildRequestAsync(
        PlugDataZone plugDataZone, string plugDefinitionId)
    {
        var url = GetVariableIgnoreCase(plugDataZone, plugDefinitionId, "Url");
        var method = GetVariableIgnoreCase(plugDataZone, plugDefinitionId, "Method") ?? "GET";
        var headers = GetVariableIgnoreCase(plugDataZone, plugDefinitionId, "Headers");
        var body = GetVariableIgnoreCase(plugDataZone, plugDefinitionId, "Body");

        if (string.IsNullOrEmpty(url))
        {
            CLog.Error("[接口类回退] 缺少 Url 变量，无法构建 HTTP 请求");
            return Task.FromResult<HttpRequestMessage?>(null);
        }

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (!string.IsNullOrEmpty(headers))
        {
            try
            {
                var headerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(headers);
                if (headerDict != null)
                {
                    foreach (var kv in headerDict)
                        request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }
            catch { /* 解析失败则跳过 Headers */ }
        }

        if (!string.IsNullOrEmpty(body) && request.Method != HttpMethod.Get)
        {
            var contentType = GetVariableIgnoreCase(plugDataZone, plugDefinitionId, "ContentType")
                ?? "application/json";
            request.Content = new StringContent(body, Encoding.UTF8, contentType);
        }

        CLog.Information($"[接口类回退] {method} {url}");
        return Task.FromResult<HttpRequestMessage?>(request);
    }

    /// <summary>
    /// 从 PDZ 的 PlugVariableDatas 中按变量名做大小写不敏感查找。
    /// 绕开基类 <see cref="GetVariableValueSafe"/> 的大小写敏感限制，
    /// 适配 MCP Tool Schema 中可能的小写参数名。
    /// </summary>
    private static string? GetVariableIgnoreCase(PlugDataZone pdz, string plugDefId, string varName)
    {
        try
        {
            return pdz?.PlugVariableDatas?
                .FirstOrDefault(p =>
                    p.PlugDefinitionId == plugDefId &&
                    string.Equals(p.Name, varName, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }
        catch { return null; }
    }
}
