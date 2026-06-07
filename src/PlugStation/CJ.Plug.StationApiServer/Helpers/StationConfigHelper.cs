using Microsoft.Data.Sqlite;

namespace CJ.Plug_Aspire.StationApiService.Helpers;

/// <summary>
/// 从 StationSettingUI 共享的 SQLite 配置数据库读取配置项。
/// 数据库路径与 StationSettingUI.Services.StationConfigService 保持一致：
/// %ProgramData%\CJStation\station_config.db
/// </summary>
public static class StationConfigHelper
{
    private static readonly string DbDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "CJStation");

    private static readonly string DbPath = Path.Combine(DbDir, "station_config.db");

    /// <summary>
    /// 从 SQLite 读取 MainServerUrl，数据库不存在或读取失败返回 null
    /// </summary>
    public static string? ReadMainServerUrl()
    {
        return ReadConfigValue("MainServerUrl");
    }

    /// <summary>
    /// 从 SQLite 读取 ToolsRootPath（工具安装根目录），数据库不存在或读取失败返回 null
    /// </summary>
    public static string? ReadToolsRootPath()
    {
        return ReadConfigValue("ToolsRootPath");
    }

    private static string? ReadConfigValue(string key)
    {
        try
        {
            if (!File.Exists(DbPath))
                return null;

            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM AppConfig WHERE Key = @key";
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar() as string;

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StationConfigHelper] 读取 {key} 失败: {ex.Message}");
            return null;
        }
    }
}
