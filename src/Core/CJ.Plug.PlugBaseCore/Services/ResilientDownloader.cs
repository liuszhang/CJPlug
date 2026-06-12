using CJ.Plug.Models.LogModels;
using Serilog;
using System.IO.Compression;
using System.Security.Cryptography;

namespace CJ.Plug.PlugBaseCore.Services;

/// <summary>
/// 带超时、重试、SHA256 校验、残留清理的可靠下载器。
/// 按工具名+版本号的维度进行重试清理，确保失败后不留破损文件。
/// 
/// 当前直接接收已配置超时的 HttpClient 实例，后续可升级为 IHttpClientFactory 注入。
/// </summary>
public class ResilientDownloader
{
    /// <summary>
    /// 下载工具 zip 并解压到目标目录。
    /// </summary>
    /// <param name="httpClient">已配置超时和 BaseAddress 的 HttpClient</param>
    /// <param name="downloadUrl">下载 URL（相对或绝对）</param>
    /// <param name="targetDir">解压目标目录</param>
    /// <param name="expectedSha256">期望的 SHA256，null 则跳过校验</param>
    /// <param name="retryCount">重试次数，默认 3</param>
    /// <param name="timeoutPerAttempt">单次尝试超时，默认 30 分钟</param>
    /// <param name="ct">取消令牌</param>
    public async Task DownloadAndExtractAsync(
        HttpClient httpClient,
        string downloadUrl,
        string targetDir,
        string? expectedSha256 = null,
        int retryCount = 3,
        TimeSpan? timeoutPerAttempt = null,
        CancellationToken ct = default)
    {
        timeoutPerAttempt ??= TimeSpan.FromMinutes(30);
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"tool_{Guid.NewGuid()}.zip");

        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                // 1. 清理上次失败的残留
                SafeDeleteFile(tempZipPath);
                if (Directory.Exists(targetDir))
                {
                    try { Directory.Delete(targetDir, recursive: true); }
                    catch (Exception ex) { Log.Warning("清理目标目录残留失败: {Path}, {Error}", targetDir, ex.Message); }
                }

                // 2. 下载（带超时）
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutPerAttempt.Value);

                var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                await using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cts.Token);
                }

                // 3. SHA256 校验（可选）
                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    var actualSha256 = await ComputeSha256Async(tempZipPath, ct);
                    if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        SafeDeleteFile(tempZipPath);
                        throw new InvalidOperationException(
                            $"SHA256 校验失败: 期望 {expectedSha256}, 实际 {actualSha256}");
                    }
                }

                // 4. 解压
                Directory.CreateDirectory(targetDir);
                ZipFile.ExtractToDirectory(tempZipPath, targetDir, overwriteFiles: true);

                // 5. 成功，清理临时 zip
                SafeDeleteFile(tempZipPath);
                return;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // 超时，进入重试
                SafeDeleteFile(tempZipPath);
                if (Directory.Exists(targetDir))
                {
                    try { Directory.Delete(targetDir, recursive: true); }
                    catch { }
                }

                if (attempt < retryCount)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt);
                    CLog.Warning($"下载工具失败(第{attempt}次超时)，{delaySeconds}秒后重试");
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                }
            }
            catch (Exception ex) when (attempt < retryCount)
            {
                SafeDeleteFile(tempZipPath);
                if (Directory.Exists(targetDir))
                {
                    try { Directory.Delete(targetDir, recursive: true); }
                    catch { }
                }

                var delaySeconds = (int)Math.Pow(2, attempt);
                CLog.Warning($"下载工具失败(第{attempt}次): {ex.Message}，{delaySeconds}秒后重试");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
            }
        }

        SafeDeleteFile(tempZipPath);
        throw new InvalidOperationException($"下载工具失败，已重试 {retryCount} 次");
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Log.Warning("清理临时文件失败: {Path}, {Error}", path, ex.Message);
        }
    }
}
