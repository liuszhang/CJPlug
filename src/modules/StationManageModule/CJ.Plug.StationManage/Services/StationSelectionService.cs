using Microsoft.JSInterop;

namespace CJ.Plug.StationManage.Services
{
    /// <summary>
    /// 管理用户选择的图站，使用 localStorage 持久化保存
    /// </summary>
    public class StationSelectionService
    {
        private const string LocalStorageKey = "cj-plug-selected-station";
        private readonly IJSRuntime _jsRuntime;
        private string? _selectedStationIp;
        private bool _isLoaded;

        public StationSelectionService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// 获取用户选择的图站 IP，如果未选择则返回 null
        /// </summary>
        public async Task<string?> GetSelectedStationIpAsync()
        {
            if (!_isLoaded)
            {
                await LoadFromStorageAsync();
            }
            return _selectedStationIp;
        }

        /// <summary>
        /// 设置用户选择的图站 IP，传入 null 表示清除选择（自动分配）
        /// </summary>
        public async Task SetSelectedStationIpAsync(string? stationIp)
        {
            _selectedStationIp = stationIp;
            await SaveToStorageAsync(stationIp);
        }

        /// <summary>
        /// 从 localStorage 加载保存的选择
        /// </summary>
        private async Task LoadFromStorageAsync()
        {
            try
            {
                _selectedStationIp = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                _isLoaded = true;
            }
            catch
            {
                // localStorage 读取失败时忽略
                _selectedStationIp = null;
                _isLoaded = true;
            }
        }

        /// <summary>
        /// 保存选择到 localStorage
        /// </summary>
        private async Task SaveToStorageAsync(string? stationIp)
        {
            try
            {
                if (string.IsNullOrEmpty(stationIp))
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, stationIp);
                }
            }
            catch
            {
                // localStorage 写入失败时忽略
            }
        }
    }
}
