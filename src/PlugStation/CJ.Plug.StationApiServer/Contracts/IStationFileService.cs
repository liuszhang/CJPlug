

namespace CJ.Plug.StationApiService.Contracts
{
    public interface IStationFileService
    {
        //通过文件ID下载文件,返回文件真实路径
        Task<string> DownloadFileByFileId(string fileId,string fileName);
        Task<(string?,string?)> UploadFileToVariable(PlugVariableData variableData);
    }
}
