using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text.Json;

namespace CJ.Plug.PlugBaseCore.Services;

/// <summary>
/// 接口类插头（HTTP API 调用）的通用执行基类。
/// 封装了 HTTP 请求发送、响应解析、OutputMapping 变量写入等通用逻辑。
/// 与 <see cref="StationPlugExecuteService"/> 同级，面向接口类插头提供公共基础服务。
///
/// 模板方法执行流程：
/// 1. DataPrepare — 数据准备（从 PDZ 读取所需变量）
/// 2. BuildRequestAsync — 构建 HTTP 请求（子类实现）
/// 3. SendAsync — 发送请求并解析响应
/// 4. ApplyOutputMappingAsync — 输出映射，将响应结果写入 PDZ 变量
/// 5. ReportCompletedResult — 报告完成
///
/// 子类只需：
/// 1. 实现 <see cref="BuildRequestAsync"/> — 从 PDZ 读取变量并构建 HttpRequestMessage
/// 2. 可选重写 <see cref="ParseResponseAsync"/> — 自定义响应解析逻辑
/// 3. 可选重写 <see cref="ParseOutputValue"/> — 自定义输出映射值解析逻辑
/// 4. 可选重写 <see cref="SendAsync"/> — 自定义 HTTP 发送逻辑（如重试/超时等）
/// </summary>
public abstract class ApiPlugExecuteService : BasePlugExecuteService
{
    /// <summary>
    /// 获取该插头绑定的 PlugTypeKey，用于 <see cref="IsThisPlugTypeKey"/> 判断。
    /// </summary>
    protected abstract string PlugTypeKey { get; }

    /// <summary>
    /// 获取用于从 PDZ 读取变量值的变量名数组，传递给 <see cref="BasePlugExecuteService.DataPrepare"/>。
    /// 通常为插头 InitVariableNames 枚举的所有值。
    /// 如果子类不需要 DataPrepare，可返回 null 或空数组。
    /// </summary>
    protected abstract string[]? DataPrepareVariableNames { get; }

    /// <summary>
    /// 获取 OutputMapping 在 PDZ 中存储的变量名（默认为 "OutputMappings"）。
    /// 子类可重写以使用自定义变量名。
    /// </summary>
    protected virtual string OutputMappingVariableName => "OutputMappings";

    // ---- 可选的依赖服务 ----

    /// <summary>
    /// HttpClient 工厂。子类可通过此属性创建 HttpClient 实例。
    /// 在构造函数中从 DI 容器自动解析。
    /// </summary>
    protected IHttpClientFactory? HttpClientFactory { get; }

    /// <summary>
    /// 构造函数。自动从 DI 容器解析 <see cref="IHttpClientFactory"/>（可选）。
    /// </summary>
    protected ApiPlugExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        HttpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
    }

    // ========================= 模板方法：HTTP 请求执行流程 =========================

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == this.PlugTypeKey);

    /// <summary>
    /// 插头通用执行入口。
    /// </summary>
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        var plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData;

        // ---- 阶段 1：数据准备 ----
        if (DataPrepareVariableNames is { Length: > 0 })
        {
            if (!await DataPrepare(plugExecutionRequest, DataPrepareVariableNames))
                return await ReportErrorResult(erd);
        }

        CLog.Information($"execute {PlugTypeKey} plug", PlugDataZone?.PDZId);

        // ---- 阶段 2：构建 HTTP 请求 ----
        var request = await BuildRequestAsync(PlugDataZone!, plugExecutionRequest!.PlugDefinitionId);
        if (request == null)
        {
            CLog.Error("构建 HTTP 请求失败，请求为 null");
            erd!.ResultString = "构建 HTTP 请求失败";
            return await ReportErrorResult(erd);
        }

        // ---- 阶段 3：发送请求并解析响应 ----
        var responseResult = await SendAsync(request, PlugDataZone!, plugExecutionRequest.PlugDefinitionId);
        if (responseResult == null)
        {
            CLog.Error("发送 HTTP 请求失败，响应为 null");
            erd!.ResultString = "发送 HTTP 请求失败";
            return await ReportErrorResult(erd);
        }

        erd!.ResultString = responseResult.ResultString;

        // ---- 阶段 4：OutputMapping（输出映射） ----
        await ApplyOutputMappingAsync(plugExecutionRequest, responseResult.ResultString);

        // ---- 阶段 5：报告完成 ----
        return await ReportCompletedResult(erd);
    }

    // ========================= 子类必须实现 =========================

    /// <summary>
    /// 构建 HTTP 请求消息。子类负责：
    /// 1. 从 PDZ 读取所需变量（Url、Method、Headers、Body 等）
    /// 2. 构建并返回 <see cref="HttpRequestMessage"/> 实例
    /// 3. 若构建失败返回 null
    /// </summary>
    /// <param name="plugDataZone">数据空间</param>
    /// <param name="plugDefinitionId">插头定义 ID</param>
    /// <returns>构建好的 HttpRequestMessage，失败返回 null</returns>
    protected abstract Task<HttpRequestMessage?> BuildRequestAsync(
        PlugDataZone plugDataZone,
        string plugDefinitionId);

    // ========================= 子类可选重写 =========================

    /// <summary>
    /// 发送 HTTP 请求并解析响应。
    /// 默认实现：创建 HttpClient → 发送请求 → 调用 <see cref="ParseResponseAsync"/> 解析响应。
    /// 子类可重写以自定义发送逻辑（如添加重试、超时处理等）。
    /// </summary>
    /// <param name="request">HTTP 请求消息</param>
    /// <param name="plugDataZone">数据空间（用于日志）</param>
    /// <param name="plugDefinitionId">插头定义 ID（用于日志）</param>
    /// <returns>执行结果数据。成功时 ResultString 为响应内容，失败时 ResultString 为错误信息。</returns>
    protected virtual async Task<ExecuteResultData?> SendAsync(
        HttpRequestMessage request,
        PlugDataZone plugDataZone,
        string plugDefinitionId)
    {
        var httpClient = HttpClientFactory?.CreateClient() ?? new HttpClient();

        try
        {
            var response = await httpClient.SendAsync(request);
            var parsedContent = await ParseResponseAsync(response, plugDataZone, plugDefinitionId);
            var statusCode = (int)response.StatusCode;

            CLog.Information($"HTTP 响应状态码: {statusCode}", plugDataZone?.PDZId);

            return new ExecuteResultData()
            {
                ResultString = parsedContent?.ToString(),
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.已完成
            };
        }
        catch (HttpRequestException ex)
        {
            CLog.Error($"HTTP 请求异常: {ex.Message}");
            return new ExecuteResultData()
            {
                ResultString = $"HTTP 请求异常: {ex.Message}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            };
        }
        catch (TaskCanceledException ex)
        {
            CLog.Error($"HTTP 请求超时: {ex.Message}");
            return new ExecuteResultData()
            {
                ResultString = $"HTTP 请求超时: {ex.Message}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            };
        }
        catch (Exception ex)
        {
            CLog.Error($"HTTP 请求失败: {ex.Message}");
            CLog.Error(ex.StackTrace);
            return new ExecuteResultData()
            {
                ResultString = $"HTTP 请求失败: {ex.Message}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            };
        }
    }

    /// <summary>
    /// 解析 HTTP 响应消息，提取响应内容。
    /// 默认实现将响应内容作为字符串读取。
    /// 子类可重写以自定义解析逻辑（如 JSON 解析、二进制处理等）。
    /// </summary>
    /// <param name="response">HTTP 响应消息</param>
    /// <param name="plugDataZone">数据空间（用于日志）</param>
    /// <param name="plugDefinitionId">插头定义 ID（用于日志）</param>
    /// <returns>解析后的响应内容</returns>
    protected virtual async Task<object?> ParseResponseAsync(
        HttpResponseMessage response,
        PlugDataZone plugDataZone,
        string plugDefinitionId)
    {
        var httpContent = response.Content;
        if (httpContent == null || httpContent.Headers.ContentLength == 0)
            return null;

        return await httpContent.ReadAsStringAsync();
    }

    /// <summary>
    /// 解析单个 OutputMapping 的值。使用 JSON 路径表达式从响应字符串中提取值。
    /// 默认实现使用 System.Text.Json 做基础的路径导航（支持 $.a.b.c / .a.b[0].c 格式）。
    /// 子类可重写以使用更复杂的解析器（如 RESTPlug 的 DataParser）。
    /// </summary>
    /// <param name="resultString">响应结果字符串</param>
    /// <param name="readSchemaValue">JSON 路径表达式</param>
    /// <returns>解析得到的值，解析失败返回 null</returns>
    protected virtual string? ParseOutputValue(string resultString, string? readSchemaValue)
    {
        if (string.IsNullOrEmpty(resultString) || string.IsNullOrEmpty(readSchemaValue))
            return null;

        try
        {
            using var document = JsonDocument.Parse(resultString);
            var expr = readSchemaValue.Trim();

            // 去掉 $ 前缀和 / 分隔符，统一按 . 分割
            if (expr.StartsWith("$"))
                expr = expr.Substring(1);
            expr = expr.Replace("/", ".").TrimStart('.');

            // 按 . 和 [idx] 分割路径段
            var segments = new List<string>();
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '.')
                {
                    if (current.Length > 0)
                    {
                        segments.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }
                else if (c == '[')
                {
                    if (current.Length > 0)
                    {
                        segments.Add(current.ToString().Trim());
                        current.Clear();
                    }
                    int j = i + 1;
                    var idxSb = new System.Text.StringBuilder();
                    while (j < expr.Length && expr[j] != ']')
                    {
                        idxSb.Append(expr[j]);
                        j++;
                    }
                    var idxTok = idxSb.ToString().Trim().Trim('"', '\'');
                    segments.Add(idxTok);
                    i = j;
                }
                else
                {
                    current.Append(c);
                }
            }
            if (current.Length > 0)
                segments.Add(current.ToString().Trim());

            JsonElement currentElement = document.RootElement;

            foreach (var segment in segments)
            {
                if (currentElement.ValueKind == JsonValueKind.Array)
                {
                    if (int.TryParse(segment, out int index) && index >= 0)
                    {
                        var arr = currentElement.EnumerateArray().ToList();
                        if (index >= arr.Count) return null;
                        currentElement = arr[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (currentElement.ValueKind == JsonValueKind.Object)
                {
                    // 尝试精确匹配，再尝试忽略大小写匹配
                    if (currentElement.TryGetProperty(segment, out var prop))
                    {
                        currentElement = prop;
                    }
                    else
                    {
                        // 忽略大小写匹配
                        var found = false;
                        foreach (var kvp in currentElement.EnumerateObject())
                        {
                            if (string.Equals(kvp.Name, segment, StringComparison.OrdinalIgnoreCase))
                            {
                                currentElement = kvp.Value;
                                found = true;
                                break;
                            }
                        }
                        if (!found) return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return currentElement.ValueKind switch
            {
                JsonValueKind.String => currentElement.GetString(),
                JsonValueKind.Null => "null",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => currentElement.GetRawText()
            };
        }
        catch (Exception ex)
        {
            CLog.Error($"ParseOutputValue 解析失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 应用 OutputMapping，将响应结果写入 PDZ 变量。
    /// 从 PDZ 读取 OutputMapping 配置，解析响应内容，写入对应变量。
    /// 子类可重写以自定义 OutputMapping 逻辑。
    /// </summary>
    /// <param name="plugExecutionRequest">执行请求</param>
    /// <param name="resultString">响应结果字符串</param>
    protected virtual async Task ApplyOutputMappingAsync(
        PlugExecutionRequest plugExecutionRequest,
        string? resultString)
    {
        if (string.IsNullOrEmpty(resultString))
            return;

        var plugDefinitionId = plugExecutionRequest.ExecuteResultData?.Ids?.PlugDefinitionId;
        if (string.IsNullOrEmpty(plugDefinitionId))
            return;

        var outputMappingVariable = PlugDataZone?.PlugVariableDatas?.FirstOrDefault(p =>
            p.PlugDefinitionId == plugDefinitionId && p.Name == OutputMappingVariableName);

        if (outputMappingVariable == null || string.IsNullOrEmpty(outputMappingVariable.Value))
            return;

        List<DefaultOutputMapping>? outputs;
        try
        {
            outputs = JsonSerializer.Deserialize<List<DefaultOutputMapping>>(outputMappingVariable.Value);
        }
        catch (JsonException ex)
        {
            CLog.Error($"反序列化 OutputMapping 失败: {ex.Message}");
            return;
        }

        if (outputs == null || outputs.Count == 0)
            return;

        PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();

        foreach (var output in outputs)
        {
            var value = ParseOutputValue(resultString, output.ReadSchemaValue);
            output.Value = value;
            CLog.Information($"OutputMapping: {output.OutputName} = {value}", PlugDataZone?.PDZId);
            PlugDataZone?.SetVariableValue(plugDefinitionId, output.OutputName, output.Value);
        }

        await PDZApiClient.CreateOrUpdatePDZ(PlugDataZone!);
    }

    // ========================= 通用工具方法 =========================

    /// <summary>
    /// 从 PDZ 安全获取变量值（带空引用保护）。
    /// </summary>
    /// <param name="plugDataZone">数据空间</param>
    /// <param name="plugDefinitionId">插头定义 ID</param>
    /// <param name="variableName">变量名</param>
    /// <returns>变量值字符串，失败返回 null</returns>
    protected static string? GetVariableValueSafe(PlugDataZone plugDataZone, string plugDefinitionId, string variableName)
    {
        try
        {
            return plugDataZone?.GetVariableValue(plugDefinitionId, variableName);
        }
        catch (Exception ex)
        {
            CLog.Error($"获取变量 {variableName} 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 判断 HTTP 内容是否有正文（非空内容）。
    /// </summary>
    protected static bool HasContent(HttpContent? httpContent) =>
        httpContent?.Headers.ContentLength > 0;
}
