using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CJ.Plug.Models.DbContexts
{
    public class DbConnectionString
    {
        private static string? _connectionString;
        private static string? _dbType;

        public static string ConnectionString
        {
            get
            {
                if (_connectionString != null)
                    return _connectionString;

                _connectionString = ReadFromConfig();
                return _connectionString;
            }
        }

        public static string DbType
        {
            get
            {
                if (_dbType != null)
                    return _dbType;

                _dbType = ReadDbTypeFromConfig();
                return _dbType;
            }
        }

        private static string? FindConfigPath()
        {
            var dir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir, "src")) &&
                    Directory.Exists(Path.Combine(dir, "02.Publish")))
                {
                    // 优先 src 源码目录（VS 调试时 builder.Configuration 从此读取）
                    var srcPath = Path.Combine(dir, "src", "PlugApiServer", "CJ.Plug.ApiServer", "appsettings.json");
                    if (File.Exists(srcPath)) return srcPath;

                    // 回退到 02.Publish 构建输出目录（Debug/Release 自动适配）
                    var debugPath = Path.Combine(dir, "02.Publish", "CJ.Plug.ApiServer", "Debug", "net10.0", "appsettings.json");
                    if (File.Exists(debugPath)) return debugPath;

                    var releasePath = Path.Combine(dir, "02.Publish", "CJ.Plug.ApiServer", "Release", "net10.0", "appsettings.json");
                    if (File.Exists(releasePath)) return releasePath;

                    return null;
                }
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return null;
        }

        private static string ReadFromConfig()
        {
            try
            {
                var configPath = FindConfigPath();
                if (configPath == null || !File.Exists(configPath))
                    return "Data Source=../../main.db;Cache=Shared;";

                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStr) &&
                    connStr.TryGetProperty("PlugDb", out var plugDb))
                {
                    var val = plugDb.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            catch
            {
                // fall through to default
            }

            return "Data Source=../../main.db;Cache=Shared;";
        }

        private static string ReadDbTypeFromConfig()
        {
            try
            {
                var configPath = FindConfigPath();
                if (configPath == null || !File.Exists(configPath))
                    return "SQLite";

                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("DatabaseConfig", out var dbConfig) &&
                    dbConfig.TryGetProperty("DbType", out var dbTypeElem))
                {
                    var val = dbTypeElem.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            catch
            {
                // fall through to default
            }

            return "SQLite";
        }
    }
}
