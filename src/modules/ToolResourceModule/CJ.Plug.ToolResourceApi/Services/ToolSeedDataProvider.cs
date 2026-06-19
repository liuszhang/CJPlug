using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Station;
using Serilog;

namespace CJ.Plug.ToolResourceApi.Services;

/// <summary>
/// 工具管理种子数据提供者 - 在应用启动时写入默认工具列表
/// </summary>
public class ToolSeedDataProvider : ISeedDataProvider
{
    public string Name => "工具管理模块种子数据";
    public int Order => 50;

    /// <summary>
    /// 默认工具列表（与 ToolsManage.razor 中的 DefaultTools 保持一致）
    /// </summary>
    public static readonly List<Tool> DefaultTools =
    [
        new()
        {
            ToolName = "获取NX模型参数",
            ToolVersion = "1.0",
            ToolCompany = "CJ",
            ToolPath = @"Tools\0System\NXGetParameters.exe",
            CommandParameter = "[ToolPath] [ModelFilePath]",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = "获取NX模型参数"
        },
        new()
        {
            ToolName = "设置NX模型参数",
            ToolVersion = "1.0",
            ToolCompany = "CJ",
            ToolPath = @"Tools\0System\NXUpdateParameters.exe",
            CommandParameter = "[ToolPath] [ModelFilePath] [NewParameterString]",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = "设置NX模型参数"
        },
        new()
        {
            ToolName = "NX模型转STL",
            ToolVersion = "1.0",
            ToolCompany = "CJ",
            ToolPath = @"Tools\0System\NXToStl.exe",
            CommandParameter = "[ToolPath] [ModelFilePath] [StlFilePath]",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = "NX模型转STL"
        },
        new()
        {
            ToolName = "NX",
            ToolVersion = "12.0",
            ToolCompany = "Siemens",
            SkipDownloadToStation = true,
            ToolPath = @"Tools\0System\ugraf.exe",
            ToolDescription = "NX 软件（前身为 Unigraphics，简称 UG）是由西门子数字工业软件（Siemens Digital Industries Software）开发和维护的，集 CAD、CAM、CAE 于一体的高效紧密集成软件。"
        },
        new()
        {
            ToolName = "CMD",
            ToolVersion = "1.0",
            ToolCompany = "Microsoft",
            IsSystemInitTool = true,
            SkipDownloadToStation = true,
            ToolPath = @"C:\Windows\System32\cmd.exe",
            CommandParameter = "[CMDCommand]",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = "系统CMD命令"
        },
        new()
        {
            ToolName = "Nastran",
            ToolVersion = "2019",
            ToolPath = @"Tools\0System\nastran.exe",
            SkipDownloadToStation = true,
            ToolDescription = "Nastran是进行网格计算的求解器"
        },
        new()
        {
            ToolName = "Patran",
            ToolVersion = "20122",
            ToolCompany = "MSC Software",
            SkipDownloadToStation = true,
            ToolPath = @"C:\MSC.Software\Patran\20122\bin\patran.exe",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = "Patran是MSC Software的有限元前后处理器，用于网格划分、载荷施加和结果可视化"
        },
        new()
        {
            ToolName = "Word",
            ToolVersion = "2016",
            ToolCompany = "Microsoft",
            ToolDescription = "Microsoft Word 是一款文字处理软件，是微软公司的办公软件套装 Microsoft Office 中的一个组成部分。",
            SkipDownloadToStation = true,
        },
        new()
        {
            ToolName = "Excel",
            ToolVersion = "2016",
            ToolCompany = "Microsoft",
            ToolDescription = "Microsoft Excel 是一款电子表格软件，是微软公司的办公软件套装 Microsoft Office 中的一个组成部分。",
            SkipDownloadToStation = true,
        },
        new()
        {
            ToolName = "PowerPoint",
            ToolVersion = "2016",
            ToolCompany = "Microsoft",
            SkipDownloadToStation = true,
            ToolDescription = "Microsoft PowerPoint 是一款幻灯片演示软件，是微软公司的办公软件套装 Microsoft Office 中的一个组成部分。"
        },
        new()
        {
            ToolName = "VS",
            ToolVersion = "2022",
            ToolCompany = "Microsoft",
            SkipDownloadToStation = true,
            ToolDescription = "Visual Studio 是一种由微软公司开发的集成开发环境（IDE），用于开发计算机程序、网站、网页应用程序、网页服务和移动应用程序。"
        },
        new()
        {
            ToolName = ".NET Framework 桥接程序",
            ToolVersion = "1.0",
            IsSystemInitTool = true,
            ToolCompany = "CJ",
            ToolPath = @"Tools\0System\DotnetCoreBridgeToDotnetFramework.exe",
            CommandParameter = "[ToolPath] --codefile [CodeFilePath] --dlls [DllPaths]",
            ToolType = ToolTypeEnum.桌面类_商业.ToString(),
            ToolDescription = ".NET Framework 桥接程序，用于在 .NET Framework 4.8 环境中执行 C# 代码，支持 DLL 引用和环境变量。\n参数说明：--codefile 代码文件路径，--dlls 分号分隔的 DLL 文件路径列表。\n系统内置工具，由 CSharpPlug 通过工具调度系统自动调用。"
        }
    ];

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        // IToolManageService 是 Scoped 的，需要创建 scope
        using var scope = serviceProvider.CreateScope();
        var toolService = scope.ServiceProvider.GetRequiredService<IToolManageService>();

        // 获取已有的所有工具
        var existingTools = (await toolService.GetAllToolsAsync(cancellationToken))?.ToList() ?? [];
        var existingKeys = existingTools
            .Select(t => $"{t.ToolName}|{t.ToolVersion}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var tool in DefaultTools)
        {
            var key = $"{tool.ToolName}|{tool.ToolVersion}";
            if (existingKeys.Contains(key))
                continue;

            await toolService.CreateToolAsync(tool, cancellationToken);
            added++;
            Log.Information("[SeedData] 创建默认工具：{ToolName} v{Version}", tool.ToolName, tool.ToolVersion);
            Console.WriteLine($"[SeedData] 创建默认工具：{tool.ToolName} v{tool.ToolVersion}");
        }

        if (added > 0)
        {
            Log.Information("[SeedData] 共新增 {Count} 个默认工具", added);
            Console.WriteLine($"[SeedData] 共新增 {added} 个默认工具");
        }
        else
        {
            Console.WriteLine("[SeedData] 默认工具已存在，无需新增");
        }
    }
}
