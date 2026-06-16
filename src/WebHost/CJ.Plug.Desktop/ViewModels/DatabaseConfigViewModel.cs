using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CJ.Plug.Desktop.ViewModels;

public partial class DatabaseConfigViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // ── ApiServer 配置 ──

    [ObservableProperty]
    private string _apiDbType = "SQLite";

    [ObservableProperty]
    private string _apiConnectionString = "Data Source=main.db;Cache=Shared;";

    [ObservableProperty]
    private string _apiServer = "";

    [ObservableProperty]
    private string _apiPort = "";

    [ObservableProperty]
    private string _apiDatabase = "";

    [ObservableProperty]
    private string _apiUsername = "";

    [ObservableProperty]
    private string _apiPassword = "";

    [ObservableProperty]
    private string _apiSaveMessage = "";

    // ── ApiServer 可见性计算属性 ──
    public bool IsApiSqlite => ApiDbType == "SQLite";
    public bool IsApiNotSqlite => ApiDbType != "SQLite";

    // ── ElsaApiServer 配置 ──

    [ObservableProperty]
    private string _elsaDbType = "SQLite";

    [ObservableProperty]
    private string _elsaConnectionString = "Data Source=../../main-elsa.db;Cache=Shared;";

    [ObservableProperty]
    private string _elsaServer = "";

    [ObservableProperty]
    private string _elsaPort = "";

    [ObservableProperty]
    private string _elsaDatabase = "";

    [ObservableProperty]
    private string _elsaUsername = "";

    [ObservableProperty]
    private string _elsaPassword = "";

    [ObservableProperty]
    private string _elsaSaveMessage = "";

    // ── ElsaApiServer 可见性计算属性 ──
    public bool IsElsaSqlite => ElsaDbType == "SQLite";
    public bool IsElsaNotSqlite => ElsaDbType != "SQLite";

    public DatabaseConfigViewModel()
    {
        LoadConfig();
    }

    /// <summary>
    /// 向上查找项目根目录（同时存在 src 和 02.Publish 子目录）。
    /// 与 AppHostLauncher.FindProjectRoot 逻辑一致。
    /// </summary>
    private static string FindProjectRoot()
    {
        var dir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) &&
                Directory.Exists(Path.Combine(dir, "02.Publish")))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
    }

    /// <summary>
    /// 获取 ApiServer 的 appsettings.json 路径。
    /// 优先 02.Publish 构建输出目录（Debug/Release 自动适配），
    /// 构建输出不存在时回退到 src 源码目录。
    /// </summary>
    private static string GetApiServerConfigPath()
    {
        var projectRoot = FindProjectRoot();

        var debugPath = Path.Combine(projectRoot, "02.Publish", "CJ.Plug.ApiServer", "Debug", "net10.0", "appsettings.json");
        if (File.Exists(debugPath)) return debugPath;

        var releasePath = Path.Combine(projectRoot, "02.Publish", "CJ.Plug.ApiServer", "Release", "net10.0", "appsettings.json");
        if (File.Exists(releasePath)) return releasePath;

        // 回退到源码目录
        return Path.Combine(projectRoot, "src", "PlugApiServer", "CJ.Plug.ApiServer", "appsettings.json");
    }

    /// <summary>
    /// 获取 ElsaApiServer 的 appsettings.json 路径。
    /// 优先 02.Publish 构建输出目录（Debug/Release 自动适配），
    /// 构建输出不存在时回退到 src 源码目录。
    /// </summary>
    private static string GetElsaApiServerConfigPath()
    {
        var projectRoot = FindProjectRoot();

        var debugPath = Path.Combine(projectRoot, "02.Publish", "CJ.Plug.ElsaApiServer", "Debug", "net10.0", "appsettings.json");
        if (File.Exists(debugPath)) return debugPath;

        var releasePath = Path.Combine(projectRoot, "02.Publish", "CJ.Plug.ElsaApiServer", "Release", "net10.0", "appsettings.json");
        if (File.Exists(releasePath)) return releasePath;

        // 回退到源码目录
        return Path.Combine(projectRoot, "src", "PlugApiServer", "CJ.Plug.ElsaApiServer", "appsettings.json");
    }

    /// <summary>
    /// 获取 ApiServer 源码目录的 appsettings.json 路径（固定返回 src 下路径）。
    /// 用于与 02.Publish 双写时确定源码目标路径。
    /// </summary>
    private static string GetApiServerSourceConfigPath()
    {
        var projectRoot = FindProjectRoot();
        return Path.Combine(projectRoot, "src", "PlugApiServer", "CJ.Plug.ApiServer", "appsettings.json");
    }

    /// <summary>
    /// 获取 ElsaApiServer 源码目录的 appsettings.json 路径（固定返回 src 下路径）。
    /// 用于与 02.Publish 双写时确定源码目标路径。
    /// </summary>
    private static string GetElsaApiServerSourceConfigPath()
    {
        var projectRoot = FindProjectRoot();
        return Path.Combine(projectRoot, "src", "PlugApiServer", "CJ.Plug.ElsaApiServer", "appsettings.json");
    }

    private void LoadConfig()
    {
        try
        {
            var apiPath = GetApiServerConfigPath();
            if (File.Exists(apiPath))
            {
                var json = File.ReadAllText(apiPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("DatabaseConfig", out var dbConfig))
                {
                    ApiDbType = GetString(dbConfig, "DbType", "SQLite");
                    ApiConnectionString = GetString(dbConfig, "ConnectionString", "Data Source=main.db;Cache=Shared;");
                    ApiServer = GetString(dbConfig, "Server", "");
                    ApiPort = GetString(dbConfig, "Port", "");
                    ApiDatabase = GetString(dbConfig, "Database", "");
                    ApiUsername = GetString(dbConfig, "Username", "");
                    ApiPassword = GetString(dbConfig, "Password", "");
                }
            }
        }
        catch
        {
            // 读取失败使用默认值
        }

        try
        {
            var elsaPath = GetElsaApiServerConfigPath();
            if (File.Exists(elsaPath))
            {
                var json = File.ReadAllText(elsaPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("DatabaseConfig", out var dbConfig))
                {
                    ElsaDbType = GetString(dbConfig, "DbType", "SQLite");
                    ElsaConnectionString = GetString(dbConfig, "ConnectionString", "Data Source=../../main-elsa.db;Cache=Shared;");
                    ElsaServer = GetString(dbConfig, "Server", "");
                    ElsaPort = GetString(dbConfig, "Port", "");
                    ElsaDatabase = GetString(dbConfig, "Database", "");
                    ElsaUsername = GetString(dbConfig, "Username", "");
                    ElsaPassword = GetString(dbConfig, "Password", "");
                }
            }
        }
        catch
        {
            // 读取失败使用默认值
        }
    }

    [RelayCommand]
    private void SaveApiConfig()
    {
        try
        {
            var connStr = BuildConnectionString(ApiDbType, ApiServer, ApiPort, ApiDatabase, ApiUsername, ApiPassword, ApiConnectionString);
            var path = GetApiServerConfigPath();
            if (!File.Exists(path))
            {
                ApiSaveMessage = "配置文件不存在: " + path;
                return;
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("DatabaseConfig"))
                    continue;
                prop.WriteTo(writer);
            }

            // 写入 DatabaseConfig
            writer.WriteStartObject("DatabaseConfig");
            writer.WriteString("DbType", ApiDbType);
            writer.WriteString("ConnectionString", connStr);
            writer.WriteString("Server", ApiServer ?? "");
            writer.WriteString("Port", ApiPort ?? "");
            writer.WriteString("Database", ApiDatabase ?? "");
            writer.WriteString("Username", ApiUsername ?? "");
            writer.WriteString("Password", ApiPassword ?? "");
            writer.WriteEndObject();

            // 确保 ConnectionStrings 存在并更新 PlugDb
            if (!root.TryGetProperty("ConnectionStrings", out _))
            {
                writer.WriteStartObject("ConnectionStrings");
                writer.WriteString("PlugDb", connStr);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            writer.Flush();
            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            // 如果已有 ConnectionStrings，还需要更新 PlugDb
            if (root.TryGetProperty("ConnectionStrings", out var connStrElem))
            {
                // 重新构建，在 ConnectionStrings 中添加 PlugDb
                using var stream2 = new MemoryStream();
                using var writer2 = new Utf8JsonWriter(stream2, new JsonWriterOptions { Indented = true });
                writer2.WriteStartObject();

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("DatabaseConfig"))
                        continue;

                    if (prop.NameEquals("ConnectionStrings"))
                    {
                        writer2.WriteStartObject("ConnectionStrings");
                        foreach (var cs in prop.Value.EnumerateObject())
                        {
                            if (!cs.NameEquals("PlugDb"))
                                cs.WriteTo(writer2);
                        }
                        writer2.WriteString("PlugDb", connStr);
                        writer2.WriteEndObject();
                    }
                    else
                    {
                        prop.WriteTo(writer2);
                    }
                }

                // 写入 DatabaseConfig
                writer2.WriteStartObject("DatabaseConfig");
                writer2.WriteString("DbType", ApiDbType);
                writer2.WriteString("ConnectionString", connStr);
                writer2.WriteString("Server", ApiServer ?? "");
                writer2.WriteString("Port", ApiPort ?? "");
                writer2.WriteString("Database", ApiDatabase ?? "");
                writer2.WriteString("Username", ApiUsername ?? "");
                writer2.WriteString("Password", ApiPassword ?? "");
                writer2.WriteEndObject();

                writer2.WriteEndObject();
                writer2.Flush();
                updatedJson = System.Text.Encoding.UTF8.GetString(stream2.ToArray());
            }

            File.WriteAllText(path, updatedJson);
            // 同时写入源码目录，确保 VS 调试时 builder.Configuration 能读到
            var apiSrcPath = GetApiServerSourceConfigPath();
            try
            {
                var srcDir = System.IO.Path.GetDirectoryName(apiSrcPath);
                if (!string.IsNullOrEmpty(srcDir) && !Directory.Exists(srcDir))
                    Directory.CreateDirectory(srcDir);
                File.WriteAllText(apiSrcPath, updatedJson);
            }
            catch { /* 源码目录写入失败不影响发布目录 */ }
            ApiSaveMessage = "配置已保存，重启服务后生效";
        }
        catch (Exception ex)
        {
            ApiSaveMessage = "保存失败: " + ex.Message;
        }
        finally
        {
            // 3 秒后清除提示
            _ = ClearApiMessageAsync();
        }
    }

    [RelayCommand]
    private void SaveElsaConfig()
    {
        try
        {
            var connStr = BuildConnectionString(ElsaDbType, ElsaServer, ElsaPort, ElsaDatabase, ElsaUsername, ElsaPassword, ElsaConnectionString);
            var path = GetElsaApiServerConfigPath();
            if (!File.Exists(path))
            {
                ElsaSaveMessage = "配置文件不存在: " + path;
                return;
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("DatabaseConfig"))
                    continue;
                prop.WriteTo(writer);
            }

            // 写入 DatabaseConfig
            writer.WriteStartObject("DatabaseConfig");
            writer.WriteString("DbType", ElsaDbType);
            writer.WriteString("ConnectionString", connStr);
            writer.WriteString("Server", ElsaServer ?? "");
            writer.WriteString("Port", ElsaPort ?? "");
            writer.WriteString("Database", ElsaDatabase ?? "");
            writer.WriteString("Username", ElsaUsername ?? "");
            writer.WriteString("Password", ElsaPassword ?? "");
            writer.WriteEndObject();

            // 确保 ConnectionStrings 存在并更新 ElsaDb
            if (!root.TryGetProperty("ConnectionStrings", out _))
            {
                writer.WriteStartObject("ConnectionStrings");
                writer.WriteString("ElsaDb", connStr);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            writer.Flush();
            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            if (root.TryGetProperty("ConnectionStrings", out _))
            {
                using var stream2 = new MemoryStream();
                using var writer2 = new Utf8JsonWriter(stream2, new JsonWriterOptions { Indented = true });
                writer2.WriteStartObject();

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("DatabaseConfig"))
                        continue;

                    if (prop.NameEquals("ConnectionStrings"))
                    {
                        writer2.WriteStartObject("ConnectionStrings");
                        foreach (var cs in prop.Value.EnumerateObject())
                        {
                            if (!cs.NameEquals("ElsaDb"))
                                cs.WriteTo(writer2);
                        }
                        writer2.WriteString("ElsaDb", connStr);
                        writer2.WriteEndObject();
                    }
                    else
                    {
                        prop.WriteTo(writer2);
                    }
                }

                writer2.WriteStartObject("DatabaseConfig");
                writer2.WriteString("DbType", ElsaDbType);
                writer2.WriteString("ConnectionString", connStr);
                writer2.WriteString("Server", ElsaServer ?? "");
                writer2.WriteString("Port", ElsaPort ?? "");
                writer2.WriteString("Database", ElsaDatabase ?? "");
                writer2.WriteString("Username", ElsaUsername ?? "");
                writer2.WriteString("Password", ElsaPassword ?? "");
                writer2.WriteEndObject();

                writer2.WriteEndObject();
                writer2.Flush();
                updatedJson = System.Text.Encoding.UTF8.GetString(stream2.ToArray());
            }

            File.WriteAllText(path, updatedJson);
            // 同时写入源码目录，确保 VS 调试时 builder.Configuration 能读到
            var elsaSrcPath = GetElsaApiServerSourceConfigPath();
            try
            {
                var srcDir = System.IO.Path.GetDirectoryName(elsaSrcPath);
                if (!string.IsNullOrEmpty(srcDir) && !Directory.Exists(srcDir))
                    Directory.CreateDirectory(srcDir);
                File.WriteAllText(elsaSrcPath, updatedJson);
            }
            catch { /* 源码目录写入失败不影响发布目录 */ }
            ElsaSaveMessage = "配置已保存，重启服务后生效";
        }
        catch (Exception ex)
        {
            ElsaSaveMessage = "保存失败: " + ex.Message;
        }
        finally
        {
            _ = ClearElsaMessageAsync();
        }
    }

    [RelayCommand]
    private void RestoreApiDefaults()
    {
        ApiDbType = "SQLite";
        ApiConnectionString = "Data Source=main.db;Cache=Shared;";
        ApiServer = "";
        ApiPort = "";
        ApiDatabase = "";
        ApiUsername = "";
        ApiPassword = "";
        ApiSaveMessage = "已恢复默认配置，请点击保存";
    }

    [RelayCommand]
    private void RestoreElsaDefaults()
    {
        ElsaDbType = "SQLite";
        ElsaConnectionString = "Data Source=../../main-elsa.db;Cache=Shared;";
        ElsaServer = "";
        ElsaPort = "";
        ElsaDatabase = "";
        ElsaUsername = "";
        ElsaPassword = "";
        ElsaSaveMessage = "已恢复默认配置，请点击保存";
    }

    [RelayCommand]
    private void SaveDatabaseConfig()
    {
        SaveApiConfig();
        SaveElsaConfig();
    }

    private static string GetString(JsonElement element, string propertyName, string defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? defaultValue;
        return defaultValue;
    }

    private async Task ClearApiMessageAsync()
    {
        await Task.Delay(5000);
        ApiSaveMessage = "";
    }

    private async Task ClearElsaMessageAsync()
    {
        await Task.Delay(5000);
        ElsaSaveMessage = "";
    }

    partial void OnApiDbTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsApiSqlite));
        OnPropertyChanged(nameof(IsApiNotSqlite));
        RegenerateApiConnectionString();
    }

    partial void OnApiServerChanged(string value) => RegenerateApiConnectionString();
    partial void OnApiPortChanged(string value) => RegenerateApiConnectionString();
    partial void OnApiDatabaseChanged(string value) => RegenerateApiConnectionString();
    partial void OnApiUsernameChanged(string value) => RegenerateApiConnectionString();
    partial void OnApiPasswordChanged(string value) => RegenerateApiConnectionString();

    partial void OnElsaDbTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsElsaSqlite));
        OnPropertyChanged(nameof(IsElsaNotSqlite));
        RegenerateElsaConnectionString();
    }

    partial void OnElsaServerChanged(string value) => RegenerateElsaConnectionString();
    partial void OnElsaPortChanged(string value) => RegenerateElsaConnectionString();
    partial void OnElsaDatabaseChanged(string value) => RegenerateElsaConnectionString();
    partial void OnElsaUsernameChanged(string value) => RegenerateElsaConnectionString();
    partial void OnElsaPasswordChanged(string value) => RegenerateElsaConnectionString();

    private void RegenerateApiConnectionString()
    {
        if (ApiDbType != "SQLite")
        {
            ApiConnectionString = BuildConnectionString(ApiDbType, ApiServer, ApiPort, ApiDatabase, ApiUsername, ApiPassword, ApiConnectionString);
        }
        else
        {
            ApiConnectionString = "Data Source=main.db;Cache=Shared;";
        }
    }

    private void RegenerateElsaConnectionString()
    {
        if (ElsaDbType != "SQLite")
        {
            ElsaConnectionString = BuildConnectionString(ElsaDbType, ElsaServer, ElsaPort, ElsaDatabase, ElsaUsername, ElsaPassword, ElsaConnectionString);
        }
        else
        {
            ElsaConnectionString = "Data Source=../../main-elsa.db;Cache=Shared;";
        }
    }

    private static string BuildConnectionString(string dbType, string server, string port, string database, string username, string password, string sqliteFallback)
    {
        switch (dbType)
        {
            case "PostgreSQL":
                return $"Host={server};Port={port};Database={database};Username={username};Password={password};Multiplexing=false;Pooling=true;MaxPoolSize=20;Connection Idle Lifetime=300;Connection Pruning Interval=10;";
            case "SqlServer":
                return $"Server={server},{port};Database={database};User Id={username};Password={password};TrustServerCertificate=true;";
            default: // SQLite
                return sqliteFallback;
        }
    }
}
