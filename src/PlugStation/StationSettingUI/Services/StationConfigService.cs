using Microsoft.Data.Sqlite;
using StationSettingUI.Models;

namespace StationSettingUI.Services;

/// <summary>
/// 应用配置持久化服务 — 基于 SQLite
/// 数据库位置: %ProgramData%\CJStation\station_config.db
/// </summary>
public class StationConfigService
{
    private static readonly string DbDir = Path.Combine(
        //Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        AppContext.BaseDirectory,
        "CJStation");

    private static readonly string DbPath = Path.Combine(DbDir, "station_config.db");

    private AppConfig? _config;

    /// <summary>
    /// 加载配置，首次运行时自动创建数据库和默认配置
    /// </summary>
    public AppConfig LoadConfig()
    {
        if (_config != null)
            return _config;

        Directory.CreateDirectory(DbDir);
        InitDatabase();

        _config = ReadFromDb() ?? new AppConfig();
        return _config;
    }

    /// <summary>
    /// 保存所有配置到 SQLite
    /// </summary>
    public void SaveConfig()
    {
        if (_config == null) return;

        Directory.CreateDirectory(DbDir);
        InitDatabase();
        WriteToDb(_config);
    }

    // ==================== SQLite 操作 ====================

    private void InitDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS AppConfig (
                Key   TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private AppConfig? ReadFromDb()
    {
        try
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var dict = new Dictionary<string, string?>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM AppConfig";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dict[reader.GetString(0)] = reader.GetString(1);
            }

            if (dict.Count == 0) return null;

            return new AppConfig
            {
                MainServerUrl = dict.GetValueOrDefault("MainServerUrl") ?? "http://127.0.0.1:6660",
                StationApiFolder = dict.GetValueOrDefault("StationApiFolder") ?? "",
                StationApiPort = int.TryParse(dict.GetValueOrDefault("StationApiPort"), out var p) ? p : 7660,
                ToolsRootPath = dict.GetValueOrDefault("ToolsRootPath") ?? @"C:\Program Files\CJTools",
                AutoStartService = dict.GetValueOrDefault("AutoStartService") != "false",
                LastKnownVersion = dict.GetValueOrDefault("LastKnownVersion"),
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SQLite 读取失败: {ex.Message}");
            return null;
        }
    }

    private void WriteToDb(AppConfig config)
    {
        try
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            using var tx = conn.BeginTransaction();

            // UPSERT 每条配置
            Upsert(conn, "MainServerUrl", config.MainServerUrl);
            Upsert(conn, "StationApiFolder", config.StationApiFolder);
            Upsert(conn, "StationApiPort", config.StationApiPort.ToString());
            Upsert(conn, "ToolsRootPath", config.ToolsRootPath);
            Upsert(conn, "AutoStartService", config.AutoStartService ? "true" : "false");
            Upsert(conn, "LastKnownVersion", config.LastKnownVersion ?? "");

            tx.Commit();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SQLite 写入失败: {ex.Message}");
        }
    }

    private static void Upsert(SqliteConnection conn, string key, string? value)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppConfig (Key, Value) VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = @value;
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value ?? "");
        cmd.ExecuteNonQuery();
    }
}
