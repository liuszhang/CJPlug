using StationSettingUI.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CJ.Plug.Models.Station;

namespace StationSettingUI.Services;

/// <summary>
/// HTTP 客户端服务 - 封装对 StationApiServer 和平台主服务器的 API 调用
/// 
/// 架构说明:
///   StationSettingUI ──HTTP──> StationApiServer(本地) ──SignalR──> DispatchServer(主服务器)
/// 
/// - StationApiServer (本地): 文件执行、连接状态、本地服务管理
/// - DispatchServer (主服务器): 工具管理 API、调度分发
/// </summary>
public class StationApiService
{
    private HttpClient _stationClient;
    private readonly AppConfig _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public StationApiService(AppConfig config)
    {
        _config = config;
        _stationClient = new HttpClient
        {
            BaseAddress = new Uri(config.StationApiUrl),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    // ==================== StationApiServer (本地) ====================

    /// <summary>
    /// 更新本地 StationApiServer 的 BaseAddress（端口变更后调用）
    /// </summary>
    public void UpdateBaseAddress()
    {
        _stationClient = new HttpClient
        {
            BaseAddress = new Uri(_config.StationApiUrl),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    /// <summary>
    /// 测试本地 StationApiServer 是否可用
    /// </summary>
    public async Task<bool> TestStationApiAsync()
    {
        try
        {
            var response = await _stationClient.GetAsync("/api/station/test");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取 StationApiServer 与主服务器的 WebSocket 连接状态
    /// </summary>
    public async Task<ConnectionStatusResponse?> GetConnectionStatusAsync()
    {
        try
        {
            var response = await _stationClient.GetAsync("/api/station/connection-status");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ConnectionStatusResponse>(JsonOptions);
                if (result != null)
                    return result;
            }
            System.Diagnostics.Debug.WriteLine(
                $"获取连接状态返回: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"连接状态请求失败 (HTTP): {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("连接状态请求超时");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取连接状态异常: {ex.GetType().Name}: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 测试平台连接
    /// </summary>
    public async Task<(bool Success, string Message)> TestMainServerAsync(string? mainServerUrl = null)
    {
        // 1. 先检查本地 StationApiServer 是否运行
        var localOk = await TestStationApiAsync();
        if (!localOk)
            return (false, "本地图站服务未运行，请先启动服务");

        // 2. 通过 StationApiServer 获取 WebSocket 连接状态
        var connStatus = await GetConnectionStatusAsync();
        if (connStatus != null)
        {
            if (connStatus.HubConnected)
                return (true, $"平台连接正常 (WebSocket → {connStatus.MainServerUrl})");

            // WebSocket 未连接，尝试直接 HTTP
            var directResult = await TryDirectConnectAsync(mainServerUrl ?? connStatus.MainServerUrl);
            if (directResult.Success)
                return (true, $"HTTP 可达但 WebSocket 未连接 (服务器: {connStatus.MainServerUrl})");

            return (false, $"平台连接失败 - 无法连接到 {connStatus.MainServerUrl}");
        }

        // 3. 降级：无法获取连接状态，直接尝试 HTTP ping
        var fallbackUrl = mainServerUrl ?? _config.MainServerUrl;
        var fallback = await TryDirectConnectAsync(fallbackUrl);
        if (fallback.Success)
            return (true, $"HTTP 连接正常 ({fallbackUrl})，WebSocket 状态未知");

        return (false, $"无法连接到平台服务器 ({fallbackUrl})");
    }

    /// <summary>
    /// 直接 HTTP 探测服务器（降级方案）
    /// </summary>
    private async Task<(bool Success, string Message)> TryDirectConnectAsync(string? serverUrl)
    {
        var url = serverUrl ?? _config.MainServerUrl;
        if (string.IsNullOrWhiteSpace(url))
            return (false, "未配置平台服务地址");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var baseUrl = url.TrimEnd('/');

            var testEndpoints = new[] { "/alive", "/health", "/swagger", "/mainHub/negotiate?negotiateVersion=1" };
            foreach (var endpoint in testEndpoints)
            {
                try
                {
                    var response = await client.GetAsync($"{baseUrl}{endpoint}");
                    if (response.IsSuccessStatusCode)
                        return (true, $"HTTP {endpoint} 可达");
                }
                catch { /* 继续尝试下一个 */ }
            }

            return (false, "HTTP 端口不可达");
        }
        catch (Exception ex)
        {
            return (false, $"网络不可达: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查平台服务器版本
    /// </summary>
    public async Task<(bool HasUpdate, string? LatestVersion, string Message)> CheckUpdateAsync()
    {
        try
        {
            // 先通过 StationApiServer 获取连接状态信息
            var connStatus = await GetConnectionStatusAsync();
            if (connStatus == null)
                return (false, null, "无法获取服务器信息");

            if (!connStatus.HubConnected)
                return (false, null, "未连接到平台服务器，无法检查更新");

            // 通过 StationApiServer 尝试获取版本
            // (实际版本获取需主服务器提供对应端点，这里用现有信息)
            var currentVersion = AppConfig.AppVersion;
            return (false, null, $"当前版本: {currentVersion}，服务器: {connStatus.MainServerUrl}");
        }
        catch (Exception ex)
        {
            return (false, null, $"检查失败: {ex.Message}");
        }
    }

    // ==================== 工具管理 API (通过主服务器) ====================

    /// <summary>
    /// 创建指向主服务器的 HttpClient
    /// </summary>
    private HttpClient CreateMainServerClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(_config.MainServerUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    /// <summary>
    /// 通过本地 StationApiServer 代理获取主服务器上的所有工具列表
    /// </summary>
    public async Task<List<Tool>?> FetchToolsFromServerAsync()
    {
        try
        {
            var response = await _stationClient.GetAsync("/api/station/tools");
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorMsg = string.IsNullOrWhiteSpace(errorBody)
                    ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    : errorBody.Trim();
                throw new HttpRequestException($"获取工具列表失败: {errorMsg}");
            }

            return await response.Content.ReadFromJsonAsync<List<Tool>>(JsonOptions);
        }
        catch (HttpRequestException)
        {
            throw; // 直接向上抛出，保留原始信息
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取工具列表失败: {ex.Message}");
            throw new HttpRequestException($"获取工具列表失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过 StationApiServer 检查图站上指定文件是否存在
    /// </summary>
    public async Task<bool> CheckFileExistsAsync(string filePath)
    {
        try
        {
            var body = new { filePath };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _stationClient.PostAsync("/api/station/fileExists", content);
            if (!response.IsSuccessStatusCode)
                return false;
            var result = await response.Content.ReadAsStringAsync();
            return bool.TryParse(result, out var exists) && exists;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"检查文件存在性失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 通过 StationApiServer 从主服务器下载工具包并解压到图站的指定目录
    /// </summary>
    public async Task<bool> DownloadToolToStationAsync(string toolName, string toolVersion, string targetPath, string? toolFilePath = null)
    {
        try
        {
            var body = new
            {
                targetPath,
                toolName,
                toolVersion,
                toolFilePath
            };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _stationClient.PostAsync("/api/station/downloadTool", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"下载工具到图站失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从主服务器获取单个工具详情
    /// </summary>
    public async Task<Tool?> GetToolByIdAsync(int toolId)
    {
        try
        {
            using var client = CreateMainServerClient();
            var response = await client.GetAsync($"/api/Tool/GetToolById/{toolId}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<Tool>(JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取工具详情失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 下载工具配置到本地文件
    /// </summary>
    public async Task<string?> DownloadToolConfigAsync(int toolId)
    {
        try
        {
            using var client = CreateMainServerClient();
            var response = await client.GetAsync($"/api/Tool/GetToolById/{toolId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var filePath = Path.Combine(_config.ToolsRootPath, $"tool_{toolId}.config.json");
            Directory.CreateDirectory(_config.ToolsRootPath);
            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"下载工具配置失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 向主服务器注册/创建工具
    /// </summary>
    public async Task<bool> CreateToolAsync(Tool tool)
    {
        try
        {
            using var client = CreateMainServerClient();
            var json = JsonSerializer.Serialize(tool);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/Tool/CreateTool", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建工具失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 向主服务器更新工具
    /// </summary>
    public async Task<bool> UpdateToolAsync(Tool tool)
    {
        try
        {
            using var client = CreateMainServerClient();
            var json = JsonSerializer.Serialize(tool);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync("/api/Tool/UpdateTool", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新工具失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 保存工具（创建或更新）
    /// </summary>
    public async Task<bool> SaveToolAsync(Tool tool)
    {
        if (tool.Id.HasValue && tool.Id > 0)
            return await UpdateToolAsync(tool);
        else
            return await CreateToolAsync(tool);
    }

    /// <summary>
    /// 从主服务器删除工具
    /// </summary>
    public async Task<bool> DeleteToolAsync(int toolId)
    {
        try
        {
            using var client = CreateMainServerClient();
            var response = await client.DeleteAsync($"/api/Tool/DeleteTool/{toolId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除工具失败: {ex.Message}");
            return false;
        }
    }

    // ==================== 任务管理 API (StationApiServer 本地) ====================

    /// <summary>
    /// 从本地 StationApiServer 获取任务列表
    /// </summary>
    public async Task<List<StationTaskInfo>?> GetTasksAsync()
    {
        try
        {
            var response = await _stationClient.GetAsync("/api/station/tasks");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<List<StationTaskInfo>>(JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取任务列表失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 停止指定任务（终止对应进程）
    /// </summary>
    public async Task<bool> StopTaskAsync(int taskId)
    {
        try
        {
            var response = await _stationClient.PostAsync($"/api/station/tasks/{taskId}/stop", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"停止任务失败: {ex.Message}");
            return false;
        }
    }

    // ==================== 远程桌面 API (StationApiServer 本地) ====================

    /// <summary>
    /// 获取远程桌面服务状态
    /// </summary>
    public async Task<RemoteServiceStatus?> GetRemoteServiceStatusAsync()
    {
        try
        {
            var response = await _stationClient.GetAsync("/api/station/remote/status");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<RemoteServiceStatus>(JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取远程桌面状态失败: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 部署 UltraVNC portable
    /// </summary>
    public async Task<(bool Success, string Message)> DeployUvncAsync()
    {
        try
        {
            var response = await _stationClient.PostAsync("/api/station/remote/uvnc/deploy", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "未知结果");
        }
        catch (Exception ex)
        {
            return (false, $"请求失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动 VNC 服务
    /// </summary>
    public async Task<(bool Success, string Message)> StartVncAsync()
    {
        try
        {
            var response = await _stationClient.PostAsync("/api/station/remote/vnc/start", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "未知结果");
        }
        catch (Exception ex)
        {
            return (false, $"请求失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止 VNC 服务
    /// </summary>
    public async Task<(bool Success, string Message)> StopVncAsync()
    {
        try
        {
            var response = await _stationClient.PostAsync("/api/station/remote/vnc/stop", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "未知结果");
        }
        catch (Exception ex)
        {
            return (false, $"请求失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动 SSH 服务
    /// </summary>
    public async Task<(bool Success, string Message)> StartSshAsync()
    {
        try
        {
            var response = await _stationClient.PostAsync("/api/station/remote/ssh/start", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "未知结果");
        }
        catch (Exception ex)
        {
            return (false, $"请求失败: {ex.Message}");
        }
    }

    // ==================== 内部辅助类型 ====================

    private class ApiResult
    {
        public string? Message { get; set; }
    }

    public class RemoteServiceStatus
    {
        public bool VncInstalled { get; set; }
        public bool VncRunning { get; set; }
        public int VncPort { get; set; } = 5900;
        public string? VncProcessName { get; set; }
        public bool VncIsPortable { get; set; }
        public bool SshInstalled { get; set; }
        public bool SshRunning { get; set; }
        public int SshPort { get; set; } = 22;
        public string? SshProcessName { get; set; }
        public bool IsWindows { get; set; }
    }

    public class ConnectionStatusResponse
    {
        public bool HubConnected { get; set; }
        public string? MainServerUrl { get; set; }
        public string? LocalTime { get; set; }
    }
}
