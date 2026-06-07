using System.Diagnostics;
using System.IO;
using System.Text;

namespace CJ.Plug.StationSetup.Services;

/// <summary>
/// 负责将预打包的组件复制到目标安装目录，配置初始设置，创建快捷方式。
/// 预打包的组件位于 CJ.Plug.StationSetup.exe 同级目录下：
///   StationAgent/  StationApiServer/  StationSettingUI/
/// </summary>
public class InstallService
{
    /// <summary>
    /// 安装包源目录（CJ.Plug.StationSetup.exe 所在目录）
    /// </summary>
    private static readonly string PackageDir = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// 执行完整安装流程
    /// </summary>
    public void Install(string installPath, string mainServerUrl, bool createShortcut, bool autoStart)
    {
        // 1. 检查预打包组件是否存在
        var agentSrc = Path.Combine(PackageDir, "StationAgent");
        var apiSrc = Path.Combine(PackageDir, "StationApiServer");
        var uiSrc = Path.Combine(PackageDir, "StationSettingUI");

        if (!Directory.Exists(agentSrc))
            throw new DirectoryNotFoundException($"找不到 StationAgent 组件: {agentSrc}");
        if (!Directory.Exists(apiSrc))
            throw new DirectoryNotFoundException($"找不到 StationApiServer 组件: {apiSrc}");
        if (!Directory.Exists(uiSrc))
            throw new DirectoryNotFoundException($"找不到 StationSettingUI 组件: {uiSrc}");

        // 2. 创建安装目录
        Directory.CreateDirectory(installPath);

        // 3. 复制到安装目录
        CopyDirectory(agentSrc, Path.Combine(installPath, "Agent"));
        CopyDirectory(apiSrc, Path.Combine(installPath, "ApiServer"));
        CopyDirectory(uiSrc, Path.Combine(installPath, "SettingUI"));

        // 4. 配置主服务器地址
        WriteMainServerUrl(installPath, mainServerUrl);

        // 5. 创建桌面快捷方式
        if (createShortcut)
            CreateDesktopShortcut(installPath);

        // 6. 创建卸载脚本
        CreateUninstallScript(installPath);

        // 7. 可选：自动启动 StationApiServer
        if (autoStart)
            StartStationApiServer(installPath);
    }

    /// <summary>
    /// 更新 StationApiServer 的 appsettings.json 中的主服务器地址
    /// </summary>
    private static void WriteMainServerUrl(string installPath, string mainServerUrl)
    {
        var appSettingsPath = Path.Combine(installPath, "ApiServer", "appsettings.json");
        if (!File.Exists(appSettingsPath)) return;

        var content = File.ReadAllText(appSettingsPath, Encoding.UTF8);
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"""Url""\s*:\s*""[^""]*""",
            $@"""Url"": ""{mainServerUrl}""");
        File.WriteAllText(appSettingsPath, content, Encoding.UTF8);
    }

    /// <summary>
    /// 创建桌面快捷方式（指向 StationSettingUI）
    /// </summary>
    private static void CreateDesktopShortcut(string installPath)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktopPath, "图站配置工具.lnk");
        var targetPath = Path.Combine(installPath, "SettingUI", "StationSettingUI.exe");

        if (!File.Exists(targetPath)) return;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            var shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            var shortcut = shellType.InvokeMember("CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod, null, shell,
                new object[] { shortcutPath });

            var linkType = shortcut.GetType();
            linkType.GetProperty("TargetPath")?.SetValue(shortcut, targetPath);
            linkType.GetProperty("WorkingDirectory")?.SetValue(shortcut, Path.GetDirectoryName(targetPath));
            linkType.GetProperty("Description")?.SetValue(shortcut, "图站配置工具");
            linkType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }
        catch
        {
            // 快捷方式创建失败不影响安装
        }
    }

    /// <summary>
    /// 创建卸载脚本
    /// </summary>
    private static void CreateUninstallScript(string installPath)
    {
        var batPath = Path.Combine(installPath, "卸载图站.bat");
        var content = $@"@echo off
chcp 65001 >nul
echo 正在停止 StationApiServer...
taskkill /f /im CJ.Plug.StationApiServer.exe 2>nul
echo 正在删除桌面快捷方式...
del /q ""%USERPROFILE%\Desktop\图站配置工具.lnk"" 2>nul
echo 正在删除安装目录...
cd /d ""%TEMP%""
rd /s /q ""{installPath}""
echo 卸载完成。
pause
";
        File.WriteAllText(batPath, content, new UTF8Encoding(false));
    }

    /// <summary>
    /// 启动 StationApiServer
    /// </summary>
    private static void StartStationApiServer(string installPath)
    {
        var exePath = Path.Combine(installPath, "ApiServer", "CJ.Plug.StationApiServer.exe");
        if (File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            });
        }
    }

    /// <summary>
    /// 递归复制目录
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");

        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}
