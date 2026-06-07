using System.Collections.Concurrent;

namespace CJ.Plug.ApiServer.Services;

/// <summary>
/// 打包进度跟踪器
/// </summary>
public class PackageProgressTracker
{
    private readonly ConcurrentDictionary<string, PackageProgress> _progressMap = new();
    
    /// <summary>
    /// 创建新的打包任务
    /// </summary>
    public string CreateTask()
    {
        var taskId = Guid.NewGuid().ToString("N");
        var progress = new PackageProgress
        {
            TaskId = taskId,
            Status = PackageStatus.Pending,
            Progress = 0,
            StartTime = DateTime.UtcNow
        };
        _progressMap[taskId] = progress;
        return taskId;
    }
    
    /// <summary>
    /// 获取任务进度
    /// </summary>
    public PackageProgress? GetProgress(string taskId)
    {
        return _progressMap.TryGetValue(taskId, out var progress) ? progress : null;
    }
    
    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(string taskId, int progress, string message)
    {
        if (_progressMap.TryGetValue(taskId, out var p))
        {
            p.Progress = progress;
            p.Message = message;
            p.Logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                Level = "Info"
            });
        }
    }
    
    /// <summary>
    /// 添加日志
    /// </summary>
    public void AddLog(string taskId, string message, string level = "Info")
    {
        if (_progressMap.TryGetValue(taskId, out var p))
        {
            p.Logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                Level = level
            });
        }
    }
    
    /// <summary>
    /// 标记任务完成
    /// </summary>
    public void CompleteTask(string taskId, byte[]? zipBytes = null)
    {
        if (_progressMap.TryGetValue(taskId, out var p))
        {
            p.Status = PackageStatus.Completed;
            p.Progress = 100;
            p.Message = "打包完成";
            p.EndTime = DateTime.UtcNow;
            p.ZipBytes = zipBytes;
            p.Logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = "打包完成",
                Level = "Info"
            });
        }
    }
    
    /// <summary>
    /// 标记任务失败
    /// </summary>
    public void FailTask(string taskId, string error)
    {
        if (_progressMap.TryGetValue(taskId, out var p))
        {
            p.Status = PackageStatus.Failed;
            p.Message = error;
            p.EndTime = DateTime.UtcNow;
            p.Logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = $"打包失败: {error}",
                Level = "Error"
            });
        }
    }
    
    /// <summary>
    /// 清理过期任务（超过1小时）
    /// </summary>
    public void CleanupExpiredTasks()
    {
        var expiredTasks = _progressMap
            .Where(x => x.Value.EndTime.HasValue && 
                       x.Value.EndTime.Value.AddHours(1) < DateTime.UtcNow)
            .Select(x => x.Key)
            .ToList();
        
        foreach (var taskId in expiredTasks)
        {
            _progressMap.TryRemove(taskId, out _);
        }
    }
}

/// <summary>
/// 打包进度
/// </summary>
public class PackageProgress
{
    public string TaskId { get; set; } = string.Empty;
    public PackageStatus Status { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public byte[]? ZipBytes { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Info";
}

/// <summary>
/// 打包状态
/// </summary>
public enum PackageStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}