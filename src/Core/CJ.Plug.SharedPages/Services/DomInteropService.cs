using Blazored.LocalStorage;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.SharedPages.Contracts;
using Microsoft.JSInterop;
using Serilog;

namespace CJ.Plug.SharedPages.Services
{
    public class DomInteropService : IDomInteropService
    {
        private ILocalStorageService LocalStorageService;
        private IJSRuntime JsRuntime;

        public DomInteropService(ILocalStorageService localStorageService, IJSRuntime jSRuntime)
        {
            LocalStorageService = localStorageService;
            JsRuntime = jSRuntime;
        }

        public async Task<int?> GetCurrentUserId()
        {
            return await LocalStorageService.GetItemAsync<int?>("userid");
        }

        public async Task<string?> GetCurrentUserName()
        {
            return await LocalStorageService.GetItemAsync<string?>("username");
        }

        public async Task<string?> GetItemValue(string key)
        {
            return await LocalStorageService.GetItemAsync<string?>(key);
        }

        public async Task SetCurrentUserId(int id)
        {
            await LocalStorageService.SetItemAsync<int>("userid", id);
        }

        public async Task SetCurrentUserName(string userName)
        {
            await LocalStorageService.SetItemAsStringAsync("username", userName);
        }

        public async Task ClearCurrentUser()
        {
            await LocalStorageService.RemoveItemAsync("userid");
            await LocalStorageService.RemoveItemAsync("username");
        }

        public async Task SetItemValue(string key, string value)
        {
            await LocalStorageService.SetItemAsStringAsync(key, value);
        }

        public async Task CopyText(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            }
            catch (Exception ex)
            {
                // 处理异常情况
                Log.Information("复制文本到剪贴板时出错: " + ex.Message);
                //await JsRuntime.InvokeVoidAsync("document.getElementById('result').textContent", "复制文本到剪贴板时出错: " + ex.Message);
            }
        }

        public async Task SetDragPayload(object? value)
        {
            await LocalStorageService.SetItemAsync("DragPayload", value);
        }

        public async Task<object?> GetDragPayload()
        {
            return await LocalStorageService.GetItemAsync<object?>("DragPayload");
        }

        public async Task<string?> GetPDZId()
        {
            return await GetItemValue("PDZId");
        }
    }
}
