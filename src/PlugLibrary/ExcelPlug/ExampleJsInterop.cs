using Microsoft.JSInterop;
using Serilog;
using System.Text.Json.Nodes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ExcelPlug
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class ExampleJsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        //private readonly Lazy<Task<IJSObjectReference>> moduleTask2;
        private readonly Lazy<Task<IJSObjectReference>> moduleTask3;
        private readonly IJSRuntime JsRuntime;

        public ExampleJsInterop(IJSRuntime jsRuntime)
        {
            JsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

            moduleTask3 = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/ExcelPlug/hst/handsontable.full.min.js").AsTask());
            //moduleTask2 = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            //    "import", "./_content/ExcelPlug/js/xlsx.mjs").AsTask());
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/ExcelPlug/exampleJsInterop.js").AsTask());
        }

        public async ValueTask<string> Prompt(string message)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("showPrompt", message);
        }

        public async Task InitializeAsync(string elementId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initSpreadsheet", elementId);
        }

        public async Task InitHandsonTableAsync(string elementId)
        {
            try
            {
                //await moduleTask3.Value;
                //await JsRuntime.InvokeVoidAsync("import", "./_content/ExcelPlug/exampleJsInterop.js");
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("initHandsonTable", elementId);
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                Log.Information(ex.StackTrace);
            }

        }

        //调用sheetJs导出数据
        public async Task ExportDataset(JsonObject DataRows)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("export_method", DataRows);
        }

        private async Task ExportHTML()
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("export_html", "weather-table");
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
