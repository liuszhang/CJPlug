using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using SendHttpRequestPlug;

/// <summary>
/// SendHttpRequest 插头的通用执行服务。继承 <see cref="ApiPlugExecuteService"/>，
/// 复用 HTTP 请求发送、响应解析等通用逻辑。
/// 不执行 OutputMapping（由调用方处理）。
/// </summary>
public class SendHttpRequestPlugCommonExecuteService : ApiPlugExecuteService
{
    /// <inheritdoc/>
    protected override string PlugTypeKey => PlugKeySetting.CommonExecuteKey;

    /// <inheritdoc/>
    protected override string[]? DataPrepareVariableNames =>
        Enum.GetNames(typeof(InitVariableNames));

    public SendHttpRequestPlugCommonExecuteService(IServiceProvider serviceProvider)
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
                CLog.Warning("请求地址为空。获取请求体失败。");
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

    // ========================= OutputMapping（跳过） =========================

    /// <summary>
    /// SendHttpRequestPlug 不执行 OutputMapping，由调用方处理。
    /// </summary>
    protected override Task ApplyOutputMappingAsync(
        PlugExecutionRequest plugExecutionRequest, string? resultString)
    {
        return Task.CompletedTask;
    }

    // ========================= 公开兼容方法 =========================

    /// <summary>
    /// 兼容旧接口的公开方法：构建请求 → 发送 → 返回结果。
    /// 供需要直接调用此服务的代码使用。
    /// </summary>
    public async Task<ExecuteResultData?> TrySendAsync(
        PlugDataZone plugDataZone,
        string plugDefinitionId)
    {
        PlugDataZone = plugDataZone;

        var request = await BuildRequestAsync(plugDataZone, plugDefinitionId);
        if (request == null)
        {
            return new ExecuteResultData()
            {
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString = "获取请求体失败"
            };
        }

        return await SendAsync(request, plugDataZone, plugDefinitionId);
    }
}
