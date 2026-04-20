using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.SharedPages.DataConnector.Interop;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CJ.Plug.SharedPages.DataConnector.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public class DataConnectorJsInterop(IJSRuntime jsRuntime, IServiceProvider serviceProvider) : JsInteropBase(jsRuntime)
{
    protected override string ModuleName => "exampleJsInterop";

    public async ValueTask<string> Prompt(string message)
    {
        return await TryInvokeAsync(async module =>
        {
            return await module.InvokeAsync<string>("showPrompt", message);
        });
    }
}