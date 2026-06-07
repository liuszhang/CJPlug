using CJ.Plug.AuditModels;
using CJ.Plug.SkillApiClient;
using CJ.Plug.Models.Skills;

public partial class MainApiClient : ISkillApiClient
{
    public async Task<IEnumerable<Skill?>> GetAllSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.GetAllSkillsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, "查询所有技能");
        return result;
    }

    public async Task<IEnumerable<Skill?>> GetActiveSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.GetActiveSkillsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, "查询活跃技能");
        return result;
    }

    public async Task<Skill?> CreateSkillAsync(Skill request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.CreateSkillAsync(request, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Create, $"创建技能: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Create, $"创建技能失败: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<Skill?> UpdateSkillAsync(Skill request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.UpdateSkillAsync(request, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Update, $"更新技能: {request.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Update, $"更新技能失败: {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task DeleteSkillAsync(int skillId, CancellationToken cancellationToken = default)
    {
        await SkillApiClient.Value.DeleteSkillAsync(skillId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Delete, $"删除技能ID: {skillId}");
    }

    public async Task<List<SkillFileInfo>> GetSkillFilesAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.GetSkillFilesAsync(skillId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, $"查询技能文件列表 - 技能ID: {skillId}");
        return result;
    }

    public async Task<Stream?> DownloadSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.DownloadSkillFileAsync(skillId, fileName, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, $"下载技能文件: {fileName} - 技能ID: {skillId}");
        return result;
    }

    public async Task<Stream?> DownloadSkillFilesArchiveAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.DownloadSkillFilesArchiveAsync(skillId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, $"打包下载技能所有文件 - 技能ID: {skillId}");
        return result;
    }

    public async Task UploadSkillFilesAsync(int skillId, List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default)
    {
        try
        {
            await SkillApiClient.Value.UploadSkillFilesAsync(skillId, files, cancellationToken);
            var fileNames = string.Join(", ", files.Select(f => f.FileName));
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Update, $"上传技能文件: {fileNames} - 技能ID: {skillId}");
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Update, $"上传技能文件失败 - 技能ID: {skillId}", ex.Message);
            throw;
        }
    }

    public async Task UploadSkillFilesWithPathsAsync(int skillId, List<(Stream Stream, string FileName, string? RelativePath)> files, CancellationToken cancellationToken = default)
    {
        try
        {
            // 将元组列表转换为 SkillApiClient 期望的格式
            var fileItems = files.Select(f => (f.Stream, f.FileName, f.RelativePath)).ToList();
            await SkillApiClient.Value.UploadSkillFilesWithPathsAsync(skillId, fileItems, cancellationToken);
            var fileNames = string.Join(", ", files.Select(f => f.FileName));
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Update, $"上传技能文件(含路径): {fileNames} - 技能ID: {skillId}");
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Update, $"上传技能文件(含路径)失败 - 技能ID: {skillId}", ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.DeleteSkillFileAsync(skillId, fileName, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Delete, $"删除技能文件: {fileName} - 技能ID: {skillId}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Delete, $"删除技能文件失败: {fileName} - 技能ID: {skillId}", ex.Message);
            throw;
        }
    }

    public async Task<Stream?> DownloadSkillFolderArchiveAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.DownloadSkillFolderArchiveAsync(skillId, folderRelativePath, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, $"打包下载技能文件夹: {folderRelativePath} - 技能ID: {skillId}");
        return result;
    }

    public async Task<bool> DeleteSkillFolderAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.DeleteSkillFolderAsync(skillId, folderRelativePath, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Delete, $"删除技能文件夹: {folderRelativePath} - 技能ID: {skillId}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Delete, $"删除技能文件夹失败: {folderRelativePath} - 技能ID: {skillId}", ex.Message);
            throw;
        }
    }

    public async Task<string?> GetSkillReadmeAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var result = await SkillApiClient.Value.GetSkillReadmeAsync(skillId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Other, $"获取技能描述文件 - 技能ID: {skillId}");
        return result;
    }

    public async Task<string?> CreateSkillReadmeAsync(int skillId, string? content = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.CreateSkillReadmeAsync(skillId, content, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Create, $"创建技能描述文件 - 技能ID: {skillId}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Create, $"创建技能描述文件失败 - 技能ID: {skillId}", ex.Message);
            throw;
        }
    }

    public async Task<Skill?> ImportSkillAsync(Stream zipStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.ImportSkillAsync(zipStream, fileName, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Create, $"从 zip 导入技能: {result?.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Create, $"从 zip 导入技能失败: {fileName}", ex.Message);
            throw;
        }
    }

    public async Task<Skill?> ImportSkillFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SkillApiClient.Value.ImportSkillFromUrlAsync(url, cancellationToken);
            await AuditLog.LogSuccessAsync(AuditModule.Skill, AuditOperationType.Create, $"从 URL 导入技能: {result?.Name}");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.Skill, AuditOperationType.Create, $"从 URL 导入技能失败: {url}", ex.Message);
            throw;
        }
    }
}
