using System.Text.RegularExpressions;

namespace CJ.Plug.McpServer.Tools;

/// <summary>
/// MCP 工具名清洗器 —— 将中文/特殊字符名转为 MCP 合法名称
/// MCP 工具名必须匹配 ^[A-Za-z0-9_.-]{1,128}$
/// </summary>
public static partial class ToolNameSanitizer
{
    [GeneratedRegex(@"[^A-Za-z0-9_.\-]")]
    private static partial Regex InvalidChars();

    [GeneratedRegex(@"[_.\-]{2,}")]
    private static partial Regex ConsecutiveSeparators();

    public static string Sanitize(string? name, string? sourcePlugId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"tool_{GetShortId(sourcePlugId)}";

        var result = InvalidChars().Replace(name, "_");
        result = ConsecutiveSeparators().Replace(result, "_");
        result = result.Trim('_', '.', '-');

        if (result.Length == 0)
            return $"tool_{GetShortId(sourcePlugId)}";

        if (result.Length > 128)
            result = result[..128];

        return result;
    }

    private static string GetShortId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "unknown";
        var clean = new string(id.Where(c => char.IsLetterOrDigit(c)).ToArray());
        return clean.Length >= 12 ? clean[..12] : clean;
    }

    /// <summary>
    /// 碰撞处理：如果 name 已存在，追加 _2, _3, ...
    /// </summary>
    public static string EnsureUnique(string baseName, HashSet<string> existing)
    {
        if (!existing.Contains(baseName))
            return baseName;

        for (int i = 2; i < 1000; i++)
        {
            var candidate = $"{baseName}_{i}";
            if (!existing.Contains(candidate))
                return candidate;
        }
        return $"{baseName}_{Guid.NewGuid():N}"[..128];
    }
}
