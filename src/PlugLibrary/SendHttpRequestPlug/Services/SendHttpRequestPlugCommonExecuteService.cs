using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using SendHttpRequestPlug;
using Serilog;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static MudBlazor.Colors;

public class SendHttpRequestPlugCommonExecuteService : BasePlugExecuteService
{
    public SendHttpRequestPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public object? parsedContent { get; set; }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData;
        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

        Log.Information($"execute SendHttpRequest plug");
        

        return await TrySendAsync(PlugDataZone, plugExecutionRequest?.PlugDefinitionId);

        //return null;
    }

    public async Task<ExecuteResultData?> TrySendAsync(PlugDataZone PlugDataZone,string PlugDefinitionId)
    {
        var request = PrepareRequest(PlugDataZone, PlugDefinitionId);     
        if (request == null)
        {
            return new ExecuteResultData()
            {                
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString ="获取请求体失败"
            };
        }
        var httpClient = new HttpClient();

        try
        {
            var response = await httpClient.SendAsync(request);
            parsedContent = await ParseContentAsync(response);
            var statusCode = (int)response.StatusCode;
            Log.Information($"{statusCode}");
            //Log.Information($"{parsedContent}");
            return new ExecuteResultData()
            {
                ResultString = parsedContent?.ToString(),
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.已完成
            };
        }
        catch (HttpRequestException e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
            return null;
        }
        catch (TaskCanceledException e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
            return null;
        }
        catch (Exception e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
            return null;
        }
    }

    private HttpRequestMessage? PrepareRequest(PlugDataZone PlugDataZone, string PlugDefinitionId)
    {
        try
        {
            //CLog.Information(PlugDataZone.PDZId);
            //CLog.Information(PlugDefinitionId);
            //var method = Plug.GetPlugSetting(PlugSettingKey.Method.ToString()) ?? "Get";
            var method = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.Method.ToString()) ?? "Get";
            var url = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.Url.ToString()) ?? "";
            if (string.IsNullOrEmpty(url))
            {
                CLog.Warning("请求地址为空。获取请求体失败。");
                return null;
            }
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            request.RequestUri = new Uri(url);

            //var headers = Plug.GetPlugSettings()?.Settings.Where(s => s.Key.Contains("Header"));
            //var authorization = Plug.GetPlugSetting(PlugSettingKey.Authorization.ToString());
            //var addAuthorizationWithoutValidation = Plug.GetPlugSetting(PlugSettingKey.DisableAuthorizationHeaderValidation.ToString());

            //if (!string.IsNullOrWhiteSpace(authorization))
            //    if (addAuthorizationWithoutValidation == "true")
            //        request.Headers.TryAddWithoutValidation("Authorization", authorization);
            //    else
            //        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

            //foreach (var header in headers)
            //    request.Headers.Add(header.Key, header.Value);

            //var contentType = Plug.GetPlugSetting(PlugSettingKey.ContentType.ToString());
            //var content = Plug.GetPlugSetting(PlugSettingKey.Content.ToString()) ?? "";

            //if (contentType != null && content != null)
            //{
            //    request.Content = new StringContent(content, Encoding.UTF8, contentType);
            //}

            return request;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            CLog.Error(ex.Message);
            CLog.Error($"Error: {ex.StackTrace}");
            return null;
        }
    }

    private async Task<object?> ParseContentAsync(HttpResponseMessage httpResponse)
    {
        var httpContent = httpResponse.Content;
        if (!HasContent(httpContent))
            return null;

        //var cancellationToken = context.CancellationToken;
        //var targetType = ParsedContent.GetTargetType(context);
        var targetType = typeof(string);
        var contentStream = await httpContent.ReadAsStringAsync();
        var responseHeaders = httpResponse.Headers;
        var contentHeaders = httpContent.Headers;
        var contentType = contentHeaders.ContentType?.MediaType ?? "application/octet-stream";

        targetType ??= contentType switch
        {
            "application/json" => typeof(object),
            _ => typeof(string)
        };

        var contentHeadersDictionary = contentHeaders.ToDictionary(x => x.Key, x => x.Value.Cast<string?>().ToArray(), StringComparer.OrdinalIgnoreCase);
        var responseHeadersDictionary = responseHeaders.ToDictionary(x => x.Key, x => x.Value.Cast<string?>().ToArray(), StringComparer.OrdinalIgnoreCase);
        var headersDictionary = contentHeadersDictionary.Concat(responseHeadersDictionary).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        //return await ParseContentAsync(contentStream, contentType, targetType, headersDictionary);
        return contentStream;
    }
    private static bool HasContent(HttpContent httpContent) => httpContent.Headers.ContentLength > 0;

}

