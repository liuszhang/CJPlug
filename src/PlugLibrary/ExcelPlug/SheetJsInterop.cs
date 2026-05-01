using Microsoft.JSInterop;
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

    public class SheetJsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public SheetJsInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/ExcelPlug/js/xlsx.full.min.js").AsTask());
        }


        //调用sheetJs导出数据
        public async Task ExportDataset(JsonObject DataRows)
        {
            var module = await moduleTask.Value;
            var ws = await module.InvokeAsync<object>("json_to_sheet", DataRows);
            var wb = await module.InvokeAsync<object>("book_new", ws,"Data");
            await module.InvokeVoidAsync("writeFile", wb, "SheetJSBlazor.xlsx");



            //await module.InvokeVoidAsync("export_method", DataRows);
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
