using CJ.Plug_Aspire.StationApiService.Models;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace CJ.Plug_Aspire.StationApiService.Services;

/// <summary>
/// 图站任务本地 SQLite 存储
/// 数据库位置: 与 StationApiServer 同目录的 station_tasks.db
/// </summary>
public class StationTaskStore
{
    private readonly string _dbPath;

    public StationTaskStore(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(AppContext.BaseDirectory, "station_tasks.db");
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    /// <summary>
    /// 初始化数据库表
    /// </summary>
    public void Init()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS StationTasks (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                CorrelationId TEXT,
                PlugTypeKey   TEXT,
                ToolName      TEXT,
                Command       TEXT,
                ExecuteMode   TEXT,
                Status        TEXT NOT NULL DEFAULT 'pending',
                SubStatus     TEXT,
                Result        TEXT,
                CreatedAt     TEXT NOT NULL,
                CompletedAt   TEXT,
                ProcessId     INTEGER,
                JobCorrelationId TEXT,
                PlugDefinitionId TEXT,
                PDZId         TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_tasks_status ON StationTasks(Status);
            CREATE INDEX IF NOT EXISTS idx_tasks_correlation ON StationTasks(CorrelationId);
            """;
        cmd.ExecuteNonQuery();

        // 兼容旧表：添加 ProcessId 列（如果不存在）
        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE StationTasks ADD COLUMN ProcessId INTEGER;";
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
        {
        }

        // 兼容旧表：添加 JobCorrelationId 列
        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE StationTasks ADD COLUMN JobCorrelationId TEXT;";
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
        {
        }

        // 兼容旧表：添加 PlugDefinitionId 列
        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE StationTasks ADD COLUMN PlugDefinitionId TEXT;";
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
        {
        }

        // 兼容旧表：添加 PDZId 列
        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE StationTasks ADD COLUMN PDZId TEXT;";
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
        {
        }
    }

    /// <summary>
    /// 新增任务
    /// </summary>
    public int Insert(StationTask task)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO StationTasks
                (CorrelationId, PlugTypeKey, ToolName, Command, ExecuteMode, Status, CreatedAt, ProcessId, JobCorrelationId, PlugDefinitionId, PDZId)
            VALUES
                (@cid, @type, @name, @cmd, @mode, @status, @created, @pid, @jobCid, @plugDefId, @pdzId);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@cid", (object?)task.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@type", (object?)task.PlugTypeKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", (object?)task.ToolName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cmd", (object?)task.Command ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@mode", (object?)task.ExecuteMode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", task.Status);
        cmd.Parameters.AddWithValue("@created", task.CreatedAt);
        cmd.Parameters.AddWithValue("@pid", (object?)task.ProcessId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@jobCid", (object?)task.JobCorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@plugDefId", (object?)task.PlugDefinitionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pdzId", (object?)task.PDZId ?? DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    public void UpdateStatus(int id, string status, string? subStatus, string? result)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE StationTasks 
            SET Status = @status, SubStatus = @sub, Result = @result,
                CompletedAt = CASE WHEN @status IN ('completed','failed') 
                              THEN @now ELSE CompletedAt END
            WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@sub", (object?)subStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@result", (object?)result ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 通过 CorrelationId 更新任务状态
    /// </summary>
    public void UpdateStatusByCorrelationId(string correlationId, string status, string? subStatus, string? result)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE StationTasks 
            SET Status = @status, SubStatus = @sub, Result = @result,
                CompletedAt = CASE WHEN @status IN ('completed','failed') 
                              THEN @now ELSE CompletedAt END
            WHERE CorrelationId = @cid;
            """;
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@sub", (object?)subStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@result", (object?)result ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@cid", correlationId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取所有任务（按时间倒序）
    /// </summary>
    public List<StationTask> GetAll(int limit = 100)
    {
        var tasks = new List<StationTask>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM StationTasks ORDER BY Id DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
    }

    /// <summary>
    /// 检查状态为 running 的任务，若对应进程已退出则自动标记为完成
    /// </summary>
    public void FixStaleRunningTasks()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, ProcessId FROM StationTasks WHERE Status = 'running' AND ProcessId IS NOT NULL";
        var staleIds = new List<int>();

        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var pid = reader.GetInt32(1);
                try
                {
                    var process = Process.GetProcessById(pid);
                    if (process.HasExited)
                    {
                        staleIds.Add(id);
                        process.Dispose();
                    }
                }
                catch (ArgumentException)
                {
                    staleIds.Add(id);
                }
                catch (Exception)
                {
                    // 无法访问进程（权限等），保留 running 状态
                }
            }
        }

        foreach (var id in staleIds)
        {
            UpdateStatus(id, "completed", "进程已退出", null);
        }
    }

    /// <summary>
    /// 按 ID 获取任务
    /// </summary>
    public StationTask? GetById(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM StationTasks WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    /// <summary>
    /// 清理已完成任务（保留最近 N 条）
    /// </summary>
    public int Cleanup(int keepCount = 200)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM StationTasks 
            WHERE Status IN ('completed','failed') 
            AND Id NOT IN (SELECT Id FROM StationTasks ORDER BY Id DESC LIMIT @keep);
            """;
        cmd.Parameters.AddWithValue("@keep", keepCount);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 更新任务的进程 ID
    /// </summary>
    public void UpdateProcessId(int id, int processId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE StationTasks SET ProcessId = @pid WHERE Id = @id";
        cmd.Parameters.AddWithValue("@pid", processId);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取任务的进程 ID
    /// </summary>
    public int? GetProcessIdById(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ProcessId FROM StationTasks WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var result = cmd.ExecuteScalar();
        return result is DBNull || result == null ? null : Convert.ToInt32(result);
    }

    private static StationTask ReadTask(SqliteDataReader reader)
    {
        return new StationTask
        {
            Id = reader.GetInt32(0),
            CorrelationId = reader.IsDBNull(1) ? null : reader.GetString(1),
            PlugTypeKey = reader.IsDBNull(2) ? null : reader.GetString(2),
            ToolName = reader.IsDBNull(3) ? null : reader.GetString(3),
            Command = reader.IsDBNull(4) ? null : reader.GetString(4),
            ExecuteMode = reader.IsDBNull(5) ? null : reader.GetString(5),
            Status = reader.GetString(6),
            SubStatus = reader.IsDBNull(7) ? null : reader.GetString(7),
            Result = reader.IsDBNull(8) ? null : reader.GetString(8),
            CreatedAt = reader.GetString(9),
            CompletedAt = reader.IsDBNull(10) ? null : reader.GetString(10),
            ProcessId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            JobCorrelationId = reader.IsDBNull(12) ? null : reader.GetString(12),
            PlugDefinitionId = reader.IsDBNull(13) ? null : reader.GetString(13),
            PDZId = reader.IsDBNull(14) ? null : reader.GetString(14),
        };
    }
}
