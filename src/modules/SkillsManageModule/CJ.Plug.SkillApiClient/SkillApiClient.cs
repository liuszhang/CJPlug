using CJ.Plug.Models.Skills;
using System.Net.Http.Json;

using FileItem = (System.IO.Stream Stream, string FileName);

namespace CJ.Plug.SkillApiClient;

public class SkillApiClient : BaseApiClient, ISkillApiClient
{
    public SkillApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    public async Task<IEnumerable<Skill?>> GetAllSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = httpClient.GetFromJsonAsAsyncEnumerable<Skill>("/api/skills/getSkills", cancellationToken);
        return result.ToEnumerable();
    }

    public async Task<IEnumerable<Skill?>> GetActiveSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = httpClient.GetFromJsonAsAsyncEnumerable<Skill>("/api/skills/getActiveSkills", cancellationToken);
        return result.ToEnumerable();
    }

    public async Task<Skill?> CreateSkillAsync(Skill request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/skills/addSkill", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Skill>(cancellationToken: cancellationToken);
    }

    public async Task<Skill?> UpdateSkillAsync(Skill request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/skills/updateSkill", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Skill>(cancellationToken: cancellationToken);
    }

    public async Task DeleteSkillAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/skills/deleteSkill/{skillId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<SkillFileInfo>> GetSkillFilesAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/skills/{skillId}/files", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SkillFileInfo>>(cancellationToken: cancellationToken) ?? new();
    }

    public async Task<Stream?> DownloadSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default)
    {
        var encodedFileName = Uri.EscapeDataString(fileName);
        var response = await httpClient.GetAsync($"/api/skills/{skillId}/files/download/{encodedFileName}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<Stream?> DownloadSkillFilesArchiveAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/skills/{skillId}/files/archive", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task UploadSkillFilesAsync(int skillId, List<FileItem> files, CancellationToken cancellationToken = default)
    {
        await UploadSkillFilesWithPathsAsync(skillId, files.Select(f => (f.Stream, f.FileName, (string?)null)).ToList(), cancellationToken);
    }

    public async Task UploadSkillFilesWithPathsAsync(int skillId, List<(Stream Stream, string FileName, string? RelativePath)> files, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();

        foreach (var (stream, fileName, relativePath) in files)
        {
            var fileContent = new StreamContent(stream);
            
            // 使用自定义头传递相对路径信息
            if (!string.IsNullOrEmpty(relativePath))
            {
                fileContent.Headers.Add("X-Relative-Path", Uri.EscapeDataString(relativePath.Replace('\\', '/')));
            }
            
            form.Add(fileContent, "files", fileName);
        }

        var response = await httpClient.PostAsync($"/api/skills/{skillId}/files/upload", form, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> DeleteSkillFileAsync(int skillId, string fileName, CancellationToken cancellationToken = default)
    {
        var encodedFileName = Uri.EscapeDataString(fileName);
        var response = await httpClient.DeleteAsync($"/api/skills/{skillId}/files/{encodedFileName}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<Stream?> DownloadSkillFolderArchiveAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default)
    {
        var encodedFolderPath = Uri.EscapeDataString(folderRelativePath);
        var response = await httpClient.GetAsync($"/api/skills/{skillId}/files/folder-archive/{encodedFolderPath}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<bool> DeleteSkillFolderAsync(int skillId, string folderRelativePath, CancellationToken cancellationToken = default)
    {
        var encodedFolderPath = Uri.EscapeDataString(folderRelativePath);
        var response = await httpClient.DeleteAsync($"/api/skills/{skillId}/files/folder/{encodedFolderPath}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetSkillReadmeAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/skills/{skillId}/readme", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<ReadmeResponse>(cancellationToken: cancellationToken);
        return result?.Content;
    }

    public async Task<string?> CreateSkillReadmeAsync(int skillId, string? content = null, CancellationToken cancellationToken = default)
    {
        var body = new { content };
        var response = await httpClient.PostAsJsonAsync($"/api/skills/{skillId}/readme", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<ReadmeResponse>(cancellationToken: cancellationToken);
        return result?.Content;
    }

    private record ReadmeResponse(string Content);

    public async Task<Skill?> ImportSkillAsync(Stream zipStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(zipStream);
        form.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync("/api/skills/import", form, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Skill>(cancellationToken: cancellationToken);
    }

    public async Task<Skill?> ImportSkillFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var body = new { Url = url };
        var response = await httpClient.PostAsJsonAsync("/api/skills/import", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Skill>(cancellationToken: cancellationToken);
    }
}