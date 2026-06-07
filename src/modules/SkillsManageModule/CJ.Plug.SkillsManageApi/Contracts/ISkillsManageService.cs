using CJ.Plug.Models.Skills;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Http;

namespace CJ.Plug.SkillsManageApi;

public interface ISkillsManageService : IBaseRepositoryService<Skill, int>
{
    Task<IEnumerable<Skill>> GetActiveSkillsAsync();

    /// <summary>
    /// 获取技能关联文件列表
    /// </summary>
    Task<List<SkillFileInfo>> GetSkillFilesAsync(int skillId);

    /// <summary>
    /// 下载单个技能文件，返回文件流、文件名和内容类型
    /// </summary>
    Task<(Stream? fileStream, string? fileName, string? contentType)> DownloadSkillFileAsync(int skillId, string fileName);

    /// <summary>
    /// 打包下载技能所有文件为 zip，返回文件流和 zip 文件名
    /// </summary>
    Task<(Stream? fileStream, string? fileName)> DownloadSkillFilesArchiveAsync(int skillId);

    /// <summary>
    /// 上传文件到指定 Skill 目录
    /// </summary>
    Task UploadSkillFilesAsync(int skillId, List<IFormFile> files);

    /// <summary>
    /// 删除指定 Skill 的单个文件
    /// </summary>
    Task<bool> DeleteSkillFileAsync(int skillId, string fileName);

    /// <summary>
    /// 打包下载技能指定子文件夹为 zip
    /// </summary>
    Task<(Stream? fileStream, string? fileName)> DownloadSkillFolderArchiveAsync(int skillId, string folderRelativePath);

    /// <summary>
    /// 递归删除技能指定子文件夹
    /// </summary>
    Task<bool> DeleteSkillFolderAsync(int skillId, string folderRelativePath);

    /// <summary>
    /// 获取技能描述文件（{skillName}.md）的内容
    /// </summary>
    Task<string?> GetSkillReadmeAsync(int skillId);

    /// <summary>
    /// 创建技能描述文件（{skillName}.md），返回创建后的文件内容
    /// </summary>
    Task<string?> CreateSkillReadmeAsync(int skillId, string? content = null);

    /// <summary>
    /// 从 zip 包导入 Skill（含 skill.md 元数据和关联文件），返回创建的 Skill
    /// </summary>
    Task<Skill> ImportSkillFromZipAsync(Stream zipStream);
}
