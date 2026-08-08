using CJ.Plug.SystemConfigModel.Models;

namespace CJ.Plug.SystemConfigApiClient;

public interface ISystemConfigApiClient
{
    /// <summary>获取全部配置项</summary>
    Task<List<SystemConfigItem>> GetAllAsync(CancellationToken ct = default);

    /// <summary>按键获取配置值（不存在返回 null）</summary>
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);

    /// <summary>设置配置值（不存在则新增）</summary>
    Task<SystemConfigItem?> SetValueAsync(string key, string value, string? description = null, CancellationToken ct = default);
}
