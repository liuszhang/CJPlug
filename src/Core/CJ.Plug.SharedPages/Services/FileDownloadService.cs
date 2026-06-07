using Elsa.Studio.DomInterop.Contracts;

namespace CJ.Plug.SharedPages.Services;

/// <summary>
/// 公共文件下载服务，封装 Elsa Studio 框架的 IFiles 下载能力。
/// 通过 DI 注入 IFiles，将 Stream 数据以浏览器下载方式输出。
/// </summary>
public class FileDownloadService
{
    private readonly IFiles _files;

    public FileDownloadService(IFiles files)
    {
        _files = files;
    }

    /// <summary>
    /// 将流数据以指定文件名触发浏览器下载。
    /// </summary>
    /// <param name="fileName">下载时显示的文件名</param>
    /// <param name="stream">文件内容流（方法内部负责释放）</param>
    public async Task DownloadFileFromStreamAsync(string fileName, Stream stream)
    {
        await _files.DownloadFileFromStreamAsync(fileName, stream);
    }
}