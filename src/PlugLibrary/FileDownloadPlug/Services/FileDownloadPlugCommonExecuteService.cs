
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Elsa.Http.Contexts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using FileDownloadPlug;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Parlot.Fluent;
using Polly;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

public class FileDownloadPlugCommonExecuteService : IPlugCommonExecute
{
    public object? parsedContent { get; set; }
    private ActivityExecutionContext activityContext { get; set; }


    public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plugToExecute = context.plugToExecute;
        
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

        Log.Information($"execute rest plug: {plugToExecute.Name}");
        await TrySendAsync(activityContext, plugToExecute);
        return null;
    }
    private async Task TrySendAsync(ActivityExecutionContext context, Plug Plug)
    {
        var request = PrepareRequest(Plug);
        //var logger = (ILogger)context.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        //var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        //var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequestBase));
        //var cancellationToken = context.CancellationToken;

        var httpClient = new HttpClient();
        Console.WriteLine(request.RequestUri?.ToString());

        try
        {
            var response = await httpClient.SendAsync(request);
            Console.WriteLine(JsonSerializer.Serialize(response));
            parsedContent = await ParseContentAsync(response);
            var statusCode = (int)response.StatusCode;
            var responseHeaders = new Elsa.Http.HttpHeaders(response.Headers);

            //Log.Information($"{parsedContent?.ToString()}");
            //context.Set(Result, response);
            //context.Set(ParsedContent, parsedContent);
            //context.Set(StatusCode, statusCode);
            //context.Set(ResponseHeaders, responseHeaders);

            //await HandleResponseAsync(context, response);
        }
        catch (HttpRequestException e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
            //context.AddExecutionLogEntry("Error", e.Message, payload: new
            //{
            //    StackTrace = e.StackTrace
            //});
            //context.JournalData.Add("Error", e.Message);
            //await HandleRequestExceptionAsync(context, e);
        }
        catch (TaskCanceledException e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
            //context.AddExecutionLogEntry("Error", e.Message, payload: new
            //{
            //    StackTrace = e.StackTrace
            //});
            //context.JournalData.Add("Cancelled", true);
            //await HandleTaskCanceledExceptionAsync(context, e);
        }
        catch (Exception e)
        {
            Log.Warning(e, "An error occurred while sending an HTTP request");
        }
    }

    private HttpRequestMessage PrepareRequest(Plug Plug)
    {
        var method = Plug.GetPlugSetting(PlugSettingKey.Method.ToString()) ?? "Get";
        var url = Plug.GetPlugSetting(PlugSettingKey.Url.ToString()) ?? "";
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        //request.RequestUri = new Uri("http://localhost:5123/api/dispatch/GetApiServer");
        request.RequestUri = new Uri(url);
        Console.WriteLine(request.RequestUri);

        var headers = Plug.GetPlugSettings()?.Settings.Where(s => s.Key.Contains("Header"));
        var authorization = Plug.GetPlugSetting(PlugSettingKey.Authorization.ToString());
        var addAuthorizationWithoutValidation = Plug.GetPlugSetting(PlugSettingKey.DisableAuthorizationHeaderValidation.ToString());

        if (!string.IsNullOrWhiteSpace(authorization))
            if (addAuthorizationWithoutValidation == "true")
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            else
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value);

        var contentType = Plug.GetPlugSetting(PlugSettingKey.ContentType.ToString());
        var content = Plug.GetPlugSetting(PlugSettingKey.Content.ToString()) ?? "";

        if (contentType != null && content != null)
        {
            request.Content = new StringContent(content, Encoding.UTF8, contentType);
            //var factories = context.GetServices<IHttpContentFactory>();
            //var factory = SelectContentWriter(contentType, factories);
            //request.Content = factory.CreateHttpContent(content, contentType);
        }

        return request;
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

    private IHttpContentFactory SelectContentWriter(string? contentType, IEnumerable<IHttpContentFactory> factories)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return new JsonContentFactory();

        var parsedContentType = new System.Net.Mime.ContentType(contentType);
        return factories.FirstOrDefault(httpContentFactory => httpContentFactory.SupportedContentTypes.Any(c => c == parsedContentType.MediaType)) ?? new JsonContentFactory();
    }

    public static async Task<object?> ParseContentAsync(Stream content, string contentType, Type? returnType, Dictionary<string, string?[]> headers, CancellationToken cancellationToken = default!)
    {
        //var parsers = context.GetServices<IHttpContentParser>().OrderByDescending(x => x.Priority).ToList();
        var httpResponseParserContext = new HttpResponseParserContext(content, contentType, returnType, headers, cancellationToken);
        //var contentParser = parsers.FirstOrDefault(x => x.GetSupportsContentType(httpResponseParserContext));

        if (httpResponseParserContext == null)
            return null;

        return httpResponseParserContext.Content;
    }

    //public static IEnumerable<KeyValuePair<string, string[]>> GetHeaders(Input input)
    //{
    //    var value = context.Get(input.MemoryBlockReference());

    //    return value switch
    //    {
    //        IDictionary<string, string[]> dictionary1 => dictionary1,
    //        IDictionary<string, string> dictionary2 => dictionary2.ToDictionary(x => x.Key, x => new[] { x.Value }),
    //        IDictionary<string, object> dictionary3 => dictionary3.ToDictionary(pair => pair.Key, pair => pair.Value is ICollection<object> collection ? collection.Select(x => x.ToString()!).ToArray() : new[] { pair.Value.ToString()! }),
    //        _ => Array.Empty<KeyValuePair<string, string[]>>()
    //    };
    //}



}

