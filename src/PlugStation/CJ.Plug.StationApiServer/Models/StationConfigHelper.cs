using Microsoft.Data.Sqlite;

namespace CJ.Plug_Aspire.StationApiService.Models;

/// <summary>
/// 从 StationSettingUI 共享的 SQLite 配置中读取平台服务地址等配置
/// 数据库位置: StationSettingUI 启动程序所在目录
/// </summary>
public static class StationConfigHelper
{
    /// <summary>
    /// 从 SQLite 读取 MainServerUrl，读取失败返回 null
    /// </summary>
    public static string? ReadMainServerUrl()
    {
        return ReadConfigValue("MainServerUrl");
    }

    /// <summary>
    /// 从 SQLite 读取 ToolsRootPath（工具安装根目录），读取失败返回 null
    /// </summary>
    public static string? ReadToolsRootPath()
    {
        return ReadConfigValue("ToolsRootPath");
    }

    /// <summary>
    /// 从 SQLite 读取 StationApiPort（本地服务端口），读取失败返回 null
    /// </summary>
    public static string? ReadStationApiPort()
    {
        return ReadConfigValue("StationApiPort");
    }

    private static string? ReadConfigValue(string key)
    {
        try
        {
            var dbPath = FindDatabasePath();
            if (dbPath == null)
                return null;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM AppConfig WHERE Key = @key";
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StationConfigHelper] 读取 {key} 失败: {ex.Message}");
            return null;
        }
    }

    private static string? FindDatabasePath()
    {
        // 优先从共享目录 %ProgramData%\CJStation 读取
        var sharedDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CJStation",
            "station_config.db");
        if (File.Exists(sharedDbPath))
            return sharedDbPath;

        // 兜底：从 StationSettingUI 所在目录读取（兼容旧版本）
        var stationSettingUiPath = FindStationSettingUiPath();
        if (stationSettingUiPath != null)
        {
            var legacyDbPath = Path.Combine(stationSettingUiPath, "station_config.db");
            if (File.Exists(legacyDbPath))
                return legacyDbPath;
        }

        return null;
    }

    /// <summary>
    /// 查找 StationSettingUI 启动程序所在目录
    /// 策略：从当前进程的父目录、同级目录等常见位置查找
    /// </summary>
    private static string? FindStationSettingUiPath()
    {
        // 策略 1: 当前进程所在目录（如果 StationApiServer 和 StationSettingUI 在同一目录）
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var dbPath = Path.Combine(currentDir, "station_config.db");
        if (File.Exists(dbPath))
            return currentDir;

        // 策略 2: 向上查找父目录
        var parentDir = Directory.GetParent(currentDir)?.FullName;
        if (parentDir != null)
        {
            dbPath = Path.Combine(parentDir, "station_config.db");
            if (File.Exists(dbPath))
                return parentDir;
        }

        // 策略 3: 在常见部署目录中查找
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "PlugStation", "StationSettingUI"),
            Path.Combine(currentDir, "..", "StationSettingUI"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CJStation"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CJStation"),
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                dbPath = Path.Combine(fullPath, "station_config.db");
                if (File.Exists(dbPath))
                    return fullPath;
            }
            catch { }
        }

        return null;
    }
}