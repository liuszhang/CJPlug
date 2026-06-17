using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.ToolResourceApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO;

public class ToolManageService : IToolManageService
{
    private readonly MainDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ToolManageService(MainDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _dbContext.Database.EnsureCreated();
    }

    public async Task<IEnumerable<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<Tool>().ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
    public async Task<Tool?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<Tool>().FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }

    }
    public async Task<Tool?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
    {
        string decodedIp = Uri.UnescapeDataString(stationIp);
        //MyCon1sole.WriteLine($"反转义后的IP为：{decodedIp}");
        return await _dbContext.Set<Tool>()
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default)
    {
        try
        {
            // 自动生成工具路径（相对路径，基于 Tools 目录结构）
            if (string.IsNullOrEmpty(newTool.ToolPath) || newTool.ToolPath == newTool.ToolName)
            {
                if (newTool.IsSystemInitTool)
                {
                    newTool.ToolPath = Path.Combine("Tools", "0System", newTool.ToolName!);
                }
                else
                {
                    var userName = GetCurrentUserName();
                    newTool.ToolPath = Path.Combine("Tools", userName, newTool.ToolName!);
                }
            }

            // 自动生成工具基础路径（工具包根目录）
            if (string.IsNullOrEmpty(newTool.ToolBasePath))
            {
                if (newTool.IsSystemInitTool)
                {
                    newTool.ToolBasePath = Path.Combine("Tools", "0System", newTool.ToolName!);
                }
                else
                {
                    var userName = GetCurrentUserName();
                    newTool.ToolBasePath = Path.Combine("Tools", userName, newTool.ToolName!);
                }
            }

            _dbContext.Set<Tool>().Add(newTool);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newTool;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return null;
    }
    public async Task<bool> DeleteToolAsync(int ToolId, CancellationToken cancellationToken = default)
    {
        var tool = await _dbContext.Set<Tool>().FindAsync(ToolId);
        if (tool == null)
        {
            return false;
        }

        _dbContext.Set<Tool>().Remove(tool);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default)
    {
        //await _context.Database.EnsureCreatedAsync(cancellationToken);
        var existingTool = await _dbContext.Set<Tool>().FindAsync(new object[] { updatedTool.Id }, cancellationToken);
        if (existingTool == null)
        {
            await CreateToolAsync(updatedTool);
            return updatedTool;
        }
        // 更新实体的属性
        existingTool.ToolName = updatedTool.ToolName;
        existingTool.ToolVersion = updatedTool.ToolVersion;
        existingTool.ToolType = updatedTool.ToolType;
        existingTool.ToolPath = updatedTool.ToolPath;
        existingTool.ToolBasePath = updatedTool.ToolBasePath;
        existingTool.ToolCompany= updatedTool.ToolCompany;
        existingTool.CommandParameter = updatedTool.CommandParameter;
        existingTool.ToolDescription = updatedTool.ToolDescription;
        existingTool.SkipDownloadToStation = updatedTool.SkipDownloadToStation;
        existingTool.IsEnabled = updatedTool.IsEnabled;
        existingTool.IsSystemInitTool = updatedTool.IsSystemInitTool;
        existingTool.IsBrowsable = updatedTool.IsBrowsable;
        existingTool.SupportsRemoteVisualization = updatedTool.SupportsRemoteVisualization;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingTool;
    }
    public async Task<Tool?> GetByDisplayNameAsync(string ToolDisplayName, CancellationToken cancellationToken = default)
    {
        var toolName = ToolDisplayName.Split("(")[0];
        var toolVersion = ToolDisplayName.Split("(")[1].TrimEnd(')');
        try
        {
            return await _dbContext.Set<Tool>()
                .Where(t => t.ToolName == toolName).FirstOrDefaultAsync(t => t.ToolVersion == toolVersion,cancellationToken);
        }
        catch (Exception ex)
        {
            CLog.Error(ex.ToString());
            return null;
        }
    }

    public async Task<bool> MoveToolFilesFromTmpAsync(string toolName, bool isSystemTool, string userName)
    {
        try
        {
            var tmpPath = Path.Combine(GlobalData.ToolsRootPath, "Tmp");
            if (!Directory.Exists(tmpPath))
            {
                return true; // 没有临时文件，直接返回成功
            }

            // 确定目标路径
            string targetPath;
            if (isSystemTool)
            {
                targetPath = Path.Combine(GlobalData.SystemToolsPath, toolName);
            }
            else
            {
                targetPath = Path.Combine(GlobalData.GetUserToolsPath(userName), toolName);
            }

            // 确保目标目录存在
            Directory.CreateDirectory(targetPath);

            // 移动所有文件
            var files = Directory.GetFiles(tmpPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(tmpPath, file);
                var targetFile = Path.Combine(targetPath, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                File.Move(file, targetFile, true);
            }

            // 删除空目录
            Directory.Delete(tmpPath, true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"移动工具包文件失败: {ex}");
            return false;
        }
    }

    public async Task<bool> DeleteToolTmpFilesAsync()
    {
        try
        {
            var tmpPath = Path.Combine(GlobalData.ToolsRootPath, "Tmp");
            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除临时工具包文件失败: {ex}");
            return false;
        }
    }

    public async Task<int> ImportDefaultToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTools = (await GetAllToolsAsync(cancellationToken))?.ToList() ?? [];
            
            // 构建现有工具的键值字典，用于快速查找
            var existingDict = existingTools
                .Where(t => t.ToolName != null && t.ToolVersion != null)
                .ToDictionary(
                    t => $"{t.ToolName}|{t.ToolVersion}",
                    t => t,
                    StringComparer.OrdinalIgnoreCase
                );

            int added = 0;
            int updated = 0;

            foreach (var defaultTool in ToolSeedDataProvider.DefaultTools)
            {
                if (string.IsNullOrEmpty(defaultTool.ToolName) || string.IsNullOrEmpty(defaultTool.ToolVersion))
                    continue;

                var key = $"{defaultTool.ToolName}|{defaultTool.ToolVersion}";
                
                if (existingDict.TryGetValue(key, out var existingTool))
                {
                    // 更新现有工具
                    existingTool.ToolPath = defaultTool.ToolPath;
                    existingTool.ToolBasePath = defaultTool.ToolBasePath;
                    existingTool.CommandParameter = defaultTool.CommandParameter;
                    existingTool.ToolDescription = defaultTool.ToolDescription;
                    existingTool.ToolCompany = defaultTool.ToolCompany;
                    existingTool.ToolType = defaultTool.ToolType;
                    existingTool.ToolLocation = defaultTool.ToolLocation;
                    existingTool.SkipDownloadToStation = defaultTool.SkipDownloadToStation;
                    existingTool.IsEnabled = defaultTool.IsEnabled;
                    existingTool.IsSystemInitTool = defaultTool.IsSystemInitTool;
                    existingTool.IsBrowsable = defaultTool.IsBrowsable;
                    existingTool.SupportsRemoteVisualization = defaultTool.SupportsRemoteVisualization;

                    _dbContext.Set<Tool>().Update(existingTool);
                    updated++;
                }
                else
                {
                    // 新增工具
                    var newTool = new Tool
                    {
                        ToolName = defaultTool.ToolName,
                        ToolVersion = defaultTool.ToolVersion,
                        ToolPath = defaultTool.ToolPath,
                        ToolBasePath = defaultTool.ToolBasePath,
                        CommandParameter = defaultTool.CommandParameter,
                        ToolDescription = defaultTool.ToolDescription,
                        ToolCompany = defaultTool.ToolCompany,
                        ToolType = defaultTool.ToolType,
                        ToolLocation = defaultTool.ToolLocation,
                        SkipDownloadToStation = defaultTool.SkipDownloadToStation,
                        IsEnabled = defaultTool.IsEnabled,
                        IsSystemInitTool = defaultTool.IsSystemInitTool,
                        IsBrowsable = defaultTool.IsBrowsable,
                        SupportsRemoteVisualization = defaultTool.SupportsRemoteVisualization
                    };

                    // 自动生成路径（如果为空）
                    if (string.IsNullOrEmpty(newTool.ToolPath) || newTool.ToolPath == newTool.ToolName)
                    {
                        if (newTool.IsSystemInitTool)
                        {
                            newTool.ToolPath = Path.Combine("Tools", "0System", newTool.ToolName!);
                        }
                        else
                        {
                            var userName = GetCurrentUserName();
                            newTool.ToolPath = Path.Combine("Tools", userName, newTool.ToolName!);
                        }
                    }

                    if (string.IsNullOrEmpty(newTool.ToolBasePath))
                    {
                        if (newTool.IsSystemInitTool)
                        {
                            newTool.ToolBasePath = Path.Combine("Tools", "0System", newTool.ToolName!);
                        }
                        else
                        {
                            var userName = GetCurrentUserName();
                            newTool.ToolBasePath = Path.Combine("Tools", userName, newTool.ToolName!);
                        }
                    }

                    _dbContext.Set<Tool>().Add(newTool);
                    added++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return added + updated; // 返回总处理数量
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导入默认工具失败: {ex}");
            throw;
        }
    }

    private string GetCurrentUserName()
    {
        try
        {
            var identityName = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(identityName))
                return identityName;

            // 不降级到 Environment.UserName，避免使用 OS 登录名而非应用登录用户
            // 调用方（DesktopPlugConfig 等）应该在创建 Tool 前预填充 ToolPath/ToolBasePath
            Log.Warning("GetCurrentUserName: HttpContext.User.Identity.Name 为空，且无降级方案。"
                + "若 ToolPath/ToolBasePath 未预填充，工具路径将无法正确生成。"
                + "请确保调用方在创建 Tool 前正确设置了 ToolPath 和 ToolBasePath。");
            return "unknown_user";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetCurrentUserName 异常");
            return "unknown_user";
        }
    }
}

