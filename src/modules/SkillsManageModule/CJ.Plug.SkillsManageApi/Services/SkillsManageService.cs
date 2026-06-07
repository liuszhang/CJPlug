using CJ.Plug.Models.Skills;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CJ.Plug.SkillsManageApi;

public class SkillsManageService : BaseRepositoryService<Skill, int>, ISkillsManageService
{
    public SkillsManageService(MainDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Skill>> GetActiveSkillsAsync()
    {
        return await _dbContext.Set<Skill>().Where(s => s.IsEnabled).ToListAsync();
    }

    /// <summary>
    /// 获取 Skill 的文件存储根目录
    /// </summary>
    private static string GetSkillFilesRoot(int skillId)
    {
        return Path.Combine(GlobalData.MainFileServerPathRoot, "Skills", skillId.ToString());
    }

    public Task<List<SkillFileInfo>> GetSkillFilesAsync(int skillId)
    {
        var dir = GetSkillFilesRoot(skillId);
        var result = new List<SkillFileInfo>();

        if (!Directory.Exists(dir))
            return Task.FromResult(result);

        try
        {
            // 递归获取所有文件，保留相对路径
            var allFiles = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            var baseDir = new DirectoryInfo(dir);

            foreach (var filePath in allFiles)
            {
                var fileInfo = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(baseDir.FullName, fileInfo.FullName);
                
                result.Add(new SkillFileInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = relativePath.Replace('\\', '/'), // 统一使用正斜杠
                    FileSize = fileInfo.Length,
                    FileType = (fileInfo.Extension.StartsWith('.') ? fileInfo.Extension[1..] : fileInfo.Extension).ToLowerInvariant(),
                    ModifiedTime = fileInfo.LastWriteTime
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "获取 Skill 文件列表失败，SkillId={SkillId}", skillId);
        }

        return Task.FromResult(result);
    }

    public async Task<(Stream? fileStream, string? fileName, string? contentType)> DownloadSkillFileAsync(int skillId, string fileName)
    {
        try
        {
            var dir = GetSkillFilesRoot(skillId);
            
            // fileName 可能是相对路径，需要正确处理
            var relativePath = fileName.Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(dir, relativePath);

            // 安全检查：确保最终路径在 skill 目录内
            var fullPath = Path.GetFullPath(filePath);
            var fullDir = Path.GetFullPath(dir);
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar) && fullPath != fullDir)
            {
                Log.Warning("非法文件路径访问尝试: {FilePath}", filePath);
                return (null, null, null);
            }

            if (!File.Exists(fullPath))
                return (null, null, null);

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            var contentType = GetContentType(Path.GetExtension(fileName));
            var downloadFileName = Path.GetFileName(fileName); // 下载时使用文件名，不包含路径

            return await Task.FromResult((fileStream, downloadFileName, contentType));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载 Skill 文件失败，SkillId={SkillId}, FileName={FileName}", skillId, fileName);
            return (null, null, null);
        }
    }

    public async Task<(Stream? fileStream, string? fileName)> DownloadSkillFilesArchiveAsync(int skillId)
    {
        try
        {
            var dir = GetSkillFilesRoot(skillId);

            if (!Directory.Exists(dir))
                return (null, null);

            // 递归查找所有文件
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
                return (null, null);

            // 获取 Skill 名称用于 zip 文件名
            var skill = await _dbContext.Set<Skill>().FindAsync(skillId);
            var skillName = skill?.Name ?? $"skill_{skillId}";
            var zipFileName = $"{skillName}_files.zip";

            // 使用临时文件避免内存压力
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"skill_download_{Guid.NewGuid()}.zip");
            
            // 保留文件夹层级结构创建 zip
            var baseDir = new DirectoryInfo(dir);
            using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                foreach (var filePath in files)
                {
                    var relativePath = Path.GetRelativePath(baseDir.FullName, filePath);
                    zipArchive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Fastest);
                }
            } // zipArchive 在此 Dispose，确保 zip 文件完整写入磁盘

            var fileStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            return (fileStream, zipFileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打包下载 Skill 文件失败，SkillId={SkillId}", skillId);
            return (null, null);
        }
    }

    public async Task UploadSkillFilesAsync(int skillId, List<IFormFile> files)
    {
        var dir = GetSkillFilesRoot(skillId);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // 从 Content-Disposition 头中获取相对路径（如果存在）
            var relativePath = GetRelativePathFromFormFile(file);
            var targetPath = string.IsNullOrEmpty(relativePath) 
                ? Path.Combine(dir, file.FileName)
                : Path.Combine(dir, relativePath.Replace('/', Path.DirectorySeparatorChar));

            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 如果文件已存在，添加序号避免覆盖
            if (File.Exists(targetPath))
            {
                var dirName = Path.GetDirectoryName(targetPath) ?? string.Empty;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
                var ext = Path.GetExtension(targetPath);
                var counter = 1;
                do
                {
                    targetPath = Path.Combine(dirName, $"{nameWithoutExt}_{counter}{ext}");
                    counter++;
                } while (File.Exists(targetPath));
            }

            await using var targetStream = new FileStream(targetPath, FileMode.CreateNew);
            await file.CopyToAsync(targetStream);
        }
    }

    /// <summary>
    /// 从自定义头中提取相对路径
    /// </summary>
    private static string? GetRelativePathFromFormFile(IFormFile file)
    {
        if (file is FormFile formFile && formFile.Headers != null)
        {
            var headerValue = formFile.Headers["X-Relative-Path"];
            if (!string.IsNullOrEmpty(headerValue))
            {
                return Uri.UnescapeDataString(headerValue);
            }
        }
        return null;
    }

    public async Task<bool> DeleteSkillFileAsync(int skillId, string fileName)
    {
        try
        {
            var dir = GetSkillFilesRoot(skillId);
            var relativePath = fileName.Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(dir, relativePath);
            var fullPath = Path.GetFullPath(filePath);
            var fullDir = Path.GetFullPath(dir);

            // 安全检查：禁止路径遍历攻击
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar) && fullPath != fullDir)
            {
                Log.Warning("非法文件删除路径尝试: {FilePath}", filePath);
                return false;
            }

            if (!File.Exists(fullPath))
                return false;

            File.Delete(fullPath);
            Log.Information("删除 Skill 文件成功，SkillId={SkillId}, FileName={FileName}", skillId, fileName);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除 Skill 文件失败，SkillId={SkillId}, FileName={FileName}", skillId, fileName);
            return false;
        }
    }

    public async Task<(Stream? fileStream, string? fileName)> DownloadSkillFolderArchiveAsync(int skillId, string folderRelativePath)
    {
        try
        {
            var dir = GetSkillFilesRoot(skillId);
            var targetDir = Path.Combine(dir, folderRelativePath.Replace('/', Path.DirectorySeparatorChar));

            // 安全检查
            var fullPath = Path.GetFullPath(targetDir);
            var fullDir = Path.GetFullPath(dir);
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar) && fullPath != fullDir)
            {
                Log.Warning("非法文件夹路径访问尝试: {FolderPath}", folderRelativePath);
                return (null, null);
            }

            if (!Directory.Exists(fullPath))
                return (null, null);

            var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
                return (null, null);

            var folderName = Path.GetFileName(fullPath);
            var zipFileName = $"{folderName}.zip";
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"skill_folder_{Guid.NewGuid()}.zip");

            using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                foreach (var filePath in files)
                {
                    var relativePath = Path.GetRelativePath(fullPath, filePath);
                    zipArchive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Fastest);
                }
            }

            var fileStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            return (fileStream, zipFileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打包下载 Skill 文件夹失败，SkillId={SkillId}, Folder={Folder}", skillId, folderRelativePath);
            return (null, null);
        }
    }

    public async Task<bool> DeleteSkillFolderAsync(int skillId, string folderRelativePath)
    {
        try
        {
            var dir = GetSkillFilesRoot(skillId);
            var targetDir = Path.Combine(dir, folderRelativePath.Replace('/', Path.DirectorySeparatorChar));

            // 安全检查
            var fullPath = Path.GetFullPath(targetDir);
            var fullDir = Path.GetFullPath(dir);
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar) && fullPath != fullDir)
            {
                Log.Warning("非法文件夹删除路径尝试: {FolderPath}", folderRelativePath);
                return false;
            }

            if (!Directory.Exists(fullPath))
                return false;

            Directory.Delete(fullPath, true);
            Log.Information("删除 Skill 文件夹成功，SkillId={SkillId}, Folder={Folder}", skillId, folderRelativePath);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除 Skill 文件夹失败，SkillId={SkillId}, Folder={Folder}", skillId, folderRelativePath);
            return false;
        }
    }

    public async Task<string?> GetSkillReadmeAsync(int skillId)
    {
        try
        {
            var skill = await _dbContext.Set<Skill>().FindAsync(skillId);
            if (skill == null) return null;

            var dir = GetSkillFilesRoot(skillId);
            var mdFilePath = Path.Combine(dir, $"{skill.Name}.md");

            if (!File.Exists(mdFilePath))
                return null;

            return await File.ReadAllTextAsync(mdFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取技能描述文件失败，SkillId={SkillId}", skillId);
            return null;
        }
    }

    public async Task<string?> CreateSkillReadmeAsync(int skillId, string? content = null)
    {
        try
        {
            var skill = await _dbContext.Set<Skill>().FindAsync(skillId);
            if (skill == null) return null;

            var dir = GetSkillFilesRoot(skillId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var mdFilePath = Path.Combine(dir, $"{skill.Name}.md");

            // 如果文件已存在，不覆盖
            if (File.Exists(mdFilePath))
                return await File.ReadAllTextAsync(mdFilePath);

            var mdContent = content ?? $"# {skill.DisplayName ?? skill.Name}\n\n> 技能描述文件，请编辑此文件。\n";
            await File.WriteAllTextAsync(mdFilePath, mdContent);
            Log.Information("创建技能描述文件成功，SkillId={SkillId}, Path={Path}", skillId, mdFilePath);
            return mdContent;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建技能描述文件失败，SkillId={SkillId}", skillId);
            return null;
        }
    }

    /// <summary>
    /// 从 zip 包导入 Skill（含 skill.md 元数据和关联文件）
    /// </summary>
    public async Task<Skill> ImportSkillFromZipAsync(Stream zipStream)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"skill_import_{Guid.NewGuid()}");
        try
        {
            // 解压到临时目录
            Directory.CreateDirectory(tempDir);
            var tempZipPath = Path.Combine(tempDir, "import.zip");
            await using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
            {
                await zipStream.CopyToAsync(fs);
            }
            ZipFile.ExtractToDirectory(tempZipPath, tempDir, true);
            File.Delete(tempZipPath);

            // 查找 skill.md
            var skillJsonPath = FindFileInDirectory(tempDir, "skill.md");
            if (skillJsonPath == null)
                throw new InvalidOperationException("zip 包中未找到 skill.md 文件");

            var jsonContent = await File.ReadAllTextAsync(skillJsonPath);
            var skillMeta = JsonSerializer.Deserialize<Skill>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (skillMeta == null || string.IsNullOrWhiteSpace(skillMeta.Name))
                throw new InvalidOperationException("skill.md 中缺少必要的 name 字段");

            // 创建 Skill 记录（系统预设）
            var skill = new Skill
            {
                Name = skillMeta.Name,
                DisplayName = skillMeta.DisplayName ?? skillMeta.Name,
                Description = skillMeta.Description,
                PromptTemplate = skillMeta.PromptTemplate,
                Category = skillMeta.Category,
                Icon = skillMeta.Icon,
                Author = skillMeta.Author,
                SourcePlugId = skillMeta.SourcePlugId,
                IsPreset = true,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Set<Skill>().AddAsync(skill);
            await _dbContext.SaveChangesAsync();

            // 将 zip 中的文件（除 skill.md 外）复制到 Skill 文件目录
            var skillDir = GetSkillFilesRoot(skill.Id);
            if (!Directory.Exists(skillDir))
                Directory.CreateDirectory(skillDir);

            var skillJsonDir = Path.GetDirectoryName(skillJsonPath)!;
            var allFiles = Directory.GetFiles(skillJsonDir, "*", SearchOption.AllDirectories);
            foreach (var srcFile in allFiles)
            {
                // 跳过 skill.md
                if (Path.GetFileName(srcFile).Equals("skill.md", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativePath = Path.GetRelativePath(skillJsonDir, srcFile);
                var destPath = Path.Combine(skillDir, relativePath);

                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(srcFile, destPath, true);
            }

            Log.Information("从 zip 导入 Skill 成功，SkillId={SkillId}, Name={Name}", skill.Id, skill.Name);
            return skill;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "从 zip 导入 Skill 失败");
            throw;
        }
        finally
        {
            // 清理临时目录
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "清理导入临时目录失败: {TempDir}", tempDir);
            }
        }
    }

    /// <summary>
    /// 在目录中递归查找指定文件名
    /// </summary>
    private static string? FindFileInDirectory(string dir, string fileName)
    {
        var files = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }

    /// <summary>
    /// 根据文件扩展名获取 MIME 类型
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".py" => "text/x-python",
            ".cs" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".zip" => "application/zip",
            ".yaml" or ".yml" => "application/x-yaml",
            _ => "application/octet-stream"
        };
    }
}
