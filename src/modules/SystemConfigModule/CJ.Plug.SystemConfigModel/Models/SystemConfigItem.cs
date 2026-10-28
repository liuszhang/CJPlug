namespace CJ.Plug.SystemConfigModel.Models;

/// <summary>
/// 系统配置项（Key/Value 通用键值表）
/// </summary>
public class SystemConfigItem
{
    public int Id { get; set; }

    /// <summary>配置键（如 RemoteViewMode）</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>配置值（如 fullscreen / window）</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>配置说明</summary>
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
}
