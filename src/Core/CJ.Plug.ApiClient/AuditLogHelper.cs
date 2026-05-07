using CJ.Plug.AuditModels;
using Serilog;
using System.Net.Http.Json;

/// <summary>
/// 审计日志辅助类
/// </summary>
public class AuditLogHelper
{
    private readonly HttpClient _httpClient;
    private readonly Func<Task<string>> _getUserNameFunc;

    public AuditLogHelper(HttpClient httpClient, Func<Task<string>> getUserNameFunc)
    {
        _httpClient = httpClient;
        _getUserNameFunc = getUserNameFunc;
    }

    /// <summary>
    /// 记录审计日志
    /// </summary>
    public async Task LogAsync(
        AuditModule module,
        AuditOperationType operationType,
        string description,
        string? detail = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            // 获取当前用户名
            var userName = "anonymous";
            try
            {
                userName = await _getUserNameFunc() ?? "anonymous";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "获取用户名失败，使用默认值");
            }

            var request = new CreateAuditLogRequest
            {
                UserName = userName,
                OperationType = operationType,
                Module = module,
                Description = description,
                Detail = detail,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            Log.Debug("准备记录审计日志: {Module} - {Operation} - {Description}", module, operationType, description);

            // 同步记录，确保日志被保存
            var response = await _httpClient.PostAsJsonAsync("/api/audit/log", request);
            
            if (response.IsSuccessStatusCode)
            {
                Log.Debug("审计日志记录成功: {Description}", description);
            }
            else
            {
                Log.Warning("审计日志记录失败: {StatusCode} - {Description}", response.StatusCode, description);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "记录审计日志异常: {Description}", description);
        }
    }

    /// <summary>
    /// 记录操作成功日志
    /// </summary>
    public Task LogSuccessAsync(AuditModule module, AuditOperationType operationType, string description, string? detail = null)
        => LogAsync(module, operationType, description, detail, true);

    /// <summary>
    /// 记录操作失败日志
    /// </summary>
    public Task LogFailureAsync(AuditModule module, AuditOperationType operationType, string description, string? errorMessage, string? detail = null)
        => LogAsync(module, operationType, description, detail, false, errorMessage);
}
