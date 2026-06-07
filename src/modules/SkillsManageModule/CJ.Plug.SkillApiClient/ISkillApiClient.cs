using CJ.Plug.Models.Skills;

namespace CJ.Plug.SkillApiClient;

public interface ISkillApiClient
{
    Task<IEnumerable<Skill?>> GetAllSkillsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Skill?>> GetActiveSkillsAsync(CancellationToken cancellationToken = default);
    Task<Skill?> CreateSkillAsync(Skill request, CancellationToken cancellationToken = default);
    Task<Skill?> UpdateSkillAsync(Skill request, CancellationToken cancellationToken = default);
    Task DeleteSkillAsync(int skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取技能关联文件列表
    /// </summary>
    Task<List<SkillFileInfo>> GetSkillFilesAsync(int skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载单个技能文件，返回文件流
    /// </summary>
    Task<Stream?> DownloadSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打包下载技能所有文件，返回 zip 流
    /// </summary>
    Task<Stream?> DownloadSkillFilesArchiveAsync(int skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传文件到指定 Skill 目录（接受流 + 文件名列表）
    /// </summary>
    Task UploadSkillFilesAsync(int skillId, List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传文件到指定 Skill 目录（支持相对路径，保留文件夹层级结构）
    /// </summary>
    Task UploadSkillFilesWithPathsAsync(int skillId, List<(Stream Stream, string FileName, string? RelativePath)> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定 Skill 的单个文件
    /// </summary>
    Task<bool> DeleteSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打包下载技能指定子文件夹为 zip
    /// </summary>
    Task<Stream?> DownloadSkillFolderArchiveAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归删除技能指定子文件夹
    /// </summary>
    Task<bool> DeleteSkillFolderAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取技能描述文件（{skillName}.md）的内容，不存在返回 null
    /// </summary>
    Task<string?> GetSkillReadmeAsync(int skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建技能描述文件（{skillName}.md），返回创建后的文件内容
    /// </summary>
    Task<string?> CreateSkillReadmeAsync(int skillId, string? content = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从 zip 文件导入 Skill（上传 zip 到服务端解析）
    /// </summary>
    Task<Skill?> ImportSkillAsync(Stream zipStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从 URL 导入 Skill（服务端下载 zip 后解析）
    /// </summary>
    Task<Skill?> ImportSkillFromUrlAsync(string url, CancellationToken cancellationToken = default);
}