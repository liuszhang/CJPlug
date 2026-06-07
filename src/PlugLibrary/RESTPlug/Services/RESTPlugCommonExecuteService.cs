using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using RESTPlug;
using RESTPlug.Utils;

/// <summary>
/// RESTPlug 的通用执行服务。继承 <see cref="ApiPlugExecuteService"/>，
/// 复用 HTTP 请求发送、响应解析、OutputMapping 变量写入等通用逻辑。
/// </summary>
public class RESTPlugCommonExecuteService : ApiPlugExecuteService
{
    /// <inheritdoc/>
    protected override string PlugTypeKey => PlugKeySetting.CommonExecuteKey;

    /// <inheritdoc/>
    protected override string[]? DataPrepareVariableNames =>
        Enum.GetNames(typeof(InitVariableNames));

    public RESTPlugCommonExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    // ========================= BuildRequestAsync =========================

    /// <summary>
    /// 从 PDZ 读取 Url 和 Method 变量，构建 HttpRequestMessage。
    /// </summary>
    protected override async Task<HttpRequestMessage?> BuildRequestAsync(
        PlugDataZone plugDataZone,
        string plugDefinitionId)
    {
        try
        {
            var method = GetVariableValueSafe(plugDataZone, plugDefinitionId,
                InitVariableNames.Method.ToString()) ?? "GET";
            var url = GetVariableValueSafe(plugDataZone, plugDefinitionId,
                InitVariableNames.Url.ToString()) ?? "";

            if (string.IsNullOrEmpty(url))
            {
                CLog.Warning("请求地址为空。构建请求失败。");
                return null;
            }

            var request = new HttpRequestMessage(new HttpMethod(method), url);
            return request;
        }
        catch (Exception ex)
        {
            CLog.Error($"构建 HTTP 请求异常: {ex.Message}");
            CLog.Error(ex.StackTrace);
            return null;
        }
    }

    // ========================= ParseOutputValue =========================

    /// <summary>
    /// 使用 RESTPlug 专用 <see cref="DataParser"/> 解析 OutputMapping 的值。
    /// </summary>
    protected override string? ParseOutputValue(string resultString, string? readSchemaValue)
    {
        return DataParser.GetParsedResult(resultString, readSchemaValue ?? "");
    }
}
