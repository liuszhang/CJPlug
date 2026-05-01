namespace StationSettingUI.Models;

/// <summary>
/// 应用配置数据模型
/// </summary>
public class AppConfig
{
    /// <summary>平台主服务器地址，如 http://192.168.1.100:8080</summary>
    public string MainServerUrl { get; set; } = "http://127.0.0.1:8080";

    /// <summary>本地StationApiServer的HTTP端口</summary>
    public int StationApiPort { get; set; } = 7660;

    /// <summary>StationApiServer 可执行程序所在文件夹（留空则自动查找）</summary>
    public string StationApiFolder { get; set; } = "";

    /// <summary>工具安装根目录</summary>
    public string ToolsRootPath { get; set; } = @"C:\Program Files\CJTools";

    /// <summary>是否开机自动启动图站服务</summary>
    public bool AutoStartService { get; set; } = true;

    /// <summary>上次检查更新时记录的版本号</summary>
    public string? LastKnownVersion { get; set; }

    /// <summary>当前应用版本</summary>
    public static string AppVersion => "0.2.0";

    /// <summary>本地StationApiServer地址</summary>
    public string StationApiUrl => $"http://localhost:{StationApiPort}";
}
